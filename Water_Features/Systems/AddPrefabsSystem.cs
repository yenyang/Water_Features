// <copyright file="AddPrefabsSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Systems
{
    using System.Collections.Generic;
    using System.Security.Policy;
    using Colossal.Entities;
    using Colossal.Logging;
    using Game;
    using Game.Prefabs;
    using Game.Simulation;
    using Game.UI;
    using Unity.Entities;
    using UnityEngine;
    using Water_Features.Domain;
    using Water_Features.Prefabs;
    using Water_Features.Tools;
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
        };

        private PrefabSystem m_PrefabSystem;
        private ILog m_Log;
        private WaterSystem m_WaterSystem;
        private List<PrefabBase> m_Prefabs;
        private WaterSourcePrefabList m_PrefabList;
        private WaterToolUISystem m_UISystem;
        private ImageSystem m_ImageSystem;

        /// <summary>
        /// Gets the prefix for prefabs.
        /// </summary>
        public string Prefix { get => PrefabPrefix; }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = WaterFeaturesMod.Instance.Log;
            m_WaterSystem = World.GetOrCreateSystemManaged<WaterSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_UISystem = World.GetOrCreateSystemManaged<WaterToolUISystem>();
            m_Prefabs = new List<PrefabBase>();
            m_PrefabList = new WaterSourcePrefabList() { waterSourcePrefabUIDatas = new List<WaterSourcePrefabUIData>() };
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            base.OnCreate();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
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
                    m_Log.Info($"{nameof(AddPrefabsSystem)}.{nameof(OnUpdate)} Added prefab for Water Source {source.m_SourceType}");
                }
            }

            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            // For some reason these components are not assigned automatically from the ones in the prefab so I am manually assigning them to the prefab entities.
            if (m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(UIAssetCategoryPrefab), TabName), out var waterToolTabPrefab) || waterToolTabPrefab is UIAssetCategoryPrefab)
            {
                if (!m_PrefabSystem.TryGetEntity(waterToolTabPrefab, out Entity waterToolTabPrefabEntity))
                {
                    m_Log.Warn($"{nameof(AddPrefabsSystem)}.{nameof(OnGameLoadingComplete)} Couldn't find waterToolTabPrefabEntity in waterToolTabPrefab for {waterToolTabPrefab.GetPrefabID()}.");
                    return;
                }

                if (!EntityManager.TryGetComponent(waterToolTabPrefabEntity, out UIAssetCategoryData currentMenu))
                {
                    m_Log.Warn($"{nameof(AddPrefabsSystem)}.{nameof(OnGameLoadingComplete)} Couldn't find UIAssetCategoryData in waterToolTabPrefabEntity for {waterToolTabPrefab.GetPrefabID()}.");
                    return;
                }

                if (!EntityManager.TryGetComponent(waterToolTabPrefabEntity, out UIObjectData objectData))
                {
                    m_Log.Warn($"{nameof(AddPrefabsSystem)}.{nameof(OnGameLoadingComplete)} Couldn't find UIObjectData in waterToolTabPrefabEntity for {waterToolTabPrefab.GetPrefabID()}.");
                    return;
                }

                // If all the requried components have been found and the data wasn't assigned automatically. Then this adds the data to get the water tool tab into the right toolbar menu.
                if (currentMenu.m_Menu == Entity.Null && m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(UIAssetMenuPrefab), "Landscaping"), out PrefabBase landscapeTabPrefab))
                {
                    Entity landscapeTabEntity = m_PrefabSystem.GetEntity(landscapeTabPrefab);
                    m_Log.Info($"{nameof(AddPrefabsSystem)}.{nameof(OnGameLoadingComplete)} currentMenu = Entity.Null so set to {landscapeTabEntity.Index}.{landscapeTabEntity.Version}");
                    currentMenu.m_Menu = landscapeTabEntity;
                    objectData.m_Priority = 21;
                    EntityManager.SetComponentData(waterToolTabPrefabEntity, currentMenu);
                    EntityManager.SetComponentData(waterToolTabPrefabEntity, objectData);
                    if (!EntityManager.TryGetBuffer(landscapeTabEntity, false, out DynamicBuffer<UIGroupElement> uiGroupBuffer))
                    {
                        m_Log.Warn($"{nameof(AddPrefabsSystem)}.{nameof(OnGameLoadingComplete)} Couldn't find UIGroupElement buffer in landscapeTabEntity.");
                        return;
                    }

                    UIGroupElement groupElement = new UIGroupElement()
                    {
                        m_Prefab = waterToolTabPrefabEntity,
                    };
                    uiGroupBuffer.Add(groupElement);
                }
                else
                {
                    if (currentMenu.m_Menu == Entity.Null)
                    {
                        m_Log.Warn($"{nameof(AddPrefabsSystem)}.{nameof(OnGameLoadingComplete)} Couldn't find Landscaping tab.");
                    }
                }
            }

            foreach (WaterSourcePrefab waterSource in m_Prefabs)
            {
                m_Log.Debug($"{nameof(AddPrefabsSystem)}.{nameof(OnGameLoadingComplete)} reviewing water sources prefabs.");
                m_Log.Debug($"waterSource.m_SourceType: {waterSource.m_SourceType}");
                m_Log.Debug($"m_WaterSystem.UseLegacyWaterSources: {m_WaterSystem.UseLegacyWaterSources} waterSource.m_LegacyWaterSource {waterSource.m_LegacyWaterSource} ");
                m_Log.Debug($"mode: {mode} waterSource.m_GameMode {waterSource.m_GameMode} ValidGameMode: {ValidGameMode(mode, waterSource)}");
                m_Log.Debug($"found prefab: {m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(WaterSourcePrefab), waterSource.name), out _)}");
                if (m_WaterSystem.UseLegacyWaterSources == waterSource.m_LegacyWaterSource &&
                    ValidGameMode(mode, waterSource) &&
                   !m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(WaterSourcePrefab), waterSource.name), out _) &&
                   (waterSource.m_SourceType != SourceType.DetentionBasin || WaterFeaturesMod.Instance.Settings.IncludeDetentionBasins) &&
                   (waterSource.m_SourceType != SourceType.RetentionBasin || WaterFeaturesMod.Instance.Settings.IncludeRetentionBasins))
                {
                    m_PrefabSystem.AddPrefab(waterSource);
                    m_PrefabList.Add(waterSource, GetSource(waterSource));
                }

                if (m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(WaterSourcePrefab), waterSource.name), out _) &&
                   (m_WaterSystem.UseLegacyWaterSources != waterSource.m_LegacyWaterSource ||
                    !ValidGameMode(mode, waterSource) ||
                   (waterSource.m_SourceType == SourceType.DetentionBasin && !WaterFeaturesMod.Instance.Settings.IncludeDetentionBasins) ||
                   (waterSource.m_SourceType == SourceType.RetentionBasin && !WaterFeaturesMod.Instance.Settings.IncludeRetentionBasins)))
                {
                    m_PrefabSystem.RemovePrefab(waterSource);
                    m_PrefabList.Remove(waterSource);
                }
            }

            if (mode == GameMode.Editor)
            {
                m_UISystem.SetPrefabList(m_PrefabList);
            }

            if (m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(UIAssetCategoryPrefab), TabName), out PrefabBase prefab1) &&
                prefab1 is not null &&
                m_PrefabSystem.TryGetEntity(prefab1, out Entity waterToolTabPrefabEntity1) &&
                EntityManager.TryGetBuffer(waterToolTabPrefabEntity1, false, out DynamicBuffer<UIGroupElement> uiGroupBuffer1) &&
                uiGroupBuffer1.Length == 0)
            {
                foreach (WaterSourcePrefabData source in m_SourcePrefabDataList)
                {
                    if (m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(WaterSourcePrefab), $"{PrefabPrefix}{source.m_SourceType}"), out var waterSourcePrefab) &&
                        waterSourcePrefab is WaterSourcePrefab)
                    {
                        if (!m_PrefabSystem.TryGetEntity(waterSourcePrefab, out Entity waterSourcePrefabEntity))
                        {
                            m_Log.Warn($"{nameof(AddPrefabsSystem)}.{nameof(OnGameLoadingComplete)} Couldn't find waterSourcePrefabEntity in waterSourcePrefab for {source.m_SourceType}.");
                            continue;
                        }

                        if (!EntityManager.TryGetComponent(waterSourcePrefabEntity, out UIObjectData uIObjectData))
                        {
                            m_Log.Warn($"{nameof(AddPrefabsSystem)}.{nameof(OnGameLoadingComplete)} Couldn't find UIObjectData in waterSourcePrefabEntity for {source.m_SourceType}.");
                            continue;
                        }

                        // If all the requried components have been found and the data wasn't assigned automatically. Then this adds the data to get the water source prefabs into the water tool tab.
                        if (uIObjectData.m_Group == Entity.Null )
                        {
                            // This is unfortunately expected that the Entity will be null and that this information needs to be added.
                            m_Log.Debug($"{nameof(AddPrefabsSystem)}.{nameof(OnGameLoadingComplete)} uIObjectData.m_Group = Entity.Null so set to {waterToolTabPrefabEntity1.Index}.{waterToolTabPrefabEntity1.Version}");
                            uIObjectData.m_Group = waterToolTabPrefabEntity1;
                            uIObjectData.m_Priority = source.m_Priority;
                            EntityManager.SetComponentData(waterSourcePrefabEntity, uIObjectData);
                            UIGroupElement groupElement = new UIGroupElement()
                            {
                                m_Prefab = waterSourcePrefabEntity,
                            };
                            uiGroupBuffer1.Add(groupElement);
                        }
                    }
                }
            }
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
            if (m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(UIAssetCategoryPrefab), name), out var prefab) || prefab is UIAssetCategoryPrefab)
            {
                return (UIAssetCategoryPrefab)prefab;
            }

            if (!m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(UIAssetMenuPrefab), menuName), out var prefabBase)
            || prefabBase is not UIAssetMenuPrefab menu)
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
