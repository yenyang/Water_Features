// <copyright file="SeasonalStreamsSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Systems
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.Prefabs.Climate;
    using Game.Simulation;
    using Game.Tools;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using Water_Features.Components;
    using Water_Features.Utils;
    using static Game.Simulation.ClimateSystem;

    /// <summary>
    /// A system for handing stream seasonality and runoff.
    /// </summary>
    public partial class SeasonalStreamsSystem : GameSystemBase
    {
        public static readonly int kUpdatesPerDay = 128;

        private TypeHandle __TypeHandle;
        private ClimateSystem m_ClimateSystem;
        private string m_CurrentSeason;
        private EntityQuery m_ClimateQuery;
        private EntityQuery m_OriginalAmountsQuery;
        private PrefabSystem m_PrefabSystem;
        private ClimatePrefab m_ClimatePrefab;
        private float m_CurrentSeasonMeanPrecipitation = 0f;
        private TerrainSystem m_TerrainSystem;
        private float m_MaxSeasonMeanPrecipitation = 0f;
        private Entity m_ClimateEntity;
        private ILog m_Log;
        private bool ClimateInteractionInitialized = false;
        private EndFrameBarrier m_EndFrameBarrier;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeasonalStreamsSystem"/> class.
        /// </summary>
        public SeasonalStreamsSystem()
        {
        }

        /// <inheritdoc/>
        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 / kUpdatesPerDay;
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = WaterFeaturesMod.Instance.Log;
            m_ClimateSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ClimateSystem>();
            m_CurrentSeason = m_ClimateSystem.currentSeasonNameID;
            m_PrefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
            m_ClimateQuery = GetEntityQuery(ComponentType.ReadOnly<ClimateData>());
            m_TerrainSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TerrainSystem>();
            m_EndFrameBarrier = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_OriginalAmountsQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new ()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<Game.Simulation.WaterSourceData>(),
                        ComponentType.ReadWrite<SeasonalStreamsData>(),
                        ComponentType.ReadOnly<Game.Objects.Transform>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Owner>(),
                    },
                },
            });
            RequireForUpdate(m_ClimateQuery);
            RequireForUpdate(m_OriginalAmountsQuery);
            m_Log.Info($"[{nameof(SeasonalStreamsSystem)}] {nameof(OnCreate)}");
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (ClimateInteractionInitialized == false)
            {
                InitializeClimateInteraction();
                ClimateInteractionInitialized = true;
            }

            if (m_ClimateSystem.currentSeasonNameID != m_CurrentSeason)
            {
                m_CurrentSeason = m_ClimateSystem.currentSeasonNameID;
                float normalizedClimateDate = GetClimateDate();
                m_CurrentSeasonMeanPrecipitation = GetMeanPrecipitation(m_ClimatePrefab, normalizedClimateDate);
            }

            __TypeHandle.__Game_Simulation_WaterSourceData_RW_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Seasonal_Streams_OriginalAmountComponent_RW_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Entity_RO_TypeHandle.Update(ref CheckedStateRef);
            ReviseWaterSourcesJob reviseWaterSourcesJob = new ()
            {
                m_SourceType = __TypeHandle.__Game_Simulation_WaterSourceData_RW_ComponentTypeHandle,
                m_SeasonalStreamDataType = __TypeHandle.__Seasonal_Streams_OriginalAmountComponent_RW_ComponentTypeHandle,
                m_TerrainHeightData = m_TerrainSystem.GetHeightData(false),
                m_TransformType = __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle,
                m_EntityType = __TypeHandle.__Entity_RO_TypeHandle,
                buffer = m_EndFrameBarrier.CreateCommandBuffer(),
            };

            // This deals with maps with 0 mean precipitation.
            float seasonalFlows = 0f;
            if (m_MaxSeasonMeanPrecipitation > 0)
            {
                seasonalFlows = m_CurrentSeasonMeanPrecipitation / m_MaxSeasonMeanPrecipitation * WaterFeaturesMod.Settings.StreamSeasonality;
            }

            // If it's not snowing, according to the climate system calculate runoff for job.
            if (m_ClimateSystem.isSnowing == false)
            {
                // Calculate water source multiplier based on precipiattion, spring water, and seasonality.
                reviseWaterSourcesJob.m_WaterSourceMultiplier = Mathf.Clamp(seasonalFlows + (m_ClimateSystem.precipitation * WaterFeaturesMod.Settings.StreamStormwaterEffects) + WaterFeaturesMod.Settings.ConstantFlowRate, WaterFeaturesMod.Settings.MinimumMultiplier, WaterFeaturesMod.Settings.MaximumMultiplier);
                reviseWaterSourcesJob.m_SnowAccumulationMultiplier = 0f;

                // If the temperature is high enough to melt snow record the temperature and the leftover multiplier that can be used for snow melt.
                if (WaterFeaturesMod.Settings.SimulateSnowMelt == true && m_ClimateSystem.temperature.value > m_ClimateSystem.freezingTemperature && reviseWaterSourcesJob.m_WaterSourceMultiplier < WaterFeaturesMod.Settings.MaximumMultiplier)
                {
                    reviseWaterSourcesJob.m_PotentialSnowMeltMultiplier = WaterFeaturesMod.Settings.MaximumMultiplier - reviseWaterSourcesJob.m_WaterSourceMultiplier;
                    reviseWaterSourcesJob.m_TemperatureDifferential = m_ClimateSystem.temperature.value - m_ClimateSystem.freezingTemperature;
                }
                else
                {
                    reviseWaterSourcesJob.m_PotentialSnowMeltMultiplier = 0f;
                    reviseWaterSourcesJob.m_TemperatureDifferential = 0f;
                }
            }

            // If snowing and if simulating snowmelt then calculate the snow accumulation multiplier from precipiation.
            else if (WaterFeaturesMod.Settings.SimulateSnowMelt == true)
            {
                // Seasonal water flow and spring water still continue during snow.
                reviseWaterSourcesJob.m_WaterSourceMultiplier = Mathf.Clamp(seasonalFlows + WaterFeaturesMod.Settings.ConstantFlowRate, WaterFeaturesMod.Settings.MinimumMultiplier, WaterFeaturesMod.Settings.MaximumMultiplier);
                reviseWaterSourcesJob.m_SnowAccumulationMultiplier = m_ClimateSystem.precipitation * WaterFeaturesMod.Settings.StreamStormwaterEffects;
                reviseWaterSourcesJob.m_PotentialSnowMeltMultiplier = 0f;
                reviseWaterSourcesJob.m_TemperatureDifferential = 0f;
            }

            ReviseWaterSourcesJob jobData = reviseWaterSourcesJob;
            JobHandle jobHandle = JobChunkExtensions.Schedule(jobData, m_OriginalAmountsQuery, Dependency);
            m_TerrainSystem.AddCPUHeightReader(jobHandle);
            m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;
        }

        /// <inheritdoc/>
        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            ClimateInteractionInitialized = false;
            base.OnGamePreload(purpose, mode);
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (!WaterFeaturesMod.Settings.EnableSeasonalStreams)
            {
                m_Log.Info($"[{nameof(TidesAndWavesSystem)}] {nameof(OnCreate)} Seasonal Streams disabled.");
                Enabled = false;
                DisableSeasonalStreamSystem disableSeasonalStreamSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<DisableSeasonalStreamSystem>();
                disableSeasonalStreamSystem.Enabled = true;
            }
        }

        /// <inheritdoc/>
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            __TypeHandle.AssignHandles(ref CheckedStateRef);
        }

        /// <summary>
        /// This calculates the mean seasonal precipiations to figure out seasonality of the climate for the map.
        /// </summary>
        private void InitializeClimateInteraction()
        {
            var m_ClimateEntityVar = m_ClimateSystem.GetMemberValue("m_CurrentClimate");
            m_ClimateEntity = (Entity)m_ClimateEntityVar;
            m_ClimatePrefab = m_PrefabSystem.GetPrefab<ClimatePrefab>(m_ClimateEntity);
            float normalizedClimateDate = GetClimateDate();
            m_CurrentSeasonMeanPrecipitation = GetMeanPrecipitation(m_ClimatePrefab, normalizedClimateDate);
            float seasonLength = GetSeasonLength(m_ClimatePrefab, normalizedClimateDate);
            for (int i = 0; i < 4; i++)
            {
                float testDate = i * seasonLength;
                float testSeasonMeanPrecipitation = GetMeanPrecipitation(m_ClimatePrefab, testDate);
                if (testSeasonMeanPrecipitation > m_MaxSeasonMeanPrecipitation)
                {
                    m_MaxSeasonMeanPrecipitation = testSeasonMeanPrecipitation;
                }
            }
        }

        /// <summary>
        /// This gets the mean precipiation for a specified date.
        /// </summary>
        /// <param name="climatePrefab">The prefab for the map's climate.</param>
        /// <param name="normalizedDate">A date converted to a float 0 - 1.</param>
        /// <returns>The mean precipitation for that season.</returns>
        private float GetMeanPrecipitation(ClimatePrefab climatePrefab, float normalizedDate)
        {
            (SeasonInfo, float, float) valueTuple = climatePrefab.FindSeasonByTime(normalizedDate);
            System.Reflection.MethodInfo calculateMeanPrecipitationMethod = m_ClimateSystem.GetType().GetMethod("CalculateMeanPrecipitation", BindingFlags.NonPublic | BindingFlags.Instance);
            object meanPrecipitationObject = calculateMeanPrecipitationMethod.Invoke(m_ClimateSystem, new object[] { climatePrefab, 48, valueTuple.Item2, valueTuple.Item3 });
            float meanPrecipitation = (float)meanPrecipitationObject;
            return meanPrecipitation;
        }

        /// <summary>
        /// Gets the length of a season. It should always be 0.25.
        /// </summary>
        /// <param name="climatePrefab">The prefab for the map's climate.</param>
        /// <param name="normalizedDate">A date converted to a float 0 - 1.</param>
        /// <returns>0.25f.</returns>
        private float GetSeasonLength(ClimatePrefab climatePrefab, float normalizedDate)
        {
            (SeasonInfo, float, float) valueTuple = climatePrefab.FindSeasonByTime(normalizedDate);
            return Math.Abs(valueTuple.Item3 - valueTuple.Item2);
        }

        /// <summary>
        /// Gets the current date as a starting point for evaluating the climate, mean precipitation, and seasonality.
        /// </summary>
        /// <returns>A float equal to the current date between 0 - 1.</returns>
        private float GetClimateDate()
        {
            var climateDateVar = m_ClimateSystem.GetMemberValue("m_Date");
            OverridableProperty<float> climateDate = (OverridableProperty<float>)climateDateVar;
            float normalizedClimateDate = float.Parse(climateDate.ToString(), CultureInfo.InvariantCulture.NumberFormat); // This is a dumb solution but climateDate.value is coming up as 0.
            return normalizedClimateDate;
        }

        private struct TypeHandle
        {
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> __Game_Simulation_WaterSourceData_RW_ComponentTypeHandle;
            public ComponentTypeHandle<SeasonalStreamsData> __Seasonal_Streams_OriginalAmountComponent_RW_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;
            [ReadOnly]
            public EntityTypeHandle __Entity_RO_TypeHandle;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AssignHandles(ref SystemState state)
            {
                __Game_Simulation_WaterSourceData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Simulation.WaterSourceData>();
                __Seasonal_Streams_OriginalAmountComponent_RW_ComponentTypeHandle = state.GetComponentTypeHandle<SeasonalStreamsData>();
                __Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>();
                __Entity_RO_TypeHandle = state.GetEntityTypeHandle();
            }
        }

        /// <summary>
        /// This job sets the stream flow amount based on all the various factors calculated during the onUpdate.
        /// </summary>
        private struct ReviseWaterSourcesJob : IJobChunk
        {
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_SourceType;
            public ComponentTypeHandle<SeasonalStreamsData> m_SeasonalStreamDataType;
            public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;
            public EntityTypeHandle m_EntityType;
            public float m_WaterSourceMultiplier;
            public float m_SnowAccumulationMultiplier;
            public float m_PotentialSnowMeltMultiplier;
            public float m_TemperatureDifferential;
            public TerrainHeightData m_TerrainHeightData;
            public EntityCommandBuffer buffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Game.Objects.Transform> transformNativeArray = chunk.GetNativeArray(ref m_TransformType);
                NativeArray<Game.Simulation.WaterSourceData> waterSourceDataNativeArray = chunk.GetNativeArray(ref m_SourceType);
                NativeArray<SeasonalStreamsData> seasonalStreamDataNativeArray = chunk.GetNativeArray(ref m_SeasonalStreamDataType);
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    float snowMelt = 0f;
                    Game.Simulation.WaterSourceData currentWaterSourceData = waterSourceDataNativeArray[i];
                    Game.Objects.Transform currentTransform = transformNativeArray[i];
                    float3 terrainPosition = new (currentTransform.m_Position.x, TerrainUtils.SampleHeight(ref m_TerrainHeightData, currentTransform.m_Position), currentTransform.m_Position.z);

                    // 500f is completely arbitrary.
                    float temperatureDifferentialAtWaterSource = m_TemperatureDifferential - (terrainPosition.y / 500f);
                    SeasonalStreamsData currentSeasonalStreamData = seasonalStreamDataNativeArray[i];

                    // If snow accumulated add that to seasonal stream data. 
                    if (m_SnowAccumulationMultiplier > 0f)
                    {
                        currentSeasonalStreamData.m_SnowAccumulation += seasonalStreamDataNativeArray[i].m_OriginalAmount * m_SnowAccumulationMultiplier;
                        buffer.SetComponent(entityNativeArray[i], currentSeasonalStreamData);
                    }

                    // Calculate the amount of snow melt at the calculated temperature at the elevation of the water source.
                    if (m_PotentialSnowMeltMultiplier > 0f && currentSeasonalStreamData.m_SnowAccumulation > 0f && temperatureDifferentialAtWaterSource > 0f)
                    {
                        snowMelt = TryMeltSnow(m_PotentialSnowMeltMultiplier, ref currentSeasonalStreamData, temperatureDifferentialAtWaterSource);
                    }

                    // Set the amount.
                    currentWaterSourceData.m_Amount = (seasonalStreamDataNativeArray[i].m_OriginalAmount * m_WaterSourceMultiplier) + snowMelt;
                    buffer.SetComponent(entityNativeArray[i], currentWaterSourceData);
                }
            }

            // Determines a value of snow to melt right now based on the amount of snow and the tepmerature differential.
            private static float TryMeltSnow(float maxMultiplier, ref SeasonalStreamsData data, float temperatureDifferential)
            {
                const float meltingRate = 1f / 30f;
                float maxSnowMelt = Mathf.Min(data.m_OriginalAmount * maxMultiplier, data.m_SnowAccumulation, temperatureDifferential * meltingRate * data.m_OriginalAmount);
                data.m_SnowAccumulation -= maxSnowMelt;
                return maxSnowMelt;
            }
        }
    }
}
