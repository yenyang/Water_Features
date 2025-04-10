// <copyright file="FindWaterSourcesSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Water_Features.Systems
{
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.Tools;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Water_Features.Components;

    /// <summary>
    /// A system for finding different water sources and assigning additional componets related to the mod.
    /// </summary>
    public partial class FindWaterSourcesSystem : GameSystemBase
    {
        private EntityQuery m_WaterSourcesQuery;
        private EndFrameBarrier m_EndFrameBarrier;
        private ILog m_Log;
        private ToolSystem m_ToolSystem;
        private PrefabSystem m_PrefabSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindWaterSourcesSystem"/> class.
        /// </summary>
        public FindWaterSourcesSystem()
        {
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_Log = WaterFeaturesMod.Instance.Log;
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_WaterSourcesQuery = GetEntityQuery(new EntityQueryDesc[] {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Game.Simulation.WaterSourceData>(),
                        ComponentType.ReadOnly<Game.Objects.Transform>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<SeasonalStreamsData>(),
                        ComponentType.ReadOnly<TidesAndWavesData>(),
                        ComponentType.ReadOnly<AutofillingLake>(),
                        ComponentType.ReadOnly<RetentionBasin>(),
                        ComponentType.ReadOnly<DetentionBasin>(),
                        ComponentType.ReadOnly<Owner>(),
                    },
                },
            });
            RequireForUpdate(m_WaterSourcesQuery);
            m_Log.Info($"[{nameof(FindWaterSourcesSystem)}] {nameof(OnCreate)}");
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            bool seasonalStreamsEnabled = WaterFeaturesMod.Instance.Settings.EnableSeasonalStreams;
            bool wavesAndTidesEnabled = WaterFeaturesMod.Instance.Settings.EnableWavesAndTides;
            if (m_ToolSystem.actionMode.IsEditor())
            {
                if (!WaterFeaturesMod.Instance.Settings.SeasonalStreamsAffectEditorSimulation)
                {
                    seasonalStreamsEnabled = false;
                }

                if (!WaterFeaturesMod.Instance.Settings.WavesAndTidesAffectEditorSimulation)
                {
                    wavesAndTidesEnabled = false;
                }
            }


            m_Log.Debug($"{nameof(FindWaterSourcesSystem)}.{nameof(OnUpdate)} WaterFeaturesMod.Instance.Settings.EnableSeasonalStreams = {WaterFeaturesMod.Instance.Settings.EnableSeasonalStreams} &  WaterFeaturesMod.Instance.Settings.EnableWavesAndTides = {WaterFeaturesMod.Instance.Settings.EnableWavesAndTides}");
            FindWaterSourcesJob findWaterSourcesJob = new ()
            {
                m_SourceType = SystemAPI.GetComponentTypeHandle<Game.Simulation.WaterSourceData>(),
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                buffer = m_EndFrameBarrier.CreateCommandBuffer(),
                m_SeasonalStreamsEnabled = seasonalStreamsEnabled,
                m_WavesAndTidesEnabled = wavesAndTidesEnabled,
            };
            JobHandle jobHandle = JobChunkExtensions.Schedule(findWaterSourcesJob, m_WaterSourcesQuery, Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;

            Enabled = false;
        }

#if BURST
        [BurstCompile]
#endif
        /// <summary>
        /// If Seasonal streams is enabled, this job will add seasonal streams component to streams and record the original amounts.
        /// If Waves and tides are enabled, this job will add waves and tides component to seas and record the original amounts.
        /// </summary>
        private struct FindWaterSourcesJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_SourceType;
            public EntityCommandBuffer buffer;
            public bool m_SeasonalStreamsEnabled;
            public bool m_WavesAndTidesEnabled;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<Game.Simulation.WaterSourceData> waterSourceDataNativeArray = chunk.GetNativeArray(ref m_SourceType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity currentEntity = entityNativeArray[i];
                    Game.Simulation.WaterSourceData currentWaterSourceData = waterSourceDataNativeArray[i];
                    if (currentWaterSourceData.m_ConstantDepth == 0 && currentWaterSourceData.m_Amount > 0f && m_SeasonalStreamsEnabled)
                    {
                        SeasonalStreamsData waterSourceRecordComponent = new ()
                        {
                            m_OriginalAmount = currentWaterSourceData.m_Amount,
                        };
                        buffer.AddComponent<SeasonalStreamsData>(currentEntity);
                        buffer.SetComponent(currentEntity, waterSourceRecordComponent);
                    }
                    else if (currentWaterSourceData.m_ConstantDepth == 3 && currentWaterSourceData.m_Amount > 0f && currentWaterSourceData.m_Radius > 0f && m_WavesAndTidesEnabled)
                    {
                        buffer.AddComponent<TidesAndWavesData>(currentEntity);
                        TidesAndWavesData wavesAndTidesData = new ()
                        {
                            m_OriginalAmount = currentWaterSourceData.m_Amount,
                        };
                        buffer.SetComponent(currentEntity, wavesAndTidesData);
                    }
                }
            }
        }
    }
}
