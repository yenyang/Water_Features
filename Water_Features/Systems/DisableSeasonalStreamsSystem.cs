// <copyright file="DisableSeasonalStreamsSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Systems
{
    using System.Runtime.CompilerServices;
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Tools;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Water_Features.Components;

    /// <summary>
    /// A system that will disable seasonal streams and remove related components.
    /// </summary>
    public partial class DisableSeasonalStreamSystem : GameSystemBase
    {
        private TypeHandle __TypeHandle;
        private EntityQuery m_SeasonalStreamsDataQuery;
        private ILog m_Log;
        private EndFrameBarrier m_EndFrameBarrier;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisableSeasonalStreamSystem"/> class.
        /// </summary>
        public DisableSeasonalStreamSystem()
        {
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = WaterFeaturesMod.Instance.Log;
            m_EndFrameBarrier = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_SeasonalStreamsDataQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<Game.Simulation.WaterSourceData>(),
                        ComponentType.ReadOnly<SeasonalStreamsData>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                    },
                },
            });
            RequireForUpdate(m_SeasonalStreamsDataQuery);
            m_Log.Info($"[{nameof(DisableSeasonalStreamSystem)}] {nameof(OnCreate)}");
            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            __TypeHandle.__Game_Simulation_WaterSourceData_RW_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Seasonal_Streams_OriginalAmountComponent_RO_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Entity_RO_TypeHandle.Update(ref CheckedStateRef);
            ResetSeasonalStreamsJob resetSeasonalStreamsJob = new ()
            {
                m_OriginalAmountType = __TypeHandle.__Seasonal_Streams_OriginalAmountComponent_RO_ComponentTypeHandle,
                m_SourceType = __TypeHandle.__Game_Simulation_WaterSourceData_RW_ComponentTypeHandle,
                m_EntityType = __TypeHandle.__Entity_RO_TypeHandle,
                buffer = m_EndFrameBarrier.CreateCommandBuffer(),
            };
            Dependency = JobChunkExtensions.Schedule(resetSeasonalStreamsJob, m_SeasonalStreamsDataQuery, Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(Dependency);
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
            public ComponentTypeHandle<SeasonalStreamsData> __Seasonal_Streams_OriginalAmountComponent_RO_ComponentTypeHandle;
            [ReadOnly]
            public EntityTypeHandle __Entity_RO_TypeHandle;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AssignHandles(ref SystemState state)
            {
                __Game_Simulation_WaterSourceData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Simulation.WaterSourceData>();
                __Seasonal_Streams_OriginalAmountComponent_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SeasonalStreamsData>();
                __Entity_RO_TypeHandle = state.GetEntityTypeHandle();
            }
        }

        /// <summary>
        /// This job sets the amounts for seasonal stream water sources back to original amount and removes the seasonal streams component.
        /// </summary>
        private struct ResetSeasonalStreamsJob : IJobChunk
        {
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_SourceType;
            [ReadOnly]
            public ComponentTypeHandle<SeasonalStreamsData> m_OriginalAmountType;
            public EntityCommandBuffer buffer;
            public EntityTypeHandle m_EntityType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Game.Simulation.WaterSourceData> waterSourceDataNativeArray = chunk.GetNativeArray(ref m_SourceType);
                NativeArray<SeasonalStreamsData> originalAmountNativeArray = chunk.GetNativeArray(ref m_OriginalAmountType);
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Game.Simulation.WaterSourceData currentWaterSourceData = waterSourceDataNativeArray[i];
                    currentWaterSourceData.m_Amount = originalAmountNativeArray[i].m_OriginalAmount;
                    buffer.SetComponent(entityNativeArray[i], currentWaterSourceData);
                    buffer.RemoveComponent<SeasonalStreamsData>(entityNativeArray[i]);
                }
            }
        }
    }
}
