// <copyright file="AddPrefabsSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Systems
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Prefabs;
    using Game.Simulation;
    using Game.Tools;
    using Game.UI;
    using System.Collections.Generic;
    using Unity.Entities;
    using UnityEngine;
    using Water_Features.Domain;
    using Water_Features.Prefabs;
    using Water_Features.Tools;
    using static Game.UI.NameSystem;
    using static Water_Features.Tools.WaterToolUISystem;

    /// <summary>
    /// System for adding new prefabs for custom water sources.
    /// </summary>
    public partial class AddPrefabsSystem : GameSystemBase
    {
        /// <summary>
        ///  Prefab for name of water source prefabs.
        /// </summary>
        public const string PrefabPrefix = "WaterSource ";

        private const string TabName = "WaterTool";
        private const string CouiPathPrefix = "coui://uil/Colored/";
        private const string FileType = ".svg";

        /// <summary>
        /// Defined the data for the prefabs here.
        /// </summary>
        private List<WaterSourcePrefabData> m_SourcePrefabDataList = new List<WaterSourcePrefabData>()
        {
            { new WaterSourcePrefabData { m_SourceType = SourceType.Stream, m_Icon = $"{CouiPathPrefix}WaterSourceCreek{FileType}", m_HeightLocaleKey = "YY_WATER_FEATURES.Flow", m_Priority = 10, m_DefaultRadius = 5f, m_DefaultHeight = 1f, m_GameMode = GameMode.GameOrEditor, m_LegacyWaterSource = true, } },
            { new WaterSourcePrefabData { m_SourceType = SourceType.River, m_Icon = $"{CouiPathPrefix}WaterSourceRiver{FileType}", m_HeightLocaleKey = "YY_WATER_FEATURES.Depth", m_Priority = 20, m_DefaultRadius = 50f, m_DefaultHeight = 20f, m_GameMode = GameMode.GameOrEditor, m_LegacyWaterSource = true, } },
            { new WaterSourcePrefabData { m_SourceType = SourceType.Sea, m_Icon = $"{CouiPathPrefix}WaterSourceSea{FileType}", m_HeightLocaleKey = "YY_WATER_FEATURES.Depth", m_Priority = 70, m_DefaultRadius = 2500f, m_DefaultHeight = 25f, m_GameMode = GameMode.GameOrEditor, m_LegacyWaterSource = true, } },
            { new WaterSourcePrefabData { m_SourceType = SourceType.Lake, m_Icon = $"{CouiPathPrefix}WaterSourceLake{FileType}", m_HeightLocaleKey = "YY_WATER_FEATURES.Depth", m_Priority = 50, m_DefaultRadius = 20f, m_DefaultHeight = 15f, m_GameMode = GameMode.Game, m_LegacyWaterSource = true, } },
            { new WaterSourcePrefabData { m_SourceType = SourceType.Automated, m_Icon = $"{CouiPathPrefix}WaterSourceAutomaticFill{FileType}", m_HeightLocaleKey = "YY_WATER_FEATURES.Depth", m_Priority = 60, m_DefaultRadius = 20f, m_DefaultHeight = 15f, m_GameMode = GameMode.Game, m_LegacyWaterSource = true, } },
            { new WaterSourcePrefabData { m_SourceType = SourceType.DetentionBasin, m_Icon = $"{CouiPathPrefix}WaterSourceDetentionBasin{FileType}", m_HeightLocaleKey = "YY_WATER_FEATURES.MaxDepth", m_Priority = 30, m_DefaultRadius = 20f, m_DefaultHeight = 15f, m_GameMode = GameMode.Game, m_LegacyWaterSource = true, } },
            { new WaterSourcePrefabData { m_SourceType = SourceType.RetentionBasin, m_Icon = $"{CouiPathPrefix}WaterSourceRetentionBasin{FileType}", m_HeightLocaleKey = "YY_WATER_FEATURES.MaxDepth", m_Priority = 40, m_DefaultRadius = 25f, m_DefaultHeight = 20f, m_GameMode = GameMode.Game, m_LegacyWaterSource = true, } },
            { new WaterSourcePrefabData { m_SourceType = SourceType.VanillaLake, m_Icon = $"{CouiPathPrefix}WaterSourceLake{FileType}", m_HeightLocaleKey = "YY_WATER_FEATURES.Depth", m_Priority = 50, m_DefaultRadius = 20f, m_DefaultHeight = 15f, m_GameMode = GameMode.Editor, m_LegacyWaterSource = true, } },
            { new WaterSourcePrefabData { m_SourceType = SourceType.Generic, m_Icon = $"{CouiPathPrefix}WaterSourceLake{FileType}", m_HeightLocaleKey = "YY_WATER_FEATURES.Depth", m_Priority = 10, m_DefaultRadius = 20f, m_DefaultHeight = 15f, m_GameMode = GameMode.GameOrEditor, m_LegacyWaterSource = false, } },
            { new WaterSourcePrefabData { m_SourceType = SourceType.Seasonal, m_Icon = $"{CouiPathPrefix}WaterSourceCreek{FileType}", m_HeightLocaleKey = "YY_WATER_FEATURES.Depth", m_Priority = 20, m_DefaultRadius = 20f, m_DefaultHeight = 15f, m_GameMode = GameMode.GameOrEditor, m_LegacyWaterSource = false, } },
        };

        private PrefabSystem m_PrefabSystem;
        private ILog m_Log;
        private WaterSystem m_WaterSystem;
        private List<PrefabBase> m_Prefabs;
        private WaterSourcePrefabList m_PrefabList;
        private WaterToolUISystem m_UISystem;
        private ImageSystem m_ImageSystem;
        private ToolSystem m_ToolSystem;
        private SeasonalStreamsSystem m_SeasonalStreamsSystem;

        /// <summary>
        /// Gets the prefix for prefabs.
        /// </summary>
        public string Prefix { get => PrefabPrefix; }

        /// <summary>
        /// Adds or removes prefabs.
        /// </summary>
        public void ReviewPrefabs()
        {
            AddOrRemovePrefabs(m_ToolSystem.actionMode);
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = WaterFeaturesMod.Instance.Log;
            m_WaterSystem = World.GetOrCreateSystemManaged<WaterSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_UISystem = World.GetOrCreateSystemManaged<WaterToolUISystem>();
            m_SeasonalStreamsSystem = World.GetOrCreateSystemManaged<SeasonalStreamsSystem>();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_Prefabs = new List<PrefabBase>();
            m_PrefabList = new WaterSourcePrefabList() { waterSourcePrefabUIDatas = new List<WaterSourcePrefabUIData>() };
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            base.OnCreate();
            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);

            if (m_Prefabs.Count == 0)
            {
                // This goes through the list of prefabs and created the prefabs and the UIObject Component.
                foreach (WaterSourcePrefabData source in m_SourcePrefabDataList)
                {
                    WaterSourcePrefab sourcePrefabBase = ScriptableObject.CreateInstance<WaterSourcePrefab>();
                    sourcePrefabBase.active = true;
                    sourcePrefabBase.m_SourceType = source.m_SourceType;
                    sourcePrefabBase.m_DefaultRadius = source.m_DefaultRadius;
                    sourcePrefabBase.m_HeightLocaleKey = source.m_HeightLocaleKey;
                    sourcePrefabBase.m_DefaultHeight = source.m_DefaultHeight;
                    sourcePrefabBase.name = $"{PrefabPrefix}{source.m_SourceType}";
                    sourcePrefabBase.m_GameMode = source.m_GameMode;
                    sourcePrefabBase.m_LegacyWaterSource = source.m_LegacyWaterSource;
                    Game.Prefabs.WaterSource waterSource = ScriptableObject.CreateInstance<Game.Prefabs.WaterSource>();
                    waterSource.m_Polluted = 0f;
                    waterSource.m_Radius = source.m_DefaultRadius;
                    waterSource.m_Height = source.m_DefaultHeight;
                    sourcePrefabBase.AddComponentFrom(waterSource);
                    UIObject uiObject = ScriptableObject.CreateInstance<UIObject>();
                    uiObject.m_Group = GetOrCreateNewToolCategory(TabName, "Landscaping", "coui://ui-mods/images/water_features_icon.svg") ?? uiObject.m_Group;
                    uiObject.m_Priority = source.m_Priority;
                    uiObject.m_Icon = source.m_Icon;
                    uiObject.active = true;
                    uiObject.m_IsDebugObject = false;
                    sourcePrefabBase.AddComponentFrom(uiObject);
                    if (m_PrefabSystem.AddPrefab(sourcePrefabBase))
                    {
                        m_Prefabs.Add(sourcePrefabBase);
                        m_PrefabList.Add(sourcePrefabBase, source.m_Icon);
                        m_Log.Info($"{nameof(AddPrefabsSystem)}.{nameof(OnGamePreload)} Added prefab for Water Source {source.m_SourceType}");
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            m_SeasonalStreamsSystem.SetSeasonalStreamsSetting(purpose, mode);

            AddOrRemovePrefabs(mode);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            Enabled = false;
            return;
        }

        /// <summary>
        /// Gets or creates a new tool tab catergory.
        /// </summary>
        /// <param name="name">Name of the new tab category.</param>
        /// <param name="menuName">Name of the menu that it is being inserted into.</param>
        /// <param name="path">Icon path.</param>
        /// <returns>A prefab component for the new AssetCategory tab.</returns>
        private UIAssetCategoryPrefab GetOrCreateNewToolCategory(string name, string menuName, string path)
        {
            if (m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(UIAssetCategoryPrefab), name), out var prefab) &&
                prefab is UIAssetCategoryPrefab)
            {
                return (UIAssetCategoryPrefab)prefab;
            }

            if (!m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(UIAssetMenuPrefab), menuName), out var prefabBase) ||
                prefabBase is not UIAssetMenuPrefab menu)
            {
                // it can happen that the menu isn't added yet, as is the case on the first run through
                return null;
            }

            UIAssetCategoryPrefab newCategory = ScriptableObject.CreateInstance<UIAssetCategoryPrefab>();
            newCategory.name = name;
            newCategory.m_Menu = menu;
            UIObject uiObjectComponent = ScriptableObject.CreateInstance<UIObject>();
            uiObjectComponent.m_Icon = path;
            uiObjectComponent.m_Priority = 21;
            uiObjectComponent.active = true;
            uiObjectComponent.m_IsDebugObject = false;
            newCategory.AddComponentFrom(uiObjectComponent);
            if (m_PrefabSystem.AddPrefab(newCategory))
            {
                m_Log.Info($"{nameof(AddPrefabsSystem)}.{nameof(OnUpdate)} Added prefab for Category {name}");
            }

            return newCategory;
        }

        private string GetSource(WaterSourcePrefab prefab)
        {
            if (m_PrefabSystem.TryGetEntity(prefab, out Entity prefabEntity))
            {
                return m_ImageSystem.GetIconOrGroupIcon(prefabEntity);
            }

            foreach (WaterSourcePrefabData waterSourcePrefabData in m_SourcePrefabDataList)
            {
                if (prefab.m_SourceType == waterSourcePrefabData.m_SourceType)
                {
                    return waterSourcePrefabData.m_Icon;
                }
            }

            return m_ImageSystem.placeholderIcon;
        }

        private bool ValidGameMode(GameMode gameMode, WaterSourcePrefab prefab)
        {
            if (gameMode == GameMode.Game &&
               (prefab.m_GameMode == GameMode.Game ||
                prefab.m_GameMode == GameMode.GameOrEditor))
            {
                return true;
            }

            if (gameMode == GameMode.Editor &&
               (prefab.m_GameMode == GameMode.Editor ||
                prefab.m_GameMode == GameMode.GameOrEditor))
            {
                return true;
            }

            return false;
        }

        private void AddOrRemovePrefabs(GameMode mode)
        {
            foreach (WaterSourcePrefab waterSource in m_Prefabs)
            {
                if (m_WaterSystem.UseLegacyWaterSources == waterSource.m_LegacyWaterSource &&
                    ValidGameMode(mode, waterSource) &&
                   !m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(WaterSourcePrefab), waterSource.name), out _) &&
                   (waterSource.m_SourceType != SourceType.DetentionBasin || WaterFeaturesMod.Instance.Settings.IncludeDetentionBasins) &&
                   (waterSource.m_SourceType != SourceType.RetentionBasin || WaterFeaturesMod.Instance.Settings.IncludeRetentionBasins) &&
                   (waterSource.m_SourceType != SourceType.Seasonal || WaterFeaturesMod.Instance.Settings.EnableSeasonalStreams))
                {
                    m_PrefabSystem.AddPrefab(waterSource);
                    m_PrefabList.Add(waterSource, GetSource(waterSource));
                }

                if (m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(WaterSourcePrefab), waterSource.name), out PrefabBase waterSourcePrefab) &&
                    m_PrefabSystem.TryGetEntity(waterSourcePrefab, out Entity waterSourcePrefabEntity) &&
                   (m_WaterSystem.UseLegacyWaterSources != waterSource.m_LegacyWaterSource ||
                    !ValidGameMode(mode, waterSource) ||
                   (waterSource.m_SourceType == SourceType.DetentionBasin && !WaterFeaturesMod.Instance.Settings.IncludeDetentionBasins) ||
                   (waterSource.m_SourceType == SourceType.RetentionBasin && !WaterFeaturesMod.Instance.Settings.IncludeRetentionBasins) ||
                   (waterSource.m_SourceType == SourceType.Seasonal && !WaterFeaturesMod.Instance.Settings.EnableSeasonalStreams)))
                {
                    if (m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(UIAssetCategoryPrefab), TabName), out var prefab) &&
                        prefab is UIAssetCategoryPrefab &&
                        m_PrefabSystem.TryGetEntity(prefab, out Entity prefabEntity) &&
                        EntityManager.TryGetBuffer(prefabEntity, isReadOnly: false, out DynamicBuffer<UIGroupElement> groupElements))
                    {
                        for (int i = 0; i < groupElements.Length; i++)
                        {
                            if (groupElements[i].m_Prefab == waterSourcePrefabEntity)
                            {
                                groupElements.RemoveAt(i);
                                break;
                            }
                        }
                    }

                    m_PrefabSystem.RemovePrefab(waterSource);
                    m_PrefabList.Remove(waterSource);
                }
            }

            if (mode == GameMode.Editor)
            {
                m_UISystem.SetPrefabList(m_PrefabList);
            }
        }

        /// <summary>
        /// A struct with all the data for a new WaterSource Prefab. It's possible this should be replaced by a custom component.
        /// </summary>
        private struct WaterSourcePrefabData
        {
            /// <summary>
            /// The type of water source.
            /// </summary>
            public SourceType m_SourceType;

            /// <summary>
            /// The string path to the icon.
            /// </summary>
            public string m_Icon;

            /// <summary>
            /// The string key code for the localization of the row that coorelates with the height field. Previously amount.
            /// </summary>
            public string m_HeightLocaleKey;

            /// <summary>
            /// An interger value for where to put the icon in the menu.
            /// </summary>
            public int m_Priority;

            /// <summary>
            /// The default radius to use with this water source.
            /// </summary>
            public float m_DefaultRadius;

            /// <summary>
            /// The default amount to use with this water source. Previuosly amount.
            /// </summary>
            public float m_DefaultHeight;

            /// <summary>
            /// Defines if used in game, editor, or both.
            /// </summary>
            public GameMode m_GameMode;

            /// <summary>
            /// Defines if legacy water source or modern.
            /// </summary>
            public bool m_LegacyWaterSource;
        }

    }
}
