// <copyright file="WaterSystemUpdateToNewWaterSystemPatch.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Patches
{
    using Game.Simulation;
    using HarmonyLib;
    using Unity.Entities;
    using Water_Features.Systems;

    /// <summary>
    /// Patches WaterPanelSystem FetchWaterSources as water sources need to be reset before data is recorded.
    /// </summary>
    [HarmonyPatch(typeof(WaterSystem), "UpgradeToNewWaterSystem")]
    public class WaterSystemUpdateToNewWaterSystemPatch
    {
        public static void PostFix()
        {
            AddPrefabsSystem addPrefabsSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AddPrefabsSystem>();
            addPrefabsSystem.ReviewPrefabs();
        }
    }
}
