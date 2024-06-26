﻿// <copyright file="BeforeSerializeSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Water_Features.Systems
{
    using System.Runtime.CompilerServices;
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
    /// A system that runs before serialization so that all water sources are reset and/or saved in a manner that can be reloaded safely without the mod.
    /// </summary>
    public partial class BeforeSerializeSystem : GameSystemBase
    {
        private EntityQuery m_SeasonalStreamsDataQuery;
        private EntityQuery m_TidesAndWavesDataQuery;
        private EntityQuery m_AutofillingLakeQuery;
        private EntityQuery m_DetentionBasinQuery;
        private EntityQuery m_RetentionBasinQuery;
        private TidesAndWavesSystem m_TidesAndWavesSystem;
        private ILog m_Log;

        /// <summary>
        /// Initializes a new instance of the <see cref="BeforeSerializeSystem"/> class.
        /// </summary>
        public BeforeSerializeSystem()
        {
        }

        /// <inheritdoc/>
        protected override void OnCreate ()
        {
            base.OnCreate();
            m_Log = WaterFeaturesMod.Instance.Log;
            m_TidesAndWavesSystem = World.GetOrCreateSystemManaged<TidesAndWavesSystem>();
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
            m_AutofillingLakeQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<Game.Simulation.WaterSourceData>(),
                        ComponentType.ReadOnly<AutofillingLake>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                    },
                },
            });
            m_RetentionBasinQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<Game.Simulation.WaterSourceData>(),
                        ComponentType.ReadOnly<RetentionBasin>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                    },
                },
            });
            m_DetentionBasinQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<Game.Simulation.WaterSourceData>(),
                        ComponentType.ReadOnly<DetentionBasin>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                    },
                },
            });
            RequireAnyForUpdate(new EntityQuery[] { m_AutofillingLakeQuery, m_SeasonalStreamsDataQuery, m_TidesAndWavesDataQuery, m_DetentionBasinQuery, m_RetentionBasinQuery});
            m_Log.Info($"[{nameof(BeforeSerializeSystem)}] {nameof(OnCreate)}");
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            m_TidesAndWavesSystem.ResetDummySeaWaterSource();
            BeforeSerializeSeasonalStreamsJob beforeSerializeSeasonalStreamsJob = new ()
            {
                m_OriginalAmountType = SystemAPI.GetComponentTypeHandle<SeasonalStreamsData>(),
                m_SourceType = SystemAPI.GetComponentTypeHandle<Game.Simulation.WaterSourceData>(),
            };
            Dependency = JobChunkExtensions.Schedule(beforeSerializeSeasonalStreamsJob, m_SeasonalStreamsDataQuery, Dependency);
            BeforeSerializeTidesAndWavesJob beforeSerializeTidesAndWavesJob = new ()
            {
                m_OriginalAmountType = SystemAPI.GetComponentTypeHandle<TidesAndWavesData>(),
                m_SourceType = SystemAPI.GetComponentTypeHandle<Game.Simulation.WaterSourceData>(),
            };
            Dependency = JobChunkExtensions.Schedule(beforeSerializeTidesAndWavesJob, m_TidesAndWavesDataQuery, Dependency);
            BeforeSerializeAutofillingLakeJob beforeSerializeAutofillingLakeJob = new ()
            {
                m_SourceType = SystemAPI.GetComponentTypeHandle<Game.Simulation.WaterSourceData>(),
                m_AutofillingLakeType = SystemAPI.GetComponentTypeHandle<AutofillingLake>(),
            };
            Dependency = JobChunkExtensions.Schedule(beforeSerializeAutofillingLakeJob, m_AutofillingLakeQuery, Dependency);
            BeforeSerializeDetentionBasinJob beforeSerializeDetentionBasinJob = new ()
            {
                m_SourceType = SystemAPI.GetComponentTypeHandle<Game.Simulation.WaterSourceData>(),
                m_DetentionBasinType = SystemAPI.GetComponentTypeHandle<DetentionBasin>(),
            };
            Dependency = JobChunkExtensions.Schedule(beforeSerializeDetentionBasinJob, m_DetentionBasinQuery, Dependency);
            BeforeSerializeRetentionBasinJob beforeSerializeLakeLikeWaterSourcesJob = new ()
            {
                m_SourceType = SystemAPI.GetComponentTypeHandle<Game.Simulation.WaterSourceData>(),
                m_RetentionBasinType = SystemAPI.GetComponentTypeHandle<RetentionBasin>(),
            };
            Dependency = JobChunkExtensions.Schedule(beforeSerializeLakeLikeWaterSourcesJob, m_RetentionBasinQuery, Dependency);
        }

        /// <summary>
        /// This job sets the amounts for seasonal stream water sources back to original amount.
        /// </summary>
#if BURST
        [BurstCompile]
#endif
        private struct BeforeSerializeSeasonalStreamsJob : IJobChunk
        {
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_SourceType;
            [ReadOnly]
            public ComponentTypeHandle<SeasonalStreamsData> m_OriginalAmountType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Game.Simulation.WaterSourceData> waterSourceDataNativeArray = chunk.GetNativeArray(ref m_SourceType);
                NativeArray<SeasonalStreamsData> originalAmountNativeArray = chunk.GetNativeArray(ref m_OriginalAmountType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Game.Simulation.WaterSourceData currentWaterSourceData = waterSourceDataNativeArray[i];
                    currentWaterSourceData.m_Amount = originalAmountNativeArray[i].m_OriginalAmount;
                    waterSourceDataNativeArray[i] = currentWaterSourceData;
                }
            }
        }

        /// <summary>
        /// This job sets the amounts for sea water sources back to original amount.
        /// </summary>
#if BURST
        [BurstCompile]
#endif
        private struct BeforeSerializeTidesAndWavesJob : IJobChunk
        {
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_SourceType;
            [ReadOnly]
            public ComponentTypeHandle<TidesAndWavesData> m_OriginalAmountType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Game.Simulation.WaterSourceData> waterSourceDataNativeArray = chunk.GetNativeArray(ref m_SourceType);
                NativeArray<TidesAndWavesData> originalAmountNativeArray = chunk.GetNativeArray(ref m_OriginalAmountType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Game.Simulation.WaterSourceData currentWaterSourceData = waterSourceDataNativeArray[i];
                    currentWaterSourceData.m_Amount = originalAmountNativeArray[i].m_OriginalAmount;
                    waterSourceDataNativeArray[i] = currentWaterSourceData;
                }
            }
        }

#if BURST
        [BurstCompile]
#endif
        /// <summary>
        /// This job makes an automatic filling lake into a vanilla lake.
        /// </summary>
        private struct BeforeSerializeAutofillingLakeJob : IJobChunk
        {
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_SourceType;
            public ComponentTypeHandle<AutofillingLake> m_AutofillingLakeType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Game.Simulation.WaterSourceData> waterSourceDataNativeArray = chunk.GetNativeArray(ref m_SourceType);
                NativeArray<AutofillingLake> autofillingLakeNativeArray = chunk.GetNativeArray(ref m_AutofillingLakeType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    Game.Simulation.WaterSourceData currentWaterSourceData = waterSourceDataNativeArray[i];
                    currentWaterSourceData.m_Amount = autofillingLakeNativeArray[i].m_MaximumWaterHeight;
                    currentWaterSourceData.m_ConstantDepth = 1; // Vanilla Lake
                    waterSourceDataNativeArray[i] = currentWaterSourceData;
                }
            }
        }

#if BURST
        [BurstCompile]
#endif
        /// <summary>
        /// This job makes all detention basins into lakes at the maximum water surface elevation.
        /// </summary>
        private struct BeforeSerializeDetentionBasinJob : IJobChunk
        {
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_SourceType;
            public ComponentTypeHandle<DetentionBasin> m_DetentionBasinType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Game.Simulation.WaterSourceData> waterSourceDataNativeArray = chunk.GetNativeArray(ref m_SourceType);
                NativeArray<DetentionBasin> detentionBasinNativeArray = chunk.GetNativeArray(ref m_DetentionBasinType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    Game.Simulation.WaterSourceData currentWaterSourceData = waterSourceDataNativeArray[i];
                    currentWaterSourceData.m_Amount = detentionBasinNativeArray[i].m_MaximumWaterHeight;
                    currentWaterSourceData.m_ConstantDepth = 1; // Vanilla Lake
                    waterSourceDataNativeArray[i] = currentWaterSourceData;
                }
            }
        }

#if BURST
        [BurstCompile]
#endif
        /// <summary>
        /// This job makes all retention basins into lakes at the maximum water surface elevation.
        /// </summary>
        private struct BeforeSerializeRetentionBasinJob : IJobChunk
        {
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_SourceType;
            public ComponentTypeHandle<RetentionBasin> m_RetentionBasinType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Game.Simulation.WaterSourceData> waterSourceDataNativeArray = chunk.GetNativeArray(ref m_SourceType);
                NativeArray<RetentionBasin> retentionBasinNativeArray = chunk.GetNativeArray(ref m_RetentionBasinType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    Game.Simulation.WaterSourceData currentWaterSourceData = waterSourceDataNativeArray[i];
                    currentWaterSourceData.m_Amount = retentionBasinNativeArray[i].m_MaximumWaterHeight;
                    currentWaterSourceData.m_ConstantDepth = 1; // Vanilla Lake
                    waterSourceDataNativeArray[i] = currentWaterSourceData;
                }
            }
        }
    }
}
