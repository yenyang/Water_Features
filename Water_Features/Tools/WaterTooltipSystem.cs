// <copyright file="WaterTooltipSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Tools
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Game.Simulation;
    using Game.Tools;
    using Game.UI;
    using Game.UI.Localization;
    using Game.UI.Tooltip;
    using Unity.Entities;
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
            m_ToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            m_CustomWaterTool = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<CustomWaterToolSystem>();
            m_WaterToolUISystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<WaterToolUISystem>();
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
            if (prefab == null || prefab is not WaterSourcePrefab)
            {
                return;
            }

            WaterSourcePrefab waterSourcePrefab = prefab as WaterSourcePrefab;

            // Checks position of river and displays tooltip if needed.
            if (waterSourcePrefab.m_SourceType == WaterToolUISystem.SourceType.River)
            {
                if (!m_CustomWaterTool.IsPositionNearBorder(m_HitPosition, m_WaterToolUISystem.Radius, true))
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
                if (!m_CustomWaterTool.IsPositionNearBorder(m_HitPosition, m_WaterToolUISystem.Radius, false))
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
            else
            {
                if (!m_CustomWaterTool.IsPositionWithinBorder(m_HitPosition))
                {
                    StringTooltip mustBePlacedInsideBorderTooltip = new ()
                    {
                        path = "Tooltip.LABEL[YY.WT.PlaceInsideBorder]",
                        value = LocalizedString.IdWithFallback("Tooltip.LABEL[YY.WT.PlaceInsideBorder]", "This water source must be placed inside the playable map."),
                    };
                    AddMouseTooltip(mustBePlacedInsideBorderTooltip);
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
            if (m_CustomWaterTool.CanDeleteWaterSource())
            {
                if (m_StartedHoveringTime == 0)
                {
                    m_StartedHoveringTime = UnityEngine.Time.time;
                }

                if (UnityEngine.Time.time > m_StartedHoveringTime + 1f)
                {
                    Entity entity = m_CustomWaterTool.GetHoveredEntity(m_HitPosition);
                    if (entity != Entity.Null)
                    {
                        if (EntityManager.TryGetComponent(entity, out WaterSourceData waterSourceData))
                        {
                            string amountLocaleKey = "YY_WATER_FEATURES.Elevation";
                            string fallback = "Elevation";
                            if (waterSourceData.m_ConstantDepth == 0)
                            {
                                amountLocaleKey = "YY_WATER_FEATURES.Flow";
                                fallback = "Flow";
                            }

                            FloatTooltip amountTooltip = new FloatTooltip
                            {
                                value = waterSourceData.m_Amount,
                                unit = "floatSingleFraction",
                                path = amountLocaleKey,
                                label = LocalizedString.IdWithFallback(amountLocaleKey, fallback),
                            };
                            AddMouseTooltip(amountTooltip);

                            FloatTooltip radiusTooltip = new FloatTooltip
                            {
                                value = waterSourceData.m_Radius,
                                unit = "floatSingleFraction",
                                path = "YY_WATER_FEATURES.Radius",
                                label = LocalizedString.IdWithFallback("YY_WATER_FEATURES.Radius", "Radius"),
                            };
                            AddMouseTooltip(radiusTooltip);
                        }

                        if (EntityManager.TryGetComponent(entity, out DetentionBasin detentionBasin))
                        {
                            FloatTooltip maxElevationTooptip = new FloatTooltip
                            {
                                value = detentionBasin.m_MaximumWaterHeight,
                                unit = "floatSingleFraction",
                                path = "YY_WATER_FEATURES.MaxElevation",
                                label = LocalizedString.IdWithFallback("YY_WATER_FEATURES.MaxElevation", "Max Elevation"),
                            };
                            AddMouseTooltip(maxElevationTooptip);
                            if (WaterFeaturesMod.Settings.SimulateSnowMelt)
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
                        else if (EntityManager.TryGetComponent(entity, out AutofillingLake lake))
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
                        else if (EntityManager.TryGetComponent(entity, out RetentionBasin retentionBasin))
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
                            if (WaterFeaturesMod.Settings.SimulateSnowMelt)
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
                        else if (EntityManager.TryGetComponent(entity, out SeasonalStreamsData seasonalStreamsData))
                        {
                            FloatTooltip originalAmount = new FloatTooltip
                            {
                                value = seasonalStreamsData.m_OriginalAmount,
                                unit = "floatSingleFraction",
                                path = "YY_WATER_FEATURES.OriginalFlow",
                                label = LocalizedString.IdWithFallback("YY_WATER_FEATURES.OriginalFlow", "Original Flow"),
                            };
                            AddMouseTooltip(originalAmount);

                            if (WaterFeaturesMod.Settings.SimulateSnowMelt)
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
                        else if (EntityManager.TryGetComponent(entity, out TidesAndWavesData tidesAndWavesData))
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
                }

                StringTooltip removeWaterSourceTooltip = new ()
                {
                    path = "Tooltip.LABEL[YY.WT.RemoveWaterSource]",
                    value = LocalizedString.IdWithFallback("Tooltip.LABEL[YY.WT.RemoveWaterSource]", "Right click to remove water source."),
                };
                AddMouseTooltip(removeWaterSourceTooltip);
            }

            // Informs the player if they can set the elevation by right clicking.
            else if (waterSourcePrefab.m_SourceType != WaterToolUISystem.SourceType.Stream && !m_WaterToolUISystem.AmountIsAnElevation)
            {
                m_StartedHoveringTime = 0;
                StringTooltip lockElevationTooltip = new ()
                {
                    path = "Tooltip.LABEL[YY.WT.LockElevation]",
                    value = LocalizedString.IdWithFallback("Tooltip.LABEL[YY.WT.LockElevation]", "Right click to designate the water surface elevation."),
                };
                AddMouseTooltip(lockElevationTooltip);
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
