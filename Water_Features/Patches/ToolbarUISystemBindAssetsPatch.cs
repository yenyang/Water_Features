// <copyright file="ToolbarUISystemBindAssetsPatch.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Patches
{
    using Colossal.UI.Binding;
    using Game.Prefabs;
    using Game.UI.InGame;
    using HarmonyLib;
    using Unity.Entities;
    using Water_Features.Systems;

    /// <summary>
    /// Toolbar UI System BindAssets to ensure stuff is correct.
    /// </summary>
    [HarmonyPatch(typeof(ToolbarUISystem), "BindAssets")]
    public class ToolbarUISystemBindAssetsPatch
    {
        /// <summary>
        /// Patches ToolbarUISystem Apply to ensure Water source prefabs are correct.
        /// </summary>
        /// <param name="writer">Not used.</param>
        /// <param name="assetCategory">Prefab Entity for Asset Menu.</param>
        public static void Prefix(IJsonWriter writer, Entity assetCategory)
        {
            WaterFeaturesMod.Instance.Log.Debug($"{nameof(ToolbarUISystemBindAssetsPatch)}");
            PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
            if (prefabSystem.TryGetPrefab(new PrefabID(nameof(UIAssetCategoryPrefab), AddPrefabsSystem.TabName), out var prefab) &&
                prefab is UIAssetCategoryPrefab &&
                prefabSystem.TryGetEntity(prefab, out Entity prefabEntity) &&
                assetCategory == prefabEntity)
            {
                AddPrefabsSystem addPrefabsSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AddPrefabsSystem>();
                addPrefabsSystem.ReviewPrefabUIGroupElements();
            }

        }
    }
}
