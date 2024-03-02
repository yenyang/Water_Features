// <copyright file="AutofillingLakesSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Systems
{
    using System.Runtime.CompilerServices;
    using Colossal.Logging;
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
    using Water_Features.Tools;

    /// <summary>
    /// A system for handing autofilling lakes custom water sources.
    /// </summary>
    public partial class AutofillingLakesSystem : GameSystemBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutofillingLakesSystem"/> class.
        /// </summary>
        public AutofillingLakesSystem()
        {
        }

        private TypeHandle __TypeHandle;
        private EndFrameBarrier m_EndFrameBarrier;
        private WaterSystem m_WaterSystem;
        private TerrainSystem m_TerrainSystem;
        private EntityQuery m_AutofillingLakesQuery;
        private ILog m_Log;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = WaterFeaturesMod.Instance.Log;
            m_EndFrameBarrier = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_WaterSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<WaterSystem>();
            m_TerrainSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TerrainSystem>();
            m_AutofillingLakesQuery = GetEntityQuery(new EntityQueryDesc[] {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<Game.Simulation.WaterSourceData>(),
                        ComponentType.ReadOnly<Game.Objects.Transform>(),
                        ComponentType.ReadWrite<AutofillingLake>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Owner>(),
                    },
                },
            });
            m_Log.Info($"[{nameof(AutofillingLakesSystem)}] {nameof(OnCreate)}");
            RequireForUpdate(m_AutofillingLakesQuery);
            base.OnCreate();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            __TypeHandle.__AutofillingLake_RW_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Simulation_WaterSourceData_RW_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Simulation_WaterSourceData_RW_ComponentTypeHandle.Update(ref CheckedStateRef);
            AutofillingLakesJob autofillingLakesJob = new ()
            {
                buffer = m_EndFrameBarrier.CreateCommandBuffer(),
                m_AutofillingLakeType = __TypeHandle.__AutofillingLake_RW_ComponentTypeHandle,
                m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_SourceType = __TypeHandle.__Game_Simulation_WaterSourceData_RW_ComponentTypeHandle,
                m_TerrainHeightData = m_TerrainSystem.GetHeightData(false),
                m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out JobHandle waterSurfaceDataJob),
                m_TransformType = __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle,
            };

            JobHandle jobHandle = JobChunkExtensions.Schedule(autofillingLakesJob, m_AutofillingLakesQuery, JobHandle.CombineDependencies(Dependency, waterSurfaceDataJob));
            m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            m_TerrainSystem.AddCPUHeightReader(jobHandle);
            m_WaterSystem.AddSurfaceReader(jobHandle);
            Dependency = jobHandle;
        }

        /// <inheritdoc/>
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            __TypeHandle.AssignHandles(ref CheckedStateRef);
        }

        /// <summary>
        /// This job checks the water level of the automatic filling lake.
        /// At first the autofilling lake is a stream water source that is filling up the lake with water.
        /// When it reaches 75% full then the amount is throttled.
        /// When it reaches 100% full or higher the water source is converted to a lake.
        /// The component for automatic filling is then removed.
        /// </summary>
        private struct AutofillingLakesJob : IJobChunk
        {
            public ComponentTypeHandle<AutofillingLake> m_AutofillingLakeType;
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
                NativeArray<AutofillingLake> autofillingLakesNativeArray = chunk.GetNativeArray(ref m_AutofillingLakeType);
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Game.Simulation.WaterSourceData currentWaterSourceData = waterSourceDataNativeArray[i];
                    Game.Objects.Transform currentTransform = transformNativeArray[i];
                    AutofillingLake currentAutofillingLake = autofillingLakesNativeArray[i];
                    Entity currentEntity = entityNativeArray[i];
                    float3 terrainPosition = new (currentTransform.m_Position.x, TerrainUtils.SampleHeight(ref m_TerrainHeightData, currentTransform.m_Position), currentTransform.m_Position.z);
                    float3 waterPosition = new (currentTransform.m_Position.x, WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, currentTransform.m_Position), currentTransform.m_Position.z);
                    float waterHeight = waterPosition.y;
                    float waterDepth = waterPosition.y - terrainPosition.y;
                    float maxDepth = currentAutofillingLake.m_MaximumWaterHeight - terrainPosition.y;

                    // When it reaches 100% full or higher the water source is converted to a lake.
                    if (waterHeight > currentAutofillingLake.m_MaximumWaterHeight)
                    {
                        currentWaterSourceData.m_ConstantDepth = (int)WaterToolUISystem.SourceType.VanillaLake;
                        currentWaterSourceData.m_Amount = currentAutofillingLake.m_MaximumWaterHeight;
                        buffer.SetComponent(currentEntity, currentWaterSourceData);
                        buffer.RemoveComponent<AutofillingLake>(currentEntity);
                    }

                    // When it reaches 75% full then the amount is throttled.
                    else if (waterDepth >= 0.75f * maxDepth)
                    {
                        currentWaterSourceData.m_ConstantDepth = 0; // Stream
                        currentWaterSourceData.m_Amount = maxDepth * 0.1f;
                        if (currentWaterSourceData.m_Radius < 20f)
                        {
                            currentWaterSourceData.m_Amount *= Mathf.Pow(currentWaterSourceData.m_Radius / 20f, 2);
                        }

                        if (currentWaterSourceData.m_ConstantDepth != 0) // Stream
                        {
                            currentWaterSourceData.m_ConstantDepth = 0; // Stream
                        }

                        buffer.SetComponent(currentEntity, currentWaterSourceData);
                    }

                    // If an automatic filling lake was saved and converted to a vanilla lake, then this converts it back into a stream to continue filling.
                    else if (currentWaterSourceData.m_ConstantDepth != 0) // Stream
                    {
                        currentWaterSourceData.m_ConstantDepth = 0; // Stream
                        currentWaterSourceData.m_Amount = maxDepth * 0.4f;
                        if (currentWaterSourceData.m_Radius < 20f)
                        {
                            currentWaterSourceData.m_Amount *= Mathf.Pow(currentWaterSourceData.m_Radius / 20f, 2);
                        }

                        buffer.SetComponent(currentEntity, currentWaterSourceData);
                    }

                }
            }
        }

        private struct TypeHandle
        {
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> __Game_Simulation_WaterSourceData_RW_ComponentTypeHandle;
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            public ComponentTypeHandle<AutofillingLake> __AutofillingLake_RW_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AssignHandles(ref SystemState state)
            {
                __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                __Game_Simulation_WaterSourceData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Simulation.WaterSourceData>();
                __AutofillingLake_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AutofillingLake>();
                __Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>();
            }
        }
    }
}
