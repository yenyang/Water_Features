// <copyright file="DetentionBasinSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Water_Features.Systems
{
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Common;
    using Game.Simulation;
    using Game.Tools;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using Water_Features.Components;

    /// <summary>
    /// A system for controlling the water output of detention basins.
    /// </summary>
    public partial class DetentionBasinSystem : GameSystemBase
    {
        public static readonly int kUpdatesPerDay = 128;
        private EndFrameBarrier m_EndFrameBarrier;
        private ClimateSystem m_ClimateSystem;
        private WaterSystem m_WaterSystem;
        private TerrainSystem m_TerrainSystem;
        private EntityQuery m_DetentionBasinQuery;
        private ILog m_Log;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetentionBasinSystem"/> class.
        /// </summary>
        public DetentionBasinSystem()
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
            m_ClimateSystem = World.GetOrCreateSystemManaged<ClimateSystem>();
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_WaterSystem = World.GetOrCreateSystemManaged<WaterSystem>();
            m_TerrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();
            m_DetentionBasinQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<Game.Simulation.WaterSourceData>(),
                        ComponentType.ReadOnly<Game.Objects.Transform>(),
                        ComponentType.ReadWrite<DetentionBasin>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Owner>(),
                    },
                },
            });
            RequireForUpdate(m_DetentionBasinQuery);
            m_Log.Info($"[{nameof(DetentionBasinSystem)}] {nameof(OnCreate)}");
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (m_WaterSystem.WaterSimSpeed == 0)
            {
                return;
            }

            DetentionBasinJob detentionBasinJob = new()
            {
                m_DetentionBasinType = SystemAPI.GetComponentTypeHandle<DetentionBasin>(),
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_SourceType = SystemAPI.GetComponentTypeHandle<Game.Simulation.WaterSourceData>(),
                m_TerrainHeightData = m_TerrainSystem.GetHeightData(false),
                m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out JobHandle waterSurfaceDataJob),
                m_TransformType = SystemAPI.GetComponentTypeHandle<Game.Objects.Transform>(),
                buffer = m_EndFrameBarrier.CreateCommandBuffer(),
                m_Precipiation = m_ClimateSystem.precipitation,
                m_Snowing = m_ClimateSystem.isSnowing,
                m_TemperatureDifferential = m_ClimateSystem.temperature.value - m_ClimateSystem.freezingTemperature,
            };
            JobHandle jobHandle = JobChunkExtensions.Schedule(detentionBasinJob, m_DetentionBasinQuery, JobHandle.CombineDependencies(waterSurfaceDataJob, Dependency));
            m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            m_TerrainSystem.AddCPUHeightReader(jobHandle);
            m_WaterSystem.AddSurfaceReader(jobHandle);
            Dependency = jobHandle;
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (!WaterFeaturesMod.Instance.Settings.IncludeDetentionBasins)
            {
                Enabled = false;
                return;
            }
        }

        /// <summary>
        /// This job checks the water level of detention basins.
        /// Detention basins only fill up with precipitation or melting snow.
        /// When it reaches 95% full then the amount is throttled.
        /// When it reaches 100% full or higher the amount is set to 0.
        /// </summary>
#if BURST
        [BurstCompile]
#endif
        private struct DetentionBasinJob : IJobChunk
        {
            public ComponentTypeHandle<DetentionBasin> m_DetentionBasinType;
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_SourceType;
            public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;
            public TerrainHeightData m_TerrainHeightData;
            public WaterSurfaceData m_WaterSurfaceData;
            public EntityCommandBuffer buffer;
            public float m_Precipiation;
            public bool m_Snowing;
            public float m_TemperatureDifferential;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Game.Simulation.WaterSourceData> waterSourceDataNativeArray = chunk.GetNativeArray(ref m_SourceType);
                NativeArray<Game.Objects.Transform> transformNativeArray = chunk.GetNativeArray(ref m_TransformType);
                NativeArray<DetentionBasin> detentionBasinNativeArray = chunk.GetNativeArray(ref m_DetentionBasinType);
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                float maxDepthToRunoffCoefficient = 0.1f;
                for (int i = 0; i < chunk.Count; i++)
                {
                    Game.Simulation.WaterSourceData currentWaterSourceData = waterSourceDataNativeArray[i];
                    Game.Objects.Transform currentTransform = transformNativeArray[i];
                    DetentionBasin currentDetentionBasin = detentionBasinNativeArray[i];
                    Entity currentEntity = entityNativeArray[i];
                    float3 terrainPosition = new (currentTransform.m_Position.x, TerrainUtils.SampleHeight(ref m_TerrainHeightData, currentTransform.m_Position), currentTransform.m_Position.z);
                    float3 waterPosition = new (currentTransform.m_Position.x, WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, currentTransform.m_Position), currentTransform.m_Position.z);
                    float waterHeight = waterPosition.y;
                    float waterDepth = waterPosition.y - terrainPosition.y;
                    float maximumDepth = currentDetentionBasin.m_MaximumWaterHeight - terrainPosition.y;
                    float temperatureDifferentialAtWaterSource = m_TemperatureDifferential - (terrainPosition.y / 500f);

                    // This resets the water source back to being a stream if it was converted to a lake for safe saving.
                    if (currentWaterSourceData.m_ConstantDepth != 0) // Stream
                    {
                        currentWaterSourceData.m_ConstantDepth = 0; // Stream
                    }

                    // If it's snowing add snow accumulation.
                    if (m_Precipiation > 0f && m_Snowing)
                    {
                        currentDetentionBasin.m_SnowAccumulation += m_Precipiation * maximumDepth * maxDepthToRunoffCoefficient;
                        buffer.SetComponent(currentEntity, currentDetentionBasin);
                    }

                    // When it reaches 100% full or higher the amount is set to 0.
                    if (waterHeight > currentDetentionBasin.m_MaximumWaterHeight && currentWaterSourceData.m_Amount >= 0f)
                    {
                        currentWaterSourceData.m_Amount = 0f;
                        buffer.SetComponent(currentEntity, currentWaterSourceData);
                    }
                    else if (m_Precipiation > 0f && !m_Snowing) // If it's not full and it's raining add water.
                    {
                        // If there is no snow to melt than just simulate rain.
                        if (Mathf.Approximately(currentDetentionBasin.m_SnowAccumulation, 0f) && temperatureDifferentialAtWaterSource > 0f)
                        {
                            currentWaterSourceData.m_Amount = m_Precipiation * maximumDepth * maxDepthToRunoffCoefficient;
                        }

                        // If there is snow that is melting add that to the amount.
                        else
                        {
                            currentWaterSourceData.m_Amount = (m_Precipiation * maximumDepth * maxDepthToRunoffCoefficient) + TryMeltSnow(ref currentDetentionBasin, temperatureDifferentialAtWaterSource, maximumDepth);
                            buffer.SetComponent(currentEntity, currentDetentionBasin);
                        }

                        // When it reaches 95% full then the amount is throttled.
                        if (waterDepth > 0.95f * maximumDepth)
                        {
                            currentWaterSourceData.m_Amount = Mathf.Min(currentWaterSourceData.m_Amount, maximumDepth * .05f);
                        }

                        buffer.SetComponent(currentEntity, currentWaterSourceData);
                    }

                    // If it is not raining, but their is snow that can melt.
                    else if (m_Precipiation == 0f && temperatureDifferentialAtWaterSource > 0f && currentDetentionBasin.m_SnowAccumulation > 0f)
                    {
                        currentWaterSourceData.m_Amount = TryMeltSnow(ref currentDetentionBasin, temperatureDifferentialAtWaterSource, maximumDepth); // If there is snow that is melting add that to the amount.

                        if (waterDepth > 0.95f * maximumDepth) // When it reaches 95% full then the amount is throttled.
                        {
                            currentWaterSourceData.m_Amount = Mathf.Min(currentWaterSourceData.m_Amount, maximumDepth * .05f);
                        }

                        buffer.SetComponent(currentEntity, currentDetentionBasin);
                    }
                    else
                    {
                        currentWaterSourceData.m_Amount = 0f;
                        buffer.SetComponent(currentEntity, currentWaterSourceData);
                    }
                }
            }

            // Determines a value of snow to melt right now based on the amount of snow and the tepmerature differential.
            private float TryMeltSnow(ref DetentionBasin data, float temperatureDifferential, float maxDepth)
            {
                const float meltingRate = 1f / 30f;
                float maxHeightToRunoffCoefficient = 0.1f;
                float maxSnowMelt = GetMinimum(data.m_SnowAccumulation, temperatureDifferential * meltingRate * maxDepth * maxHeightToRunoffCoefficient);
                data.m_SnowAccumulation -= maxSnowMelt;
                return maxSnowMelt;
            }

            // Determines the minimum between two floats.
            private float GetMinimum(float one, float two)
            {
                if (one < two)
                {
                    return one;
                }

                return two;
            }
        }
    }
}
