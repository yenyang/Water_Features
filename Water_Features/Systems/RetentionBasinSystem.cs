// <copyright file="RetentionBasinSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Systems
{
    using System.Runtime.CompilerServices;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Common;
    using Game.Simulation;
    using Game.Tools;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using Water_Features.Components;

    /// <summary>
    /// A system for handling retention basin custom water sources.
    /// </summary>
    public partial class RetentionBasinSystem : GameSystemBase
    {
        public static readonly int kUpdatesPerDay = 128;

        private TypeHandle __TypeHandle;
        private EndFrameBarrier m_EndFrameBarrier;
        private ClimateSystem m_ClimateSystem;
        private WaterSystem m_WaterSystem;
        private TerrainSystem m_TerrainSystem;
        private EntityQuery m_RetentionBasinQuery;
        private ILog m_Log;


        /// <summary>
        /// Initializes a new instance of the <see cref="RetentionBasinSystem"/> class.
        /// </summary>
        public RetentionBasinSystem()
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
            m_EndFrameBarrier = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_WaterSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<WaterSystem>();
            m_TerrainSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TerrainSystem>();
            m_RetentionBasinQuery = GetEntityQuery(new EntityQueryDesc[] 
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<Game.Simulation.WaterSourceData>(),
                        ComponentType.ReadOnly<Game.Objects.Transform>(),
                        ComponentType.ReadWrite<RetentionBasin>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Owner>(),
                    },
                },
            });
            RequireForUpdate(m_RetentionBasinQuery);
            m_Log.Info($"[{nameof(RetentionBasinSystem)}] {nameof(OnCreate)}");
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            __TypeHandle.__RetentionBasin_RW_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Simulation_WaterSourceData_RW_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Simulation_WaterSourceData_RW_ComponentTypeHandle.Update(ref CheckedStateRef);
            RetentionBasinJob retentionBasinJob = new ()
            {
                m_RetentionBasinType = __TypeHandle.__RetentionBasin_RW_ComponentTypeHandle,
                m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_SourceType = __TypeHandle.__Game_Simulation_WaterSourceData_RW_ComponentTypeHandle,
                m_TerrainHeightData = m_TerrainSystem.GetHeightData(false),
                m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out JobHandle waterSurfaceDataJob),
                m_TransformType = __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle,
                buffer = m_EndFrameBarrier.CreateCommandBuffer(),
                m_Precipiation = m_ClimateSystem.precipitation,
                m_Snowing = m_ClimateSystem.isSnowing,
                m_TemperatureDifferential = m_ClimateSystem.temperature.value - m_ClimateSystem.freezingTemperature,
            };
            JobHandle jobHandle = JobChunkExtensions.Schedule(retentionBasinJob, m_RetentionBasinQuery, JobHandle.CombineDependencies(waterSurfaceDataJob, Dependency));
            m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;
        }

        /// <inheritdoc/>
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            __TypeHandle.AssignHandles(ref CheckedStateRef);
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (!WaterFeaturesMod.Settings.IncludeRetentionBasins)
            {
                Enabled = false;
                return;
            }
        }

        private struct TypeHandle
        {
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> __Game_Simulation_WaterSourceData_RW_ComponentTypeHandle;
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            public ComponentTypeHandle<RetentionBasin> __RetentionBasin_RW_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AssignHandles(ref SystemState state)
            {
                __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                __Game_Simulation_WaterSourceData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Simulation.WaterSourceData>();
                __RetentionBasin_RW_ComponentTypeHandle = state.GetComponentTypeHandle<RetentionBasin>();
                __Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>();
            }
        }

        /// <summary>
        /// This job checks the water level of retention basins.
        /// Retention basins have a minimum water level and fill up with precipitation or melting snow.
        /// When it reaches 95% full then the amount is throttled.
        /// When it reaches 100% full or higher the amount is set to 0.
        /// </summary>
        private struct RetentionBasinJob : IJobChunk
        {
            public ComponentTypeHandle<RetentionBasin> m_RetentionBasinType;
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
                NativeArray<RetentionBasin> retentionBasinNativeArray = chunk.GetNativeArray(ref m_RetentionBasinType);
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                float maxDepthToRunoffCoefficient = 0.1f;
                for (int i = 0; i < chunk.Count; i++)
                {
                    Game.Simulation.WaterSourceData currentWaterSourceData = waterSourceDataNativeArray[i];
                    Game.Objects.Transform currentTransform = transformNativeArray[i];
                    RetentionBasin currentRetentionBasin = retentionBasinNativeArray[i];
                    Entity currentEntity = entityNativeArray[i];
                    float3 terrainPosition = new (currentTransform.m_Position.x, TerrainUtils.SampleHeight(ref m_TerrainHeightData, currentTransform.m_Position), currentTransform.m_Position.z);
                    float3 waterPosition = new (currentTransform.m_Position.x, WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, currentTransform.m_Position), currentTransform.m_Position.z);
                    float waterHeight = waterPosition.y;
                    float waterDepth = waterPosition.y - terrainPosition.y;
                    float minDepth = currentRetentionBasin.m_MinimumWaterHeight - terrainPosition.y;
                    float maxDepth = currentRetentionBasin.m_MaximumWaterHeight - terrainPosition.y;
                    float temperatureDifferentialAtWaterSource = m_TemperatureDifferential - (terrainPosition.y / 500f);

                    // This resets the water source back to being a stream if it was converted to a vanilla lake for safe saving.
                    if (currentWaterSourceData.m_ConstantDepth != 0) // Stream
                    {
                        currentWaterSourceData.m_ConstantDepth = 0; // Stream
                    }

                    // If it's snowing add snow accumulation.
                    if (m_Precipiation > 0f && m_Snowing)
                    {
                        currentRetentionBasin.m_SnowAccumulation += m_Precipiation * maxDepth * maxDepthToRunoffCoefficient;
                        buffer.SetComponent(currentEntity, currentRetentionBasin);
                    }


                    // When it reaches 100% full or higher the amount is set to 0.
                    if (waterHeight > currentRetentionBasin.m_MaximumWaterHeight && currentWaterSourceData.m_Amount >= 0f)
                    {
                        currentWaterSourceData.m_Amount = 0f;
                        buffer.SetComponent(currentEntity, currentWaterSourceData);
                    }

                    // If the water depth is less than 95% of the minimum depth than set the amount of water.
                    else if (waterDepth < 0.95 * minDepth && currentRetentionBasin.m_MinimumWaterHeight < currentRetentionBasin.m_MaximumWaterHeight)
                    {
                        currentWaterSourceData.m_Amount = minDepth * maxDepthToRunoffCoefficient;
                        buffer.SetComponent(currentEntity, currentWaterSourceData);
                    }
                    else if (m_Precipiation > 0f && !m_Snowing) // If it's not full and it's raining add water.
                    {
                        // If there is no snow to melt than just simulate rain.
                        if (Mathf.Approximately(currentRetentionBasin.m_SnowAccumulation, 0f) && temperatureDifferentialAtWaterSource > 0f)
                        {
                            currentWaterSourceData.m_Amount = m_Precipiation * maxDepth * maxDepthToRunoffCoefficient;
                        }

                        // If there is snow that is melting add that to the amount.
                        else
                        {
                            currentWaterSourceData.m_Amount = (m_Precipiation * maxDepth * maxDepthToRunoffCoefficient) + TryMeltSnow(ref currentRetentionBasin, temperatureDifferentialAtWaterSource, maxDepth);
                            buffer.SetComponent(currentEntity, currentRetentionBasin);
                        }

                        // When it reaches 95% full then the amount is throttled.
                        if (waterHeight > 0.95f * currentRetentionBasin.m_MaximumWaterHeight)
                        {
                            currentWaterSourceData.m_Amount = Mathf.Min(currentWaterSourceData.m_Amount, maxDepth * .05f);
                        }

                        buffer.SetComponent(currentEntity, currentWaterSourceData);
                    }

                    // If it is not raining, but their is snow that can melt.
                    else if (m_Precipiation == 0f && temperatureDifferentialAtWaterSource > 0f && currentRetentionBasin.m_SnowAccumulation > 0f)
                    {
                        currentWaterSourceData.m_Amount = TryMeltSnow(ref currentRetentionBasin, temperatureDifferentialAtWaterSource, maxDepth); // If there is snow that is melting add that to the amount.

                        if (waterDepth > 0.95f * maxDepth) // When it reaches 95% full then the amount is throttled.
                        {
                            currentWaterSourceData.m_Amount = Mathf.Min(currentWaterSourceData.m_Amount, maxDepth * .05f);
                        }

                        buffer.SetComponent(currentEntity, currentRetentionBasin);
                    }
                    else
                    {
                        currentWaterSourceData.m_Amount = 0f;
                        buffer.SetComponent(currentEntity, currentWaterSourceData);
                    }
                }
            }

            private static float TryMeltSnow(ref RetentionBasin data, float temperatureDifferential, float maxDepth)
            {
                const float meltingRate = 1f / 30f;
                float maxHeightToRunoffCoefficient = 0.1f;
                float maxSnowMelt = Mathf.Min(data.m_SnowAccumulation, temperatureDifferential * meltingRate * maxDepth * maxHeightToRunoffCoefficient);
                data.m_SnowAccumulation -= maxSnowMelt;
                return maxSnowMelt;
            }
        }
    }
}
