﻿// <copyright file="TidesAndWavesSystem.cs" company="Yenyang's Mods. MIT License">
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
    using UnityEngine;
    using Water_Features.Components;

    /// <summary>
    /// A system for handing waves and tides.
    /// </summary>
    public partial class TidesAndWavesSystem : GameSystemBase
    {
        private EndFrameBarrier m_EndFrameBarrier;
        private TimeSystem m_TimeSystem;
        private EntityQuery m_WaterSourceQuery;
        private ILog m_Log;
        private Entity m_DummySeaWaterSource = Entity.Null;
        private float m_PreviousWaveAndTideHeight = 0f;
        private WaterSystem m_WaterSystem;
        private ToolSystem m_ToolSystem;
        private TerrainToolSystem m_TerrainToolSystem;
        private int m_TerrainToolCooloff;
        private TerrainSystem m_TerrainSystem;
        private ChangeWaterSystemValues m_ChangeWaterSystemValues;
        private int m_RecordedWaterSimSpeed = 1;
        private FindWaterSourcesSystem m_FindWaterSourcesSystem;

        /// <summary>
        /// Gets the previous wave and tide height that was used to determine the dummy sea water source.
        /// </summary>
        public float PreviousWaveAndTideHeight { get => m_PreviousWaveAndTideHeight; }

        /// <summary>
        /// Gets the query for waves and tides data water sources.
        /// </summary>
        public EntityQuery WavesAndTidesDataQuery => m_WaterSourceQuery;

        /// <summary>
        /// The dummy sea water source should not be saved so this allows it to be removed before saving. This may need to be done in a job with a jobhandle. . .?.
        /// </summary>
        public void ResetDummySeaWaterSource()
        {
            if (m_DummySeaWaterSource != Entity.Null)
            {
                EntityManager.DestroyEntity(m_DummySeaWaterSource);
                m_DummySeaWaterSource = Entity.Null;
            }
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = WaterFeaturesMod.Instance.Log;
            m_TimeSystem = World.GetOrCreateSystemManaged<TimeSystem>();
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_TerrainToolSystem = World.GetOrCreateSystemManaged<TerrainToolSystem>();
            m_WaterSystem = World.GetOrCreateSystemManaged<WaterSystem>();
            m_ChangeWaterSystemValues = World.GetOrCreateSystemManaged<ChangeWaterSystemValues>();
            m_FindWaterSourcesSystem = World.GetOrCreateSystemManaged<FindWaterSourcesSystem>();
            m_TerrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();
            m_TerrainToolCooloff = -1;
            m_WaterSourceQuery = GetEntityQuery(new EntityQueryDesc[]
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
                        ComponentType.ReadOnly<Owner>(),
                    },
                },
            });
            RequireForUpdate(m_WaterSourceQuery);
            m_Log.Info($"[{nameof(TidesAndWavesSystem)}] {nameof(OnCreate)}");
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (m_ToolSystem.actionMode.IsEditor() && m_RecordedWaterSimSpeed != m_WaterSystem.WaterSimSpeed)
            {
                m_RecordedWaterSimSpeed = m_WaterSystem.WaterSimSpeed;
                m_FindWaterSourcesSystem.Enabled = true;
            }


            if (m_ToolSystem.actionMode.IsEditor() && m_RecordedWaterSimSpeed != m_WaterSystem.WaterSimSpeed)
            {
                m_FindWaterSourcesSystem.Enabled = true;
            }

            if (m_ToolSystem.activeTool == m_TerrainToolSystem)
            {
                if (!m_ChangeWaterSystemValues.TemporarilyUseOriginalDamping)
                {
                    m_Log.Debug($"{nameof(TidesAndWavesSystem)}.{nameof(OnUpdate)} Back to original damping values.");
                }

                m_ChangeWaterSystemValues.TemporarilyUseOriginalDamping = true;
                m_WaterSystem.m_Damping = m_ChangeWaterSystemValues.OriginalDamping;
                m_TerrainToolCooloff = 300;
            }

            if (m_ToolSystem.activeTool != m_TerrainToolSystem)
            {
                if (m_TerrainToolCooloff == 0)
                {
                    m_Log.Debug($"{nameof(TidesAndWavesSystem)}.{nameof(OnUpdate)} Back to modded damping values.");
                    m_ChangeWaterSystemValues.TemporarilyUseOriginalDamping = false;
                    if (m_WaterSystem.WaterSimSpeed == 0)
                    {
                        m_WaterSystem.WaterSimSpeed = 1;
                    }

                    m_TerrainToolCooloff -= 1;
                }
                else if (m_TerrainToolCooloff > 0)
                {
                    if (!m_ChangeWaterSystemValues.TemporarilyUseOriginalDamping)
                    {
                        m_Log.Debug($"{nameof(TidesAndWavesSystem)}.{nameof(OnUpdate)} Back to original damping values.");
                    }

                    m_ChangeWaterSystemValues.TemporarilyUseOriginalDamping = true;
                    m_WaterSystem.m_Damping = m_ChangeWaterSystemValues.OriginalDamping;
                    m_TerrainToolCooloff -= 1;
                }
            }

            // This section adds the dummy water source if it does not exist.
            if (m_DummySeaWaterSource == Entity.Null)
            {
                float seaLevel = float.MaxValue;
                NativeArray<TidesAndWavesData> seaWaterSources = m_WaterSourceQuery.ToComponentDataArray<TidesAndWavesData>(Allocator.Temp);
                foreach (TidesAndWavesData seaData in seaWaterSources)
                {
                    if (seaLevel > seaData.m_OriginalAmount)
                    {
                        seaLevel = seaData.m_OriginalAmount;
                    }
                }

                m_PreviousWaveAndTideHeight = WaterFeaturesMod.Instance.Settings.WaveHeight + WaterFeaturesMod.Instance.Settings.TideHeight;

                seaLevel -= WaterFeaturesMod.Instance.Settings.WaveHeight + WaterFeaturesMod.Instance.Settings.TideHeight;
                WaterSourceData waterSourceData = new WaterSourceData()
                {
                    m_Amount = seaLevel,
                    m_ConstantDepth = 3,
                    m_Multiplier = 30f,
                    m_Polluted = 0f,
                    m_Radius = 0f,
                };

                /* The dummy water source must be a sea water source, with the amount at the designated constant sea level.
                 * In this case that is the lowest original amount for all the sea levels minus waves and tides.
                 * The dummy water source is at coordinate 0,0 and has a radius of 0 so that it can be distinguished from actual sea water sources.
                */

                Game.Objects.Transform transform = new Game.Objects.Transform()
                {
                    m_Position = default,
                    m_Rotation = default,
                };

                m_DummySeaWaterSource = EntityManager.CreateEntity();
                EntityManager.AddComponent(m_DummySeaWaterSource, ComponentType.ReadWrite<WaterSourceData>());
                EntityManager.SetComponentData(m_DummySeaWaterSource, waterSourceData);
                EntityManager.AddComponent(m_DummySeaWaterSource, ComponentType.ReadWrite<Game.Objects.Transform>());
                EntityManager.SetComponentData(m_DummySeaWaterSource, transform);
                m_WaterSystem.WaterSimSpeed = 0;
                m_TerrainToolCooloff = 300;
            }

            AlterSeaWaterSourcesJob alterSeaWaterSourcesJob = new ()
            {
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_SourceType = SystemAPI.GetComponentTypeHandle<Game.Simulation.WaterSourceData>(),
                m_TidesAndWavesDataType = SystemAPI.GetComponentTypeHandle<TidesAndWavesData>(),
                buffer = m_EndFrameBarrier.CreateCommandBuffer(),
                m_WaveHeight = (WaterFeaturesMod.Instance.Settings.WaveHeight / 2f * Mathf.Sin(2f * Mathf.PI * WaterFeaturesMod.Instance.Settings.WaveFrequency * m_TimeSystem.normalizedTime)) + (WaterFeaturesMod.Instance.Settings.TideHeight / 2f * Mathf.Cos(2f * Mathf.PI * (float)WaterFeaturesMod.Instance.Settings.TideClassification * m_TimeSystem.normalizedDate)) + (WaterFeaturesMod.Instance.Settings.WaveHeight / 2f) + (WaterFeaturesMod.Instance.Settings.TideHeight / 2f),
            };
            JobHandle jobHandle = JobChunkExtensions.Schedule(alterSeaWaterSourcesJob, m_WaterSourceQuery, Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            // This will disable the system if the user has the setting for Waves and Tides disabled.
            if (!WaterFeaturesMod.Instance.Settings.EnableWavesAndTides)
            {
                m_Log.Info($"[{nameof(TidesAndWavesSystem)}] {nameof(OnGameLoadingComplete)} Waves and Tides disabled.");
                Enabled = false;
                DisableWavesAndTidesSystem disableWavesAndTidesSystem = World.GetOrCreateSystemManaged<DisableWavesAndTidesSystem>();
                disableWavesAndTidesSystem.Enabled = true;
            }

            // Sometimes the dummy water source does not have the correct sea level at first, so resetting it at game loading fixes it.
            ResetDummySeaWaterSource();
        }

#if BURST
        [BurstCompile]
#endif
        /// <summary>
        /// This job adjusts the water surface elevation of sea water sources according to the settings for waves and tides.
        /// </summary>
        private struct AlterSeaWaterSourcesJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_SourceType;
            public EntityCommandBuffer buffer;
            public ComponentTypeHandle<TidesAndWavesData> m_TidesAndWavesDataType;
            public float m_WaveHeight;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<Game.Simulation.WaterSourceData> waterSourceDataNativeArray = chunk.GetNativeArray(ref m_SourceType);
                NativeArray<TidesAndWavesData> wavesAndTidesDataNativeArray = chunk.GetNativeArray(ref m_TidesAndWavesDataType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity currentEntity = entityNativeArray[i];
                    Game.Simulation.WaterSourceData currentWaterSourceData = waterSourceDataNativeArray[i];
                    TidesAndWavesData currentTidesAndWavesData = wavesAndTidesDataNativeArray[i];
                    if (currentWaterSourceData.m_ConstantDepth == 3 && currentWaterSourceData.m_Amount > 0f)
                    {
                        currentWaterSourceData.m_Amount = currentTidesAndWavesData.m_OriginalAmount - m_WaveHeight;
                        buffer.SetComponent(currentEntity, currentWaterSourceData);
                    }
                    else if (currentWaterSourceData.m_ConstantDepth != 3)
                    {
                        buffer.RemoveComponent<TidesAndWavesData>(currentEntity);
                    }
                }
            }
        }
    }
}