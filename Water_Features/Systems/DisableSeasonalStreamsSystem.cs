// <copyright file="DisableSeasonalStreamsSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Water_Features.Systems
{
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Tools;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Water_Features.Components;

    /// <summary>
    /// A system that will disable seasonal streams and remove related components.
    /// </summary>
    public partial class DisableSeasonalStreamSystem : GameSystemBase
    {
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
            ResetSeasonalStreamsJob resetSeasonalStreamsJob = new ()
            {
                m_OriginalAmountType = SystemAPI.GetComponentTypeHandle<SeasonalStreamsData>(),
                m_SourceType = SystemAPI.GetComponentTypeHandle<Game.Simulation.WaterSourceData>(),
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                buffer = m_EndFrameBarrier.CreateCommandBuffer(),
            };
            Dependency = JobChunkExtensions.Schedule(resetSeasonalStreamsJob, m_SeasonalStreamsDataQuery, Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(Dependency);
            Enabled = false;
        }

#if BURST
        [BurstCompile]
#endif
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
