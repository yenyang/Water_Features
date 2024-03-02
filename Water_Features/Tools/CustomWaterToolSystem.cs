// <copyright file="CustomWaterToolSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Tools
{
    using System.Runtime.CompilerServices;
    using Colossal.Entities;
    using Colossal.Logging;
    using Game.Common;
    using Game.Input;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Simulation;
    using Game.Tools;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using Water_Features;
    using Water_Features.Components;
    using Water_Features.Prefabs;
    using Water_Features.Systems;

    /// <summary>
    /// A custom water tool system for creating and removing water sources.
    /// </summary>
    public partial class CustomWaterToolSystem : ToolBaseSystem
    {
        private const float MapExtents = 7168f;
        private EntityArchetype m_WaterSourceArchetype;
        private EntityArchetype m_AutoFillingLakeArchetype;
        private EntityArchetype m_DetentionBasinArchetype;
        private EntityArchetype m_RetentionBasinArchetype;
        private ProxyAction m_ApplyAction;
        private ProxyAction m_SecondaryApplyAction;
        private ControlPoint m_RaycastPoint;
        private TypeHandle __TypeHandle;
        private EntityQuery m_WaterSourcesQuery;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private OverlayRenderSystem m_OverlayRenderSystem;
        private TidesAndWavesSystem m_TidesAndWavesSystem;
        private WaterToolUISystem m_WaterToolUISystem;
        private FindWaterSourcesSystem m_FindWaterSourcesSystem;
        private WaterTooltipSystem m_WaterTooltipSystem;
        private WaterSystem m_WaterSystem;
        private TerrainSystem m_TerrainSystem;
        private ILog m_Log;
        private NativeList<Entity> m_HoveredWaterSources;
        private WaterSourcePrefab m_ActivePrefab;

        /// <summary>
        /// Gets a value indicating the toolid.
        /// </summary>
        public override string toolID => "Yenyang's Water Tool";

        /// <summary>
        /// A method for determining if a position is close to the border.
        /// </summary>
        /// <param name="pos">Position to be checked.</param>
        /// <param name="radius">Tolerance for acceptable position.</param>
        /// <param name="fixedMaxDistance">Should the radius be checked for a maximum.</param>
        /// <returns>True if within proximity of border.</returns>
        public bool IsPositionNearBorder(float3 pos, float radius, bool fixedMaxDistance)
        {
            if (fixedMaxDistance)
            {
                radius = Mathf.Max(150f, radius * 2f / 3f);
            }

            if (Mathf.Abs(MapExtents - Mathf.Abs(pos.x)) < radius || Mathf.Abs(MapExtents - Mathf.Abs(pos.z)) < radius)
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
        public bool IsPositionWithinBorder(float3 pos)
        {
            if (Mathf.Max(Mathf.Abs(pos.x), Mathf.Abs(pos.z)) < MapExtents)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// If there is an interactable portion of a water source under the cursor then it returns true.
        /// </summary>
        /// <returns>True if a water source can be deleted. False if not.</returns>
        public bool CanDeleteWaterSource()
        {
            if (m_HoveredWaterSources.Length > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Loops through hovered entities and finds the one that is closest to the position.
        /// </summary>
        /// <param name="position">Should be raycast hit position.</param>
        /// <returns>Entity.null if it can't find anything otherwise Entity of closest hovered source.</returns>
        public Entity GetHoveredEntity(float3 position)
        {
            if (m_HoveredWaterSources.IsEmpty)
            {
                return Entity.Null;
            }

            position.y = 0f;
            float distance = float.MaxValue;
            Entity entity = Entity.Null;
            foreach (Entity e in m_HoveredWaterSources)
            {
                if (EntityManager.TryGetComponent(e, out Game.Objects.Transform transform)) 
                {
                    transform.m_Position.y = 0f;
                    if (math.distance(transform.m_Position, position) < distance)
                    {
                        distance = math.distance(transform.m_Position, position);
                        entity = e;
                    }
                }
            }

            return entity;
        }

        /// <summary>
        /// When the tool is canceled set active tool to default tool.
        /// </summary>
        public void RequestDisable()
        {
            m_ToolSystem.activeTool = m_DefaultToolSystem;
        }

        /// <inheritdoc/>
        public override PrefabBase GetPrefab()
        {
            if (m_ToolSystem.activeTool == this && m_ActivePrefab != null)
            {
                return m_ActivePrefab;
            }

            return null;
        }

        /// <inheritdoc/>
        public override bool TrySetPrefab(PrefabBase prefab)
        {
            m_Log.Debug($"{nameof(CustomWaterToolSystem)}.{nameof(TrySetPrefab)}");
            if (prefab is WaterSourcePrefab)
            {
                m_Log.Debug($"{nameof(CustomWaterToolSystem)}.{nameof(TrySetPrefab)} prefab is {prefab.name}.");
                if (m_ActivePrefab != null)
                {
                    if ((prefab as WaterSourcePrefab) == m_ActivePrefab)
                    {
                        return true;
                    }
                }

                m_ActivePrefab = prefab as WaterSourcePrefab;
                m_ToolSystem.EventPrefabChanged?.Invoke(prefab);
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            m_ToolRaycastSystem.typeMask = TypeMask.Terrain | TypeMask.Water;
            m_ToolRaycastSystem.raycastFlags = RaycastFlags.Outside;
        }

        /// <inheritdoc/>
        public override void GetAvailableSnapMask(out Snap onMask, out Snap offMask)
        {
            base.GetAvailableSnapMask(out onMask, out offMask);
            onMask |= Snap.ContourLines;
            offMask |= Snap.ContourLines;
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = WaterFeaturesMod.Instance.Log;
            Enabled = false;
            m_ApplyAction = InputManager.instance.FindAction("Tool", "Apply");
            m_SecondaryApplyAction = InputManager.instance.FindAction("Tool", "Secondary Apply");
            m_Log.Info($"[{nameof(CustomWaterToolSystem)}] {nameof(OnCreate)}");
            m_ToolOutputBarrier = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_WaterSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<WaterSystem>();
            m_WaterTooltipSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<WaterTooltipSystem>();
            m_TerrainSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TerrainSystem>();
            m_TidesAndWavesSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TidesAndWavesSystem>();
            m_WaterToolUISystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<WaterToolUISystem>();
            m_FindWaterSourcesSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<FindWaterSourcesSystem>();
            m_WaterSourceArchetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Simulation.WaterSourceData>(), ComponentType.ReadWrite<Game.Objects.Transform>());
            m_AutoFillingLakeArchetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Simulation.WaterSourceData>(), ComponentType.ReadWrite<Game.Objects.Transform>(), ComponentType.ReadWrite<AutofillingLake>());
            m_DetentionBasinArchetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Simulation.WaterSourceData>(), ComponentType.ReadWrite<Game.Objects.Transform>(), ComponentType.ReadWrite<DetentionBasin>());
            m_RetentionBasinArchetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Simulation.WaterSourceData>(), ComponentType.ReadWrite<Game.Objects.Transform>(), ComponentType.ReadWrite<RetentionBasin>());
            m_OverlayRenderSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<OverlayRenderSystem>();
            m_HoveredWaterSources = new NativeList<Entity>(0, Allocator.Persistent);
            m_WaterSourcesQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new ()
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
                        ComponentType.ReadOnly<Owner>(),
                    },
                },
            });
            base.OnCreate();
        }

        /// <inheritdoc/>
        protected override void OnStartRunning()
        {
            m_Log.Debug($"{nameof(CustomWaterToolSystem)}.{nameof(OnStartRunning)}");
            m_ApplyAction.shouldBeEnabled = true;
            m_SecondaryApplyAction.shouldBeEnabled = true;
            m_RaycastPoint = default;
        }

        /// <inheritdoc/>
        protected override void OnStopRunning()
        {
            m_ApplyAction.shouldBeEnabled = false;
            m_SecondaryApplyAction.shouldBeEnabled = false;
        }

        /// <inheritdoc/>
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = Dependency;
            __TypeHandle.__Game_Simulation_WaterSourceData_RO_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__DetentionBasin_Lookup.Update(ref CheckedStateRef);
            __TypeHandle.__RententionBasin_Lookup.Update(ref CheckedStateRef);
            __TypeHandle.__AutofillingLake_Lookup.Update(ref CheckedStateRef);

            TerrainHeightData terrainHeightData = m_TerrainSystem.GetHeightData();

            WaterSourceCirclesRenderJob waterSourceCirclesRenderJob = new ()
            {
                m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out JobHandle outJobHandle),
                m_SourceType = __TypeHandle.__Game_Simulation_WaterSourceData_RO_ComponentTypeHandle,
                m_TransformType = __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle,
                m_TerrainHeightData = m_TerrainSystem.GetHeightData(false),
                m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out JobHandle waterSurfaceDataJob),
                m_DetentionBasinLookup = __TypeHandle.__DetentionBasin_Lookup,
                m_RetentionBasinLookup = __TypeHandle.__RententionBasin_Lookup,
                m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_AutofillingLakeLookup = __TypeHandle.__AutofillingLake_Lookup,
            };
            inputDeps = JobChunkExtensions.Schedule(waterSourceCirclesRenderJob, m_WaterSourcesQuery, JobHandle.CombineDependencies(inputDeps, outJobHandle, waterSurfaceDataJob));
            m_OverlayRenderSystem.AddBufferWriter(inputDeps);
            m_TerrainSystem.AddCPUHeightReader(inputDeps);
            m_WaterSystem.AddSurfaceReader(inputDeps);

            bool raycastHit = GetRaycastResult(out m_RaycastPoint);

            // This clears HoveredWaterSources and returns early if raycast cannot hit terrain or water. Usually this is from hovering over a ui panel.
            if (!raycastHit)
            {
                m_HoveredWaterSources.Clear();
                return inputDeps;
            }

            if (m_ApplyAction.WasPressedThisFrame() && m_HoveredWaterSources.IsEmpty)
            {
                // Checks for valid placement of Seas, and water sources placed within the playable area.
                if ((m_ActivePrefab.m_SourceType != WaterToolUISystem.SourceType.River && m_ActivePrefab.m_SourceType != WaterToolUISystem.SourceType.Sea && IsPositionWithinBorder(m_RaycastPoint.m_HitPosition)) || (IsPositionNearBorder(m_RaycastPoint.m_HitPosition, m_WaterToolUISystem.Radius, false) && m_ActivePrefab.m_SourceType == WaterToolUISystem.SourceType.Sea))
                {
                    float terrainHeight = TerrainUtils.SampleHeight(ref terrainHeightData, m_RaycastPoint.m_HitPosition);
                    TryAddWaterSource(ref inputDeps, new float3(m_RaycastPoint.m_HitPosition.x, terrainHeight, m_RaycastPoint.m_HitPosition.z));
                    return inputDeps;
                }

                // Checks for valid placement of Rivers.
                else if (IsPositionNearBorder(m_RaycastPoint.m_HitPosition, m_WaterToolUISystem.Radius, true) && m_ActivePrefab.m_SourceType == WaterToolUISystem.SourceType.River)
                {
                    float3 borderPosition = m_RaycastPoint.m_HitPosition;
                    if (Mathf.Abs(m_RaycastPoint.m_HitPosition.x) >= Mathf.Abs(m_RaycastPoint.m_HitPosition.z))
                    {
                        if (m_RaycastPoint.m_HitPosition.x > 0f)
                        {
                            borderPosition.x = MapExtents;
                        }
                        else
                        {
                            borderPosition.x = MapExtents * -1f;
                        }
                    }
                    else
                    {
                        if (m_RaycastPoint.m_HitPosition.z > 0f)
                        {
                            borderPosition.z = MapExtents;
                        }
                        else
                        {
                            borderPosition.z = MapExtents * -1f;
                        }
                    }

                    float terrainHeight = TerrainUtils.SampleHeight(ref terrainHeightData, borderPosition);
                    TryAddWaterSource(ref inputDeps, new float3(borderPosition.x, terrainHeight, borderPosition.z));
                    return inputDeps;
                }
            }

            // This section is for removing water sources. The player must have hovered over one in the previous frame.
            else if (m_SecondaryApplyAction.WasReleasedThisFrame() && m_HoveredWaterSources.Length > 0)
            {
                Entity closestWaterSource = GetHoveredEntity(m_RaycastPoint.m_HitPosition);
                if (closestWaterSource != Entity.Null)
                {
                    RemoveEntityJob removeEntityJob = new RemoveEntityJob()
                    {
                        m_Entity = closestWaterSource,
                        buffer = m_ToolOutputBarrier.CreateCommandBuffer(),
                    };

                    JobHandle jobHandle1 = removeEntityJob.Schedule(inputDeps);
                    m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle1);
                    inputDeps = jobHandle1;
                }
                else
                {
                    RemoveWaterSourcesJob removeWaterSourcesJob = new ()
                    {
                        m_SourceType = __TypeHandle.__Game_Simulation_WaterSourceData_RO_ComponentTypeHandle,
                        m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle,
                        m_Position = m_RaycastPoint.m_HitPosition,
                        m_TransformType = __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle,
                        buffer = m_ToolOutputBarrier.CreateCommandBuffer(),
                        m_MapExtents = MapExtents,
                    };
                    JobHandle jobHandle = JobChunkExtensions.Schedule(removeWaterSourcesJob, m_WaterSourcesQuery, inputDeps);
                    m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
                    inputDeps = jobHandle;
                }
            }

            // This section is for setting the target elevation with sources other than Streams.
            else if (m_SecondaryApplyAction.WasPressedThisFrame() && m_HoveredWaterSources.IsEmpty && m_ActivePrefab.m_SourceType != WaterToolUISystem.SourceType.Stream)
            {
                m_WaterToolUISystem.SetElevation(m_RaycastPoint.m_HitPosition.y);
            }

            m_WaterTooltipSystem.HitPosition = m_RaycastPoint.m_HitPosition;

            // This section will render the circle(s) for new water source if not hovering over a water source, and valid placement.
            if (m_HoveredWaterSources.IsEmpty)
            {
                if ((m_ActivePrefab.m_SourceType == WaterToolUISystem.SourceType.River && IsPositionNearBorder(m_RaycastPoint.m_HitPosition, m_WaterToolUISystem.Radius, true))
                 || (m_ActivePrefab.m_SourceType == WaterToolUISystem.SourceType.Sea && IsPositionNearBorder(m_RaycastPoint.m_HitPosition, m_WaterToolUISystem.Radius, false))
                 || (m_ActivePrefab.m_SourceType != WaterToolUISystem.SourceType.River && m_ActivePrefab.m_SourceType != WaterToolUISystem.SourceType.Sea && IsPositionWithinBorder(m_RaycastPoint.m_HitPosition)))
                {
                    float radius = m_WaterToolUISystem.Radius;
                    float terrainHeight = TerrainUtils.SampleHeight(ref terrainHeightData, m_RaycastPoint.m_HitPosition);
                    float3 position = new float3(m_RaycastPoint.m_HitPosition.x, terrainHeight, m_RaycastPoint.m_HitPosition.z);

                    // This section makes the overlay for Rivers snap to the boundary.
                    if (m_ActivePrefab.m_SourceType == WaterToolUISystem.SourceType.River)
                    {
                        float3 borderPosition = m_RaycastPoint.m_HitPosition;
                        if (Mathf.Abs(m_RaycastPoint.m_HitPosition.x) >= Mathf.Abs(m_RaycastPoint.m_HitPosition.z))
                        {
                            if (m_RaycastPoint.m_HitPosition.x > 0f)
                            {
                                borderPosition.x = MapExtents;
                            }
                            else
                            {
                                borderPosition.x = MapExtents * -1f;
                            }
                        }
                        else
                        {
                            if (m_RaycastPoint.m_HitPosition.z > 0f)
                            {
                                borderPosition.z = MapExtents;
                            }
                            else
                            {
                                borderPosition.z = MapExtents * -1f;
                            }
                        }

                        terrainHeight = TerrainUtils.SampleHeight(ref terrainHeightData, borderPosition);
                        position = new float3(borderPosition.x, terrainHeight, borderPosition.z);
                    }

                    // This section handles projected water surface elevation.
                    if (m_ActivePrefab.m_SourceType != WaterToolUISystem.SourceType.Stream)
                    {
                        float amount = m_WaterToolUISystem.Amount;
                        float elevation = terrainHeight + amount;
                        if (m_WaterToolUISystem.AmountIsAnElevation)
                        {
                            elevation = amount;
                        }

                        // Based on experiments the predicted water surface elevation is always higher than the result.
                        float approximateError = 2.5f;

                        float3 projectedWaterSurfacePosition = new float3(m_RaycastPoint.m_HitPosition.x, elevation - approximateError, m_RaycastPoint.m_HitPosition.z);
                        if (m_ActivePrefab.m_SourceType == WaterToolUISystem.SourceType.River)
                        {
                            projectedWaterSurfacePosition = new float3(position.x, elevation - approximateError, position.z);
                        }

                        WaterLevelProjectionJob waterLevelProjectionJob = new ()
                        {
                            m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out JobHandle outputJobHandle),
                            m_Position = projectedWaterSurfacePosition,
                            m_Radius = radius,
                        };
                        JobHandle jobHandle1 = IJobExtensions.Schedule(waterLevelProjectionJob, outputJobHandle);
                        m_OverlayRenderSystem.AddBufferWriter(jobHandle1);
                        inputDeps = JobHandle.CombineDependencies(jobHandle1, inputDeps);
                    }

                    WaterToolRadiusJob waterToolRadiusJob = new ()
                    {
                        m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out JobHandle outJobHandle2),
                        m_Position = position,
                        m_Radius = radius,
                        m_SourceType = m_ActivePrefab.m_SourceType,
                    };
                    JobHandle jobHandle = IJobExtensions.Schedule(waterToolRadiusJob, outJobHandle2);
                    m_OverlayRenderSystem.AddBufferWriter(jobHandle);
                    inputDeps = JobHandle.CombineDependencies(jobHandle, inputDeps);
                }
            }

            m_HoveredWaterSources.Clear();
            HoverOverWaterSourceJob hoverOverWaterSourceJob = new ()
            {
                m_SourceType = __TypeHandle.__Game_Simulation_WaterSourceData_RO_ComponentTypeHandle,
                m_TransformType = __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle,
                m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_Position = m_RaycastPoint.m_HitPosition,
                m_Entities = m_HoveredWaterSources,
                m_MapExtents = MapExtents,
            };
            inputDeps = JobChunkExtensions.Schedule(hoverOverWaterSourceJob, m_WaterSourcesQuery, inputDeps);

            return inputDeps;
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            m_HoveredWaterSources.Dispose();
            base.OnDestroy();
        }

        /// <inheritdoc/>
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            __TypeHandle.AssignHandles(ref CheckedStateRef);
        }

        /// <summary>
        /// This method setsup the components needed to create a water source and schedules the job.
        /// </summary>
        /// <param name="deps">A jobhandle, usually InputDeps.</param>
        /// <param name="position">The location for the new water source.</param>
        private void TryAddWaterSource(ref JobHandle deps, float3 position)
        {
            float pollution = 0; // Alter later if UI for adding pollution. Also check to make sure it's smaller than amount later.
            float amount = m_WaterToolUISystem.Amount;

            // This section handles saving new default values for future use.
            if (!m_WaterToolUISystem.AmountIsAnElevation)
            {
                m_WaterToolUISystem.TrySaveDefaultValuesForWaterSource(m_ActivePrefab, m_WaterToolUISystem.Amount, m_WaterToolUISystem.Radius);
            }
            else
            {
                m_WaterToolUISystem.TrySaveDefaultValuesForWaterSource(m_ActivePrefab, m_WaterToolUISystem.Radius);
            }

            // This section adjusts the amount value for different types of water sources.
            if (m_ActivePrefab.m_SourceType != WaterToolUISystem.SourceType.Stream && m_ActivePrefab.m_SourceType != WaterToolUISystem.SourceType.Lake && !m_WaterToolUISystem.AmountIsAnElevation)
            {
                amount += position.y;
            }
            else if (m_WaterToolUISystem.AmountIsAnElevation && m_ActivePrefab.m_SourceType == WaterToolUISystem.SourceType.Lake)
            {
                amount -= position.y;
            }

            float radius = m_WaterToolUISystem.Radius;
            int constantDepth = (int)m_ActivePrefab.m_SourceType;
            if (constantDepth >= 4 && constantDepth <= 6)
            {
                constantDepth = 0;
            }

            Game.Simulation.WaterSourceData waterSourceDataComponent = new ()
            {
                m_Amount = amount,
                m_ConstantDepth = constantDepth,
                m_Radius = radius,
                m_Polluted = pollution,
                m_Multiplier = 30f,
            };
            Game.Objects.Transform transformComponent = new ()
            {
                m_Position = new Unity.Mathematics.float3(position.x, position.y, position.z),
                m_Rotation = quaternion.identity,
            };

            // This section checks for unaccectable multipliers and tries to adjust the radius if necessary. Right now the ui is limitting the radius amount to a generally acceptable range.
            bool acceptableMultiplier = true;
            bool unacceptableMultiplier = false;
            if (m_ActivePrefab.m_SourceType != WaterToolUISystem.SourceType.River && m_ActivePrefab.m_SourceType != WaterToolUISystem.SourceType.Sea)
            {
                int attempts = 0;
                waterSourceDataComponent.m_Multiplier = 1f;
                while (waterSourceDataComponent.m_Multiplier == 1f)
                {
                    waterSourceDataComponent.m_Multiplier = WaterSystem.CalculateSourceMultiplier(waterSourceDataComponent, transformComponent.m_Position);
                    attempts++;
                    if (attempts >= 1000f)
                    {
                        acceptableMultiplier = false;
                        break;
                    }

                    if (waterSourceDataComponent.m_Multiplier == 1f)
                    {
                        waterSourceDataComponent.m_Radius += 1f;
                        unacceptableMultiplier = true;
                    }
                }
            }

            if (unacceptableMultiplier == true)
            {
                m_WaterTooltipSystem.RadiusTooSmall = true;
                m_Log.Info($"{nameof(CustomWaterToolSystem)}.{nameof(TryAddWaterSource)} Radius too small. Increased radius to {waterSourceDataComponent.m_Radius}.");
            }

            if (acceptableMultiplier)
            {
                if ((int)m_ActivePrefab.m_SourceType <= 3)
                {
                    if (m_ActivePrefab.m_SourceType == WaterToolUISystem.SourceType.Sea)
                    {
                        m_TidesAndWavesSystem.ResetDummySeaWaterSource(); // Hopefully this doesn't cause problems.
                    }

                    AddWaterSourceJob addWaterSourceJob = new ()
                    {
                        waterSourceData = waterSourceDataComponent,
                        transform = transformComponent,
                        buffer = m_ToolOutputBarrier.CreateCommandBuffer(),
                        entityArchetype = m_WaterSourceArchetype,
                    };
                    JobHandle jobHandle = IJobExtensions.Schedule(addWaterSourceJob, Dependency);
                    m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
                    deps = jobHandle;
                }
                else if (m_ActivePrefab.m_SourceType == WaterToolUISystem.SourceType.Lake)
                {
                    waterSourceDataComponent.m_Amount *= 0.4f;

                    if (waterSourceDataComponent.m_Radius < 20f)
                    {
                        waterSourceDataComponent.m_Amount *= Mathf.Pow(waterSourceDataComponent.m_Radius / 20f, 2);
                    }

                    AddAutoFillingLakeJob addAutoFillingLakeJob = new ()
                    {
                        autoFillingLakeData = new AutofillingLake() { m_MaximumWaterHeight = amount + position.y },
                        entityArchetype = m_AutoFillingLakeArchetype,
                        buffer = m_ToolOutputBarrier.CreateCommandBuffer(),
                        transform = transformComponent,
                        waterSourceData = waterSourceDataComponent,
                    };

                    JobHandle jobHandle = IJobExtensions.Schedule(addAutoFillingLakeJob, Dependency);
                    m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
                    deps = jobHandle;
                }
                else if (m_ActivePrefab.m_SourceType == WaterToolUISystem.SourceType.DetentionBasin)
                {
                    waterSourceDataComponent.m_Amount = 0f;
                    AddDetentionBasinJob addDetentionBasinJob = new ()
                    {
                        detentionBasinData = new DetentionBasin() { m_MaximumWaterHeight = amount },
                        entityArchetype = m_DetentionBasinArchetype,
                        buffer = m_ToolOutputBarrier.CreateCommandBuffer(),
                        transform = transformComponent,
                        waterSourceData = waterSourceDataComponent,
                    };
                    JobHandle jobHandle = IJobExtensions.Schedule(addDetentionBasinJob, Dependency);
                    m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
                    deps = jobHandle;
                }
                else if (m_ActivePrefab.m_SourceType == WaterToolUISystem.SourceType.RetentionBasin)
                {
                    waterSourceDataComponent.m_Amount = m_WaterToolUISystem.MinDepth;
                    AddRetentionBasinJob addRetentionBasinJob = new ()
                    {
                        retentionBasinData = new RetentionBasin() { m_MaximumWaterHeight = amount, m_MinimumWaterHeight = m_WaterToolUISystem.MinDepth + position.y },
                        entityArchetype = m_RetentionBasinArchetype,
                        buffer = m_ToolOutputBarrier.CreateCommandBuffer(),
                        transform = transformComponent,
                        waterSourceData = waterSourceDataComponent,
                    };
                    JobHandle jobHandle = IJobExtensions.Schedule(addRetentionBasinJob, Dependency);
                    m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
                    deps = jobHandle;
                }
            }
            else
            {
                m_Log.Warn("{WaterToolUpdate.TryAddWaterSource} After 1000 attempts couldn't produce acceptable water source!");
            }

            m_FindWaterSourcesSystem.Enabled = true;
        }

        /// <summary>
        /// This job removes any Entity.
        /// </summary>
        private struct RemoveEntityJob : IJob
        {
            public Entity m_Entity;
            public EntityCommandBuffer buffer;

            public void Execute()
            {
                buffer.DestroyEntity(m_Entity);
            }
        }


        /// <summary>
        /// This job removes a water source.
        /// </summary>
        private struct RemoveWaterSourcesJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_SourceType;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;
            public float3 m_Position;
            public EntityCommandBuffer buffer;
            public float m_MapExtents;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<Game.Simulation.WaterSourceData> waterSourceDataNativeArray = chunk.GetNativeArray(ref m_SourceType);
                NativeArray<Game.Objects.Transform> transformNativeArray = chunk.GetNativeArray(ref m_TransformType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity currentEntity = entityNativeArray[i];
                    Game.Simulation.WaterSourceData currentWaterSourceData = waterSourceDataNativeArray[i];
                    Game.Objects.Transform currentTransform = transformNativeArray[i];
                    m_Position.y = 0;
                    currentTransform.m_Position.y = 0;
                    if (math.distance(m_Position, currentTransform.m_Position) < Mathf.Clamp(currentWaterSourceData.m_Radius, 25f, 150f))
                    {
                        buffer.DestroyEntity(currentEntity);
                    }
                }
            }
        }

        /// <summary>
        /// This job adds a vanilla water source.
        /// </summary>
        private struct AddWaterSourceJob : IJob
        {
            public Game.Simulation.WaterSourceData waterSourceData;
            public Game.Objects.Transform transform;
            public EntityCommandBuffer buffer;
            public EntityArchetype entityArchetype;

            public void Execute()
            {
                Entity currentEntity = buffer.CreateEntity(entityArchetype);
                buffer.SetComponent(currentEntity, waterSourceData);
                buffer.SetComponent(currentEntity, transform);
                buffer.AddComponent<Updated>(currentEntity);
            }
        }

        /// <summary>
        /// This job adds an AutoFillingLake water source.
        /// </summary>
        private struct AddAutoFillingLakeJob : IJob
        {
            public Game.Simulation.WaterSourceData waterSourceData;
            public Game.Objects.Transform transform;
            public AutofillingLake autoFillingLakeData;
            public EntityCommandBuffer buffer;
            public EntityArchetype entityArchetype;

            public void Execute()
            {
                Entity currentEntity = buffer.CreateEntity(entityArchetype);
                buffer.SetComponent(currentEntity, waterSourceData);
                buffer.SetComponent(currentEntity, transform);
                buffer.SetComponent(currentEntity, autoFillingLakeData);
                buffer.AddComponent<Updated>(currentEntity);
            }
        }

        /// <summary>
        /// This job adds a detention basin water source.
        /// </summary>
        private struct AddDetentionBasinJob : IJob
        {
            public Game.Simulation.WaterSourceData waterSourceData;
            public Game.Objects.Transform transform;
            public DetentionBasin detentionBasinData;
            public EntityCommandBuffer buffer;
            public EntityArchetype entityArchetype;

            public void Execute()
            {
                Entity currentEntity = buffer.CreateEntity(entityArchetype);
                buffer.SetComponent(currentEntity, waterSourceData);
                buffer.SetComponent(currentEntity, transform);
                buffer.SetComponent(currentEntity, detentionBasinData);
                buffer.AddComponent<Updated>(currentEntity);
            }
        }

        /// <summary>
        /// This job adds a retention basin water source.
        /// </summary>
        private struct AddRetentionBasinJob : IJob
        {
            public Game.Simulation.WaterSourceData waterSourceData;
            public Game.Objects.Transform transform;
            public RetentionBasin retentionBasinData;
            public EntityCommandBuffer buffer;
            public EntityArchetype entityArchetype;

            public void Execute()
            {
                Entity currentEntity = buffer.CreateEntity(entityArchetype);
                buffer.SetComponent(currentEntity, waterSourceData);
                buffer.SetComponent(currentEntity, transform);
                buffer.SetComponent(currentEntity, retentionBasinData);
                buffer.AddComponent<Updated>(currentEntity);
            }
        }

        /// <summary>
        /// This job renders circles related to the various water sources.
        /// </summary>
        private struct WaterSourceCirclesRenderJob : IJobChunk
        {
            public OverlayRenderSystem.Buffer m_OverlayBuffer;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;
            [ReadOnly]
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_SourceType;
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            public TerrainHeightData m_TerrainHeightData;
            public WaterSurfaceData m_WaterSurfaceData;
            [ReadOnly]
            public ComponentLookup<RetentionBasin> m_RetentionBasinLookup;
            [ReadOnly]
            public ComponentLookup<DetentionBasin> m_DetentionBasinLookup;
            [ReadOnly]
            public ComponentLookup<AutofillingLake> m_AutofillingLakeLookup;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Game.Simulation.WaterSourceData> waterSourceDataNativeArray = chunk.GetNativeArray(ref m_SourceType);
                NativeArray<Game.Objects.Transform> transformNativeArray = chunk.GetNativeArray(ref m_TransformType);
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                for (int i = 0; i < chunk.Count; i++)
                {

                    Game.Simulation.WaterSourceData currentWaterSourceData = waterSourceDataNativeArray[i];
                    if (currentWaterSourceData.m_Radius == 0f)
                    {
                        continue;
                    }

                    Game.Objects.Transform currentTransform = transformNativeArray[i];
                    float3 terrainPosition = new (currentTransform.m_Position.x, TerrainUtils.SampleHeight(ref m_TerrainHeightData, currentTransform.m_Position), currentTransform.m_Position.z);
                    float3 waterPosition = new (currentTransform.m_Position.x, WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, currentTransform.m_Position), currentTransform.m_Position.z);
                    float3 position = terrainPosition;
                    if (waterPosition.y > terrainPosition.y)
                    {
                        position = waterPosition;
                    }

                    UnityEngine.Color borderColor = GetWaterSourceColor(currentWaterSourceData.m_ConstantDepth);

                    if (m_RetentionBasinLookup.HasComponent(entityNativeArray[i]))
                    {
                        borderColor = UnityEngine.Color.magenta;
                    }
                    else if (m_DetentionBasinLookup.HasComponent(entityNativeArray[i]))
                    {
                        borderColor = new UnityEngine.Color(0.95f, 0.44f, 0.13f, 1f);
                    }
                    else if (m_AutofillingLakeLookup.HasComponent(entityNativeArray[i]))
                    {
                        borderColor = new UnityEngine.Color(0.422f, 0.242f, 0.152f);
                    }

                    UnityEngine.Color insideColor = borderColor;
                    insideColor.a = 0.1f;

                    float radius = Mathf.Clamp(currentWaterSourceData.m_Radius, 25f, 150f);
                    if (radius > currentWaterSourceData.m_Radius)
                    {
                        m_OverlayBuffer.DrawCircle(borderColor, insideColor, currentWaterSourceData.m_Radius / 20f, 0, new float2(0, 1), position, currentWaterSourceData.m_Radius * 2f);
                        m_OverlayBuffer.DrawCircle(borderColor, default, radius / 20f, 0, new float2(0, 1), position, radius * 2.05f);
                    }
                    else
                    {
                        m_OverlayBuffer.DrawCircle(borderColor, insideColor, radius / 20f, 0, new float2(0, 1), position, radius * 2f);
                        m_OverlayBuffer.DrawCircle(borderColor, default, currentWaterSourceData.m_Radius / 20f, 0, new float2(0, 1), position, currentWaterSourceData.m_Radius * 2.05f);
                    }
                }
            }

            private UnityEngine.Color GetWaterSourceColor(int constantDepth)
            {
                switch (constantDepth)
                {
                    case 0:
                        return UnityEngine.Color.red;
                    case 1:
                        return new UnityEngine.Color(0.422f, 0.242f, 0.152f);
                    case 2:
                        return UnityEngine.Color.yellow;
                    case 3:
                        return UnityEngine.Color.green;
                    default:
                        return UnityEngine.Color.red;
                }
            }
        }

        /// <summary>
        /// This job renders the circle for the current water source being placed.
        /// </summary>
        private struct WaterToolRadiusJob : IJob
        {
            public OverlayRenderSystem.Buffer m_OverlayBuffer;
            public float3 m_Position;
            public float m_Radius;
            public WaterToolUISystem.SourceType m_SourceType;

            public void Execute()
            {
                UnityEngine.Color borderColor = GetWaterSourceColor();
                UnityEngine.Color insideColor = borderColor;
                insideColor.a = 0.1f;

                float radius = Mathf.Clamp(m_Radius, 25f, 150f);
                if (radius > m_Radius)
                {
                    m_OverlayBuffer.DrawCircle(borderColor, insideColor, m_Radius / 20f, 0, new float2(0, 1), m_Position, m_Radius * 2f);
                    m_OverlayBuffer.DrawCircle(borderColor, default, radius / 20f, 0, new float2(0, 1), m_Position, radius * 2.05f);
                }
                else
                {
                    m_OverlayBuffer.DrawCircle(borderColor, insideColor, radius / 20f, 0, new float2(0, 1), m_Position, radius * 2f);
                    m_OverlayBuffer.DrawCircle(borderColor, default, m_Radius / 20f, 0, new float2(0, 1), m_Position, m_Radius * 2.05f);
                }
            }

            private UnityEngine.Color GetWaterSourceColor()
            {
                switch (m_SourceType)
                {
                    case WaterToolUISystem.SourceType.Stream:
                        return UnityEngine.Color.red;
                    case WaterToolUISystem.SourceType.VanillaLake:
                        return new UnityEngine.Color(0.422f, 0.242f, 0.152f);
                    case WaterToolUISystem.SourceType.River:
                        return UnityEngine.Color.yellow;
                    case WaterToolUISystem.SourceType.Sea:
                        return UnityEngine.Color.green;
                    case WaterToolUISystem.SourceType.Lake:
                        return new UnityEngine.Color(0.422f, 0.242f, 0.152f);
                    case WaterToolUISystem.SourceType.DetentionBasin:
                        return new UnityEngine.Color(0.95f, 0.44f, 0.13f, 1f);
                    case WaterToolUISystem.SourceType.RetentionBasin:
                        return UnityEngine.Color.magenta;
                    default:
                        return UnityEngine.Color.red;
                }
            }
        }

        /// <summary>
        /// This job draws the overlay for the projected water level.
        /// </summary>
        private struct WaterLevelProjectionJob : IJob
        {
            public OverlayRenderSystem.Buffer m_OverlayBuffer;
            public float3 m_Position;
            public float m_Radius;

            public void Execute()
            {
                m_OverlayBuffer.DrawCircle(new UnityEngine.Color(0f, 0f, 1f, 0.375f), m_Position, m_Radius * 6f);
            }
        }

        private struct HoverOverWaterSourceJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_SourceType;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;
            public float3 m_Position;
            public NativeList<Entity> m_Entities;
            public float m_MapExtents;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<Game.Simulation.WaterSourceData> waterSourceDataNativeArray = chunk.GetNativeArray(ref m_SourceType);
                NativeArray<Game.Objects.Transform> transformNativeArray = chunk.GetNativeArray(ref m_TransformType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity currentEntity = entityNativeArray[i];
                    Game.Simulation.WaterSourceData currentWaterSourceData = waterSourceDataNativeArray[i];
                    if (currentWaterSourceData.m_Radius == 0)
                    {
                        continue;
                    }

                    Game.Objects.Transform currentTransform = transformNativeArray[i];
                    m_Position.y = 0;
                    currentTransform.m_Position.y = 0;
                    if (math.distance(m_Position, currentTransform.m_Position) < Mathf.Clamp(currentWaterSourceData.m_Radius, 25f, 150f))
                    {
                        m_Entities.Add(in currentEntity);
                    }
                }
            }
        }

        private struct TypeHandle
        {
            [ReadOnly]
            public ComponentTypeHandle<Game.Simulation.WaterSourceData> __Game_Simulation_WaterSourceData_RO_ComponentTypeHandle;
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentLookup<RetentionBasin> __RententionBasin_Lookup;
            [ReadOnly]
            public ComponentLookup<DetentionBasin> __DetentionBasin_Lookup;
            [ReadOnly]
            public ComponentLookup<AutofillingLake> __AutofillingLake_Lookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AssignHandles(ref SystemState state)
            {
                __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                __Game_Simulation_WaterSourceData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Simulation.WaterSourceData>();
                __Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>();
                __DetentionBasin_Lookup = state.GetComponentLookup<DetentionBasin>();
                __RententionBasin_Lookup = state.GetComponentLookup<RetentionBasin>();
                __AutofillingLake_Lookup = state.GetComponentLookup<AutofillingLake>();
            }
        }
    }
}
