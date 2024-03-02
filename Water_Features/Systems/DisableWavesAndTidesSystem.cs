// <copyright file="DisableWavesAndTidesSystem.cs" company="Yenyang's Mods. MIT License">
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
    using Water_Features.Components;

    /// <summary>
    /// A system that disables waves and tides and removes relevant components.
    /// </summary>
    public partial class DisableWavesAndTidesSystem : GameSystemBase
    {
        private TypeHandle __TypeHandle;
        private EntityQuery m_TidesAndWavesDataQuery;
        private TidesAndWavesSystem m_TidesAndWavesSystem;
        private ILog m_Log;
        private EndFrameBarrier m_EndFrameBarrier;
        private WaterSystem m_WaterSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisableWavesAndTidesSystem"/> class.
        /// </summary>
        public DisableWavesAndTidesSystem()
        {
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = WaterFeaturesMod.Instance.Log;
            m_TidesAndWavesSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TidesAndWavesSystem>();
            m_WaterSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<WaterSystem>();
            m_TidesAndWavesDataQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<Game.Simulation.WaterSourceData>(),
                        ComponentType.ReadOnly<TidesAndWavesData>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                    },
                },
            });
            m_EndFrameBarrier = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<EndFrameBarrier>();
            RequireForUpdate(m_TidesAndWavesDataQuery);
            m_Log.Info($"[{nameof(DisableWavesAndTidesSystem)}] {nameof(OnCreate)}");
            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            __TypeHandle.__Game_Simulation_WaterSourceData_RW_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__TidesAndWavesData_RO_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Entity_RO_TypeHandle.Update(ref CheckedStateRef);
            m_TidesAndWavesSystem.ResetDummySeaWaterSource();
            ResetTidesAndWavesJob resetTidesAndWavesJob = new ()
            {
                m_OriginalAmountType = __TypeHandle.__TidesAndWavesData_RO_ComponentTypeHandle,
                m_SourceType = __TypeHandle.__Game_Simulation_WaterSourceData_RW_ComponentTypeHandle,
                m_EntityType = __TypeHandle.__Entity_RO_TypeHandle,
                buffer = m_EndFrameBarrier.CreateCommandBuffer(),
            };
            Dependency = JobChunkExtensions.Schedule(resetTidesAndWavesJob, m_TidesAndWavesDataQuery, Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(Dependency);
            if (m_WaterSystem.WaterSimSpeed == 0)
            {
                m_WaterSystem.WaterSimSpeed = 1;
            }

            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            __TypeHandle.AssignHandles(ref CheckedStateRef);
        }


        private struct TypeHandle
        {
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> __Game_Simulation_WaterSourceData_RW_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<TidesAndWavesData> __TidesAndWavesData_RO_ComponentTypeHandle;
            [ReadOnly]
            public EntityTypeHandle __Entity_RO_TypeHandle;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AssignHandles(ref SystemState state)
            {
                __Game_Simulation_WaterSourceData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Simulation.WaterSourceData>();
                __TidesAndWavesData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TidesAndWavesData>();
                __Entity_RO_TypeHandle = state.GetEntityTypeHandle();
            }
        }


        /// <summary>
        /// This job sets the amounts for sea water sources back to original amount and removes the waves and tides component.
        /// </summary>
        private struct ResetTidesAndWavesJob : IJobChunk
        {
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_SourceType;
            [ReadOnly]
            public ComponentTypeHandle<TidesAndWavesData> m_OriginalAmountType;
            public EntityCommandBuffer buffer;
            public EntityTypeHandle m_EntityType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Game.Simulation.WaterSourceData> waterSourceDataNativeArray = chunk.GetNativeArray(ref m_SourceType);
                NativeArray<TidesAndWavesData> originalAmountNativeArray = chunk.GetNativeArray(ref m_OriginalAmountType);
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Game.Simulation.WaterSourceData currentWaterSourceData = waterSourceDataNativeArray[i];
                    currentWaterSourceData.m_Amount = originalAmountNativeArray[i].m_OriginalAmount;
                    buffer.SetComponent(entityNativeArray[i], currentWaterSourceData);
                    buffer.RemoveComponent<TidesAndWavesData>(entityNativeArray[i]);
                }
            }
        }
    }
}
