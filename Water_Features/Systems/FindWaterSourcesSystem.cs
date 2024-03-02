// <copyright file="FindWaterSourcesSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Systems
{
    using System.Runtime.CompilerServices;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.Tools;
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
        private TypeHandle __TypeHandle;
        private EntityQuery m_WaterSourcesQuery;
        private EndFrameBarrier m_EndFrameBarrier;
        private ILog m_Log;
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
            m_Log = WaterFeaturesMod.Instance.Log;
            m_EndFrameBarrier = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<EndFrameBarrier>();
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
            __TypeHandle.__Game_Simulation_WaterSourceData_RW_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref CheckedStateRef);
            m_Log.Debug($"{nameof(FindWaterSourcesSystem)}.{nameof(OnUpdate)} WaterFeaturesMod.Settings.EnableSeasonalStreams = {WaterFeaturesMod.Settings.EnableSeasonalStreams} &  WaterFeaturesMod.Settings.EnableWavesAndTides = {WaterFeaturesMod.Settings.EnableWavesAndTides}");
            FindWaterSourcesJob findWaterSourcesJob = default;
            findWaterSourcesJob.m_SourceType = __TypeHandle.__Game_Simulation_WaterSourceData_RW_ComponentTypeHandle;
            findWaterSourcesJob.m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
            findWaterSourcesJob.buffer = m_EndFrameBarrier.CreateCommandBuffer();
            findWaterSourcesJob.m_SeasonalStreamsEnabled = WaterFeaturesMod.Settings.EnableSeasonalStreams;
            findWaterSourcesJob.m_WavesAndTidesEnabled = WaterFeaturesMod.Settings.EnableWavesAndTides;
            FindWaterSourcesJob jobData = findWaterSourcesJob;
            JobHandle jobHandle = JobChunkExtensions.Schedule(jobData, m_WaterSourcesQuery, Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;

            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            Enabled = true;
            base.OnGameLoadingComplete(purpose, mode);
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
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AssignHandles(ref SystemState state)
            {
                __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                __Game_Simulation_WaterSourceData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Simulation.WaterSourceData>();
            }
        }

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
