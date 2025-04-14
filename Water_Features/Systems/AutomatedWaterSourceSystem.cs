// <copyright file="AutomatedWaterSourceSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Water_Features.Systems
{
    using Colossal.Logging;
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
    using Water_Features.Tools;

    /// <summary>
    /// A system for handing automated water source.
    /// </summary>
    public partial class AutomatedWaterSourceSystem : GameSystemBase
    {
        private EndFrameBarrier m_EndFrameBarrier;
        private WaterSystem m_WaterSystem;
        private TerrainSystem m_TerrainSystem;
        private EntityQuery m_AutomatedWaterSources;
        private ILog m_Log;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = WaterFeaturesMod.Instance.Log;
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_WaterSystem = World.GetOrCreateSystemManaged<WaterSystem>();
            m_TerrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();
            m_AutomatedWaterSources = GetEntityQuery(new EntityQueryDesc[] {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<Game.Simulation.WaterSourceData>(),
                        ComponentType.ReadOnly<Game.Objects.Transform>(),
                        ComponentType.ReadWrite<AutomatedWaterSource>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Owner>(),
                    },
                },
            });
            m_Log.Info($"[{nameof(AutomatedWaterSourceSystem)}] {nameof(OnCreate)}");
            RequireForUpdate(m_AutomatedWaterSources);
            base.OnCreate();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (m_WaterSystem.WaterSimSpeed == 0)
            {
                return;
            }

            AutomatedWaterSourcesJob automatedWaterSourcesJob = new ()
            {
                buffer = m_EndFrameBarrier.CreateCommandBuffer(),
                m_AutomatedWaterSourceType = SystemAPI.GetComponentTypeHandle<AutomatedWaterSource>(),
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_SourceType = SystemAPI.GetComponentTypeHandle<WaterSourceData>(),
                m_TerrainHeightData = m_TerrainSystem.GetHeightData(false),
                m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out JobHandle waterSurfaceDataJob),
                m_TransformType = SystemAPI.GetComponentTypeHandle<Game.Objects.Transform>(),
            };

            JobHandle jobHandle = JobChunkExtensions.Schedule(automatedWaterSourcesJob, m_AutomatedWaterSources, JobHandle.CombineDependencies(Dependency, waterSurfaceDataJob));
            m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            m_TerrainSystem.AddCPUHeightReader(jobHandle);
            m_WaterSystem.AddSurfaceReader(jobHandle);
            Dependency = jobHandle;
        }

        /// <summary>
        /// This job checks the water level of the automatic filling lake.
        /// At first the autofilling lake is a stream water source that is filling up the lake with water.
        /// When it reaches 75% full then the amount is throttled.
        /// When it reaches 100% full or higher the water source is converted to a lake.
        /// The component for automatic filling is then removed.
        /// </summary>
#if BURST
        [BurstCompile]
#endif
        private struct AutomatedWaterSourcesJob : IJobChunk
        {
            public ComponentTypeHandle<AutomatedWaterSource> m_AutomatedWaterSourceType;
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_SourceType;
            public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;
            public TerrainHeightData m_TerrainHeightData;
            public WaterSurfaceData m_WaterSurfaceData;
            public EntityCommandBuffer buffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Game.Simulation.WaterSourceData> waterSourceDataNativeArray = chunk.GetNativeArray(ref m_SourceType);
                NativeArray<Game.Objects.Transform> transformNativeArray = chunk.GetNativeArray(ref m_TransformType);
                NativeArray<AutomatedWaterSource> automatedWaterSourceNativeArray = chunk.GetNativeArray(ref m_AutomatedWaterSourceType);
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Game.Simulation.WaterSourceData currentWaterSourceData = waterSourceDataNativeArray[i];
                    Game.Objects.Transform currentTransform = transformNativeArray[i];
                    AutomatedWaterSource currentAutomatedWaterSource = automatedWaterSourceNativeArray[i];
                    Entity currentEntity = entityNativeArray[i];
                    float3 terrainPosition = new (currentTransform.m_Position.x, TerrainUtils.SampleHeight(ref m_TerrainHeightData, currentTransform.m_Position), currentTransform.m_Position.z);
                    float3 waterPosition = new (currentTransform.m_Position.x, WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, currentTransform.m_Position), currentTransform.m_Position.z);
                    float waterHeight = waterPosition.y;
                    float waterDepth = waterPosition.y - terrainPosition.y;
                    float maxDepth = currentAutomatedWaterSource.m_MaximumWaterHeight - terrainPosition.y;

                    float4 previousWaterHeights = currentAutomatedWaterSource.m_PreviousWaterHeights;
                    float totalPreviousWaterHeight = 0;
                    for (int j = 0; j < 4; j++)
                    {
                        if (j < 3)
                        {
                            currentAutomatedWaterSource.m_PreviousWaterHeights[j] = currentAutomatedWaterSource.m_PreviousWaterHeights[j + 1];
                        }
                        else
                        {
                            currentAutomatedWaterSource.m_PreviousWaterHeights[j] = waterHeight;
                        }

                        totalPreviousWaterHeight += previousWaterHeights[j];
                    }

                    float averagePreviousWaterHeight = totalPreviousWaterHeight / 4f;
                    float rateOfChange = (previousWaterHeights.w - previousWaterHeights.x) / 4f;
                    float fillDepth = maxDepth - waterDepth;

                    // When it reaches 100% full or higher the water source is converted to a lake.
                    if (averagePreviousWaterHeight > currentAutomatedWaterSource.m_MaximumWaterHeight - 0.1f)
                    {
                        currentWaterSourceData.m_ConstantDepth = (int)WaterToolUISystem.SourceType.VanillaLake;
                        currentWaterSourceData.m_Amount = currentAutomatedWaterSource.m_MaximumWaterHeight;
                        buffer.SetComponent(currentEntity, currentWaterSourceData);
                    }

                    // When it reaches 75% full then the amount is throttled.
                    else
                    {
                        currentWaterSourceData.m_ConstantDepth = 0; // Stream
                        if (rateOfChange > 0 && fillDepth / rateOfChange < 40f)
                        {
                            currentWaterSourceData.m_Amount *= rateOfChange / fillDepth;
                            currentWaterSourceData.m_Amount = Mathf.Min(currentWaterSourceData.m_Amount, 0);
                        }
                        else
                        {
                            currentWaterSourceData.m_Amount += fillDepth * currentWaterSourceData.m_Radius * 0.00001f;
                        }

                        buffer.SetComponent(currentEntity, currentWaterSourceData);
                    }

                }
            }
        }
    }
}
