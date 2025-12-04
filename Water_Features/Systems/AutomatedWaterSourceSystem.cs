// <copyright file="AutomatedWaterSourceSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Water_Features.Systems
{
    using Colossal.Logging;
    using Colossal.Mathematics;
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
        /// <summary>
        /// Used to calcuate how many times this system runs during a simulated game day.
        /// </summary>
        public static readonly int UpdatesPerDay = 1024;
        private EndFrameBarrier m_EndFrameBarrier;
        private WaterSystem m_WaterSystem;
        private TerrainSystem m_TerrainSystem;
        private EntityQuery m_AutomatedWaterSources;
        private ILog m_Log;

        /// <inheritdoc/>
        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 / UpdatesPerDay;
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = WaterFeaturesMod.Instance.Log;
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_WaterSystem = World.GetOrCreateSystemManaged<WaterSystem>();
            m_TerrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();
            m_AutomatedWaterSources = GetEntityQuery(new EntityQueryDesc[]
            {
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
            public WaterSurfaceData<Game.Simulation.SurfaceWater> m_WaterSurfaceData;
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
                    if (averagePreviousWaterHeight > currentAutomatedWaterSource.m_MaximumWaterHeight)
                    {
                        if (currentAutomatedWaterSource.m_PreviousFlowRate == 0)
                        {
                            currentAutomatedWaterSource.m_PreviousFlowRate = currentWaterSourceData.m_Height;
                        }

                        if (currentWaterSourceData.m_Radius >= 500f &&
                            IsPositionNearBorder(currentTransform.m_Position, currentWaterSourceData.m_Radius, false))
                        {
                            currentWaterSourceData.m_ConstantDepth = (int)WaterToolUISystem.SourceType.Sea;
                        }
                        else if (IsPositionNearBorder(currentTransform.m_Position, currentWaterSourceData.m_Radius, true))
                        {
                            currentWaterSourceData.m_ConstantDepth = (int)WaterToolUISystem.SourceType.River;
                        }
                        else
                        {
                            currentWaterSourceData.m_ConstantDepth = (int)WaterToolUISystem.SourceType.Lake;
                        }

                        currentWaterSourceData.m_Height = currentAutomatedWaterSource.m_MaximumWaterHeight;
                        buffer.SetComponent(currentEntity, currentWaterSourceData);
                    }
                    else
                    {
                        if (currentWaterSourceData.m_ConstantDepth != 0)
                        {
                            currentWaterSourceData.m_Height = 0.975f * currentAutomatedWaterSource.m_PreviousFlowRate;
                            currentAutomatedWaterSource.m_PreviousFlowRate = 0f;
                            currentWaterSourceData.m_ConstantDepth = 0; // Stream
                        }

                        currentWaterSourceData.m_Height += fillDepth * currentWaterSourceData.m_Radius * 0.00001f * Mathf.Pow(10f, Mathf.Max(Mathf.Round(Mathf.Log10(currentWaterSourceData.m_Height)), 1));

                        buffer.SetComponent(currentEntity, currentWaterSourceData);
                    }

                    buffer.SetComponent(currentEntity, currentAutomatedWaterSource);
                }
            }

            /// <summary>
            /// A method for determining if a position is close to the border.
            /// </summary>
            /// <param name="pos">Position to be checked.</param>
            /// <param name="radius">Tolerance for acceptable position.</param>
            /// <param name="fixedMaxDistance">Should the radius be checked for a maximum.</param>
            /// <returns>True if within proximity of border.</returns>
            private bool IsPositionNearBorder(float3 pos, float radius, bool fixedMaxDistance)
            {
                Bounds3 terrainBounds = TerrainUtils.GetBounds(ref m_TerrainHeightData);
                if (fixedMaxDistance)
                {
                    radius = Mathf.Max(150f, radius * 2f / 3f);
                }

                if (Mathf.Abs(terrainBounds.max.x - Mathf.Abs(pos.x)) < radius || Mathf.Abs(terrainBounds.max.z - Mathf.Abs(pos.z)) < radius)
                {
                    return true;
                }

                return false;
            }

            /// <summary>
            /// A method for determining if a position is within the border.
            /// </summary>
            /// <param name="pos">Position to be checked.</param>
            /// <returns>True if within the border. False if not.</returns>
            private bool IsPositionWithinBorder(float3 pos)
            {
                Bounds3 terrainBounds = TerrainUtils.GetBounds(ref m_TerrainHeightData);
                if (Mathf.Max(Mathf.Abs(pos.x), Mathf.Abs(pos.z)) < terrainBounds.max.x)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
