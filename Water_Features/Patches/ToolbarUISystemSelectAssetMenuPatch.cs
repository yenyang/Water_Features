// <copyright file="ToolbarUISystemApplyPatch.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Patches
{
    using Colossal.Entities;
    using Game;
    using Game.Prefabs;
    using Game.Simulation;
    using Game.UI.Editor;
    using Game.UI.InGame;
    using HarmonyLib;
    using Unity.Collections;
    using Unity.Entities;
    using Water_Features.Components;
    using Water_Features.Systems;

    /// <summary>
    /// Patches WaterPanelSystem FetchWaterSources as water sources need to be reset before data is recorded.
    /// </summary>
    [HarmonyPatch(typeof(ToolbarUISystem), "SelectAssetMenu")]
    public class ToolbarUISystemSelectAssetMenuPatch
    {
        /// <summary>
        /// Patches ToolbarUISystem Apply to ensure Water source prefabs are correct.
        /// </summary>
        /// <param name="assetMenu">Prefab Entity for Asset Menu.</param>
        public static void Prefix(Entity assetMenu)
        {
            WaterFeaturesMod.Instance.Log.Debug($"{nameof(ToolbarUISystemSelectAssetMenuPatch)}");
            PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
            if (!prefabSystem.TryGetPrefab(new PrefabID(nameof(UIAssetMenuPrefab), "Landscaping"), out var prefabBase) ||
                prefabBase is not UIAssetMenuPrefab menu ||
               !prefabSystem.TryGetEntity(prefabBase, out Entity prefabEntity) ||
                prefabEntity != assetMenu)
            {
                return;
            }

            AddPrefabsSystem addPrefabsSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AddPrefabsSystem>();
            addPrefabsSystem.ReviewPrefabs();
        }
    }
}
