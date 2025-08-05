// <copyright file="WaterTooltipSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Tools
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Game.Simulation;
    using Game.Tools;
    using Game.UI.Localization;
    using Game.UI.Tooltip;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using Water_Features.Components;
    using Water_Features.Prefabs;

    /// <summary>
    /// A system for handing the tooltip for custom water tool.
    /// </summary>
    public partial class WaterTooltipSystem : TooltipSystemBase
    {
        private Vector3 m_HitPosition = new ();
        private bool m_RadiusTooSmall = false;
        private ToolSystem m_ToolSystem;
        private CustomWaterToolSystem m_CustomWaterTool;
        private float m_TimeLastWarned;
        private float m_StartedHoveringTime;
        private ILog m_Log;
        private WaterToolUISystem m_WaterToolUISystem;
        private WaterSystem m_WaterSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaterTooltipSystem"/> class.
        /// </summary>
        public WaterTooltipSystem()
        {
        }

        /// <summary>
        /// Sets a value indicating the hit position.
        /// </summary>
        public Vector3 HitPosition
        {
            set { m_HitPosition = value; }
        }

        /// <summary>
        /// Sets a value indicating whether the radius is too small.
        /// </summary>
        public bool RadiusTooSmall
        {
            set { m_RadiusTooSmall = value; }
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = WaterFeaturesMod.Instance.Log;
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_WaterSystem = World.GetOrCreateSystemManaged<WaterSystem>();
            m_CustomWaterTool = World.GetOrCreateSystemManaged<CustomWaterToolSystem>();
            m_WaterToolUISystem = World.GetOrCreateSystemManaged<WaterToolUISystem>();
            m_Log.Info($"[{nameof(WaterTooltipSystem)}] {nameof(OnCreate)}");
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (m_ToolSystem.activeTool != m_CustomWaterTool)
            {
                return;
            }

            var prefab = m_CustomWaterTool.GetPrefab();
            if (m_WaterToolUISystem.ToolMode != CustomWaterToolSystem.ToolModes.PlaceWaterSource)
            {
                prefab = m_CustomWaterTool.GetSelectedPrefab();
            }

            Entity hoveredWaterSourceEntity = m_CustomWaterTool.GetHoveredEntity(m_HitPosition);

            if (prefab != null && prefab is WaterSourcePrefab)
            {
                WaterSourcePrefab waterSourcePrefab = prefab as WaterSourcePrefab;
                float radius = m_WaterToolUISystem.Radius;
                if ((m_WaterToolUISystem.ToolMode == CustomWaterToolSystem.ToolModes.MoveWaterSource || m_WaterToolUISystem.ToolMode == CustomWaterToolSystem.ToolModes.RadiusChange) && m_CustomWaterTool.TryGetSelectedRadius(out float waterSourceRadius))
                {
                    radius = waterSourceRadius;
                }

                Vector3 position = m_HitPosition;
                if (m_WaterToolUISystem.ToolMode == CustomWaterToolSystem.ToolModes.RadiusChange && m_CustomWaterTool.TryGetSelectedPosition(out float3 waterSourcePosition))
                {
                    position = waterSourcePosition;
                }

                if ((hoveredWaterSourceEntity == Entity.Null && m_WaterToolUISystem.ToolMode != CustomWaterToolSystem.ToolModes.ElevationChange) || m_WaterToolUISystem.ToolMode == CustomWaterToolSystem.ToolModes.MoveWaterSource)
                {
                    // Checks position of river and displays tooltip if needed.
                    if (waterSourcePrefab.m_SourceType == WaterToolUISystem.SourceType.River)
                    {
                        if (!m_CustomWaterTool.IsPositionNearBorder(position, radius, true))
                        {
                            StringTooltip mustBePlacedNearMapBorderTooltip = new ()
                            {
                                path = "Tooltip.LABEL[YY.WT.PlaceNearBorder]",
                                value = LocalizedString.IdWithFallback("Tooltip.LABEL[YY.WT.PlaceNearBorder]", "Rivers must be placed near map border."),
                            };
                            AddMouseTooltip(mustBePlacedNearMapBorderTooltip);
                        }
                    }

                    // Checks position of sea and displays tooltip if needed.
                    else if (waterSourcePrefab.m_SourceType == WaterToolUISystem.SourceType.Sea)
                    {
                        if (!m_CustomWaterTool.IsPositionNearBorder(position, radius, false))
                        {
                            StringTooltip mustTouchBorderTooltip = new ()
                            {
                                path = "Tooltip.LABEL[YY.WT.MustTouchBorder]",
                                value = LocalizedString.IdWithFallback("Tooltip.LABEL[YY.WT.MustTouchBorder]", "Sea water sources must touch the map border."),
                            };
                            AddMouseTooltip(mustTouchBorderTooltip);
                        }
                    }

                    // Checks position of water sources placed in playable area and displays tooltip if needed.
                    else if (m_WaterSystem.UseLegacyWaterSources)
                    {
                        if (!m_CustomWaterTool.IsPositionWithinBorder(position))
                        {
                            StringTooltip mustBePlacedInsideBorderTooltip = new ()
                            {
                                path = "Tooltip.LABEL[YY.WT.PlaceInsideBorder]",
                                value = LocalizedString.IdWithFallback("Tooltip.LABEL[YY.WT.PlaceInsideBorder]", "This water source must be placed inside the playable map."),
                            };
                            AddMouseTooltip(mustBePlacedInsideBorderTooltip);
                        }
                    }
                }

                // Informs the player if they can set the elevation by right clicking.
                if (waterSourcePrefab.m_SourceType != WaterToolUISystem.SourceType.Stream && !m_WaterToolUISystem.HeightIsAnElevation && m_WaterToolUISystem.ToolMode == CustomWaterToolSystem.ToolModes.PlaceWaterSource)
                {
                    StringTooltip lockElevationTooltip = new ()
                    {
                        path = "Tooltip.LABEL[YY.WT.LockElevation]",
                        value = LocalizedString.IdWithFallback("Tooltip.LABEL[YY.WT.LockElevation]", "Right click to designate the water surface elevation."),
                    };
                    AddMouseTooltip(lockElevationTooltip);
                }

                if (waterSourcePrefab.m_SourceType != WaterToolUISystem.SourceType.Stream && m_WaterToolUISystem.ToolMode == CustomWaterToolSystem.ToolModes.ElevationChange)
                {
                    m_StartedHoveringTime = 0;
                    FloatTooltip newElevationTooltip = new FloatTooltip
                    {
                        value = m_HitPosition.y,
                        unit = "floatSingleFraction",
                        path = "YY_WATER_FEATURES.ElevationChange",
                        label = LocalizedString.IdWithFallback("YY_WATER_FEATURES.Elevation", "Elevation"),
                    };
                    AddMouseTooltip(newElevationTooltip);
                }

                if (m_WaterToolUISystem.ToolMode == CustomWaterToolSystem.ToolModes.RadiusChange)
                {
                    m_StartedHoveringTime = 0;
                    FloatTooltip radiusTooltip = new FloatTooltip
                    {
                        value = radius,
                        unit = "floatSingleFraction",
                        path = "YY_WATER_FEATURES.RadiusChange",
                        label = LocalizedString.IdWithFallback("YY_WATER_FEATURES.Radius", "Radius"),
                    };
                    AddMouseTooltip(radiusTooltip);
                }
            }

            // If Radius is too small displays a tooltip.
            if (m_RadiusTooSmall || UnityEngine.Time.time < m_TimeLastWarned + 3f)
            {
                StringTooltip radiusTooSmallTooltip = new ()
                {
                    path = "Tooltip.LABEL[YY.WT.RadiusTooSmall]",
                    value = LocalizedString.IdWithFallback("Tooltip.LABEL[YY.WT.RadiusTooSmall]", "The radius is too small and has been automically increased."),
                };
                AddMouseTooltip(radiusTooSmallTooltip);
                if (m_RadiusTooSmall)
                {
                    m_TimeLastWarned = UnityEngine.Time.time;
                }

                m_RadiusTooSmall = false;
            }

            // Displays a tooltip while hovering over a water source.
            if (hoveredWaterSourceEntity != Entity.Null && m_CustomWaterTool.GetSelectedPrefab() == null)
            {
                if (m_StartedHoveringTime == 0)
                {
                    m_StartedHoveringTime = UnityEngine.Time.time;
                }

                if (UnityEngine.Time.time > m_StartedHoveringTime + 1f)
                {
                    if (EntityManager.TryGetComponent(hoveredWaterSourceEntity, out Game.Simulation.WaterSourceData waterSourceData))
                    {
                        string heightLocaleKey = "YY_WATER_FEATURES.Elevation";
                        string fallback = "Elevation";
                        if (waterSourceData.m_ConstantDepth == 0 && m_WaterSystem.UseLegacyWaterSources)
                        {
                            heightLocaleKey = "YY_WATER_FEATURES.Flow";
                            fallback = "Flow";
                        }

                        FloatTooltip heightTooltip = new FloatTooltip
                        {
                            value = waterSourceData.m_Height,
                            unit = "floatSingleFraction",
                            path = heightLocaleKey,
                            label = LocalizedString.IdWithFallback(heightLocaleKey, fallback),
                        };
                        if (!m_WaterSystem.UseLegacyWaterSources &&
                            EntityManager.TryGetComponent(hoveredWaterSourceEntity, out Game.Objects.Transform transform))
                        {
                            heightTooltip.value = waterSourceData.m_Height + transform.m_Position.y;
                        }

                        AddMouseTooltip(heightTooltip);

                        FloatTooltip radiusTooltip = new FloatTooltip
                        {
                            value = waterSourceData.m_Radius,
                            unit = "floatSingleFraction",
                            path = "YY_WATER_FEATURES.Radius",
                            label = LocalizedString.IdWithFallback("YY_WATER_FEATURES.Radius", "Radius"),
                        };
                        AddMouseTooltip(radiusTooltip);
                    }

                    if (EntityManager.TryGetComponent(hoveredWaterSourceEntity, out DetentionBasin detentionBasin))
                    {
                        FloatTooltip maxElevationTooptip = new FloatTooltip
                        {
                            value = detentionBasin.m_MaximumWaterHeight,
                            unit = "floatSingleFraction",
                            path = "YY_WATER_FEATURES.MaxElevation",
                            label = LocalizedString.IdWithFallback("YY_WATER_FEATURES.MaxElevation", "Max Elevation"),
                        };
                        AddMouseTooltip(maxElevationTooptip);
                        if (WaterFeaturesMod.Instance.Settings.SimulateSnowMelt)
                        {
                            FloatTooltip snowAccumulation = new FloatTooltip
                            {
                                value = detentionBasin.m_SnowAccumulation,
                                unit = "floatSingleFraction",
                                path = "YY_WATER_FEATURES.SnowAccumulation",
                                label = LocalizedString.IdWithFallback("YY_WATER_FEATURES.SnowAccumulation", "Snow Accumulation"),
                            };
                            AddMouseTooltip(snowAccumulation);
                        }
                    }
                    else if (EntityManager.TryGetComponent(hoveredWaterSourceEntity, out AutofillingLake lake))
                    {
                        FloatTooltip maxElevationTooptip = new FloatTooltip
                        {
                            value = lake.m_MaximumWaterHeight,
                            unit = "floatSingleFraction",
                            path = "YY_WATER_FEATURES.MaxElevation",
                            label = LocalizedString.IdWithFallback("YY_WATER_FEATURES.MaxElevation", "Max Elevation"),
                        };
                        AddMouseTooltip(maxElevationTooptip);
                    }
                    else if (EntityManager.TryGetComponent(hoveredWaterSourceEntity, out AutomatedWaterSource automatedWaterSource))
                    {
                        FloatTooltip maxElevationTooptip = new FloatTooltip
                        {
                            value = automatedWaterSource.m_MaximumWaterHeight,
                            unit = "floatSingleFraction",
                            path = "YY_WATER_FEATURES.MaxElevation",
                            label = LocalizedString.IdWithFallback("YY_WATER_FEATURES.MaxElevation", "Max Elevation"),
                        };
                        AddMouseTooltip(maxElevationTooptip);
                    }
                    else if (EntityManager.TryGetComponent(hoveredWaterSourceEntity, out RetentionBasin retentionBasin))
                    {
                        FloatTooltip maxElevationTooptip = new FloatTooltip
                        {
                            value = retentionBasin.m_MaximumWaterHeight,
                            unit = "floatSingleFraction",
                            path = "YY_WATER_FEATURES.MaxElevation",
                            label = LocalizedString.IdWithFallback("YY_WATER_FEATURES.MaxElevation", "Max Elevation"),
                        };
                        AddMouseTooltip(maxElevationTooptip);
                        FloatTooltip minElevationTooptip = new FloatTooltip
                        {
                            value = retentionBasin.m_MinimumWaterHeight,
                            unit = "floatSingleFraction",
                            path = "YY_WATER_FEATURES.MinElevation",
                            label = LocalizedString.IdWithFallback("YY_WATER_FEATURES.MinElevation", "Min Elevation"),
                        };
                        AddMouseTooltip(minElevationTooptip);
                        if (WaterFeaturesMod.Instance.Settings.SimulateSnowMelt)
                        {
                            FloatTooltip snowAccumulation = new FloatTooltip
                            {
                                value = retentionBasin.m_SnowAccumulation,
                                unit = "floatSingleFraction",
                                path = "YY_WATER_FEATURES.SnowAccumulation",
                                label = LocalizedString.IdWithFallback("YY_WATER_FEATURES.SnowAccumulation", "Snow Accumulation"),
                            };
                            AddMouseTooltip(snowAccumulation);
                        }
                    }
                    else if (EntityManager.TryGetComponent(hoveredWaterSourceEntity, out SeasonalStreamsData seasonalStreamsData))
                    {
                        FloatTooltip originalAmount = new FloatTooltip
                        {
                            value = seasonalStreamsData.m_OriginalAmount,
                            unit = "floatSingleFraction",
                            path = "YY_WATER_FEATURES.OriginalFlow",
                            label = LocalizedString.IdWithFallback("YY_WATER_FEATURES.OriginalFlow", "Original Flow"),
                        };
                        AddMouseTooltip(originalAmount);

                        if (WaterFeaturesMod.Instance.Settings.SimulateSnowMelt)
                        {
                            FloatTooltip snowAccumulation = new FloatTooltip
                            {
                                value = seasonalStreamsData.m_SnowAccumulation,
                                unit = "floatSingleFraction",
                                path = "YY_WATER_FEATURES.SnowAccumulation",
                                label = LocalizedString.IdWithFallback("YY_WATER_FEATURES.SnowAccumulation", "Snow Accumulation"),
                            };
                            AddMouseTooltip(snowAccumulation);
                        }
                    }
                    else if (EntityManager.TryGetComponent(hoveredWaterSourceEntity, out TidesAndWavesData tidesAndWavesData))
                    {
                        FloatTooltip maxElevationTooptip = new FloatTooltip
                        {
                            value = tidesAndWavesData.m_OriginalAmount,
                            unit = "floatSingleFraction",
                            path = "YY_WATER_FEATURES.MaxElevation",
                            label = LocalizedString.IdWithFallback("YY_WATER_FEATURES.MaxElevation", "Max Elevation"),
                        };
                        AddMouseTooltip(maxElevationTooptip);
                    }
                }

                if (m_WaterToolUISystem.ToolMode == CustomWaterToolSystem.ToolModes.PlaceWaterSource)
                {
                    StringTooltip removeWaterSourceTooltip = new ()
                    {
                        path = "Tooltip.LABEL[YY.WT.RemoveWaterSource]",
                        value = LocalizedString.IdWithFallback("Tooltip.LABEL[YY.WT.RemoveWaterSource]", "Right click to remove water source."),
                    };
                    AddMouseTooltip(removeWaterSourceTooltip);
                }
            }
            else
            {
                m_StartedHoveringTime = 0;
            }
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
