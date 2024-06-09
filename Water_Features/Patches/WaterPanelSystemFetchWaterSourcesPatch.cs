﻿// <copyright file="UniqueAssetTrackingSystemOnCreatePatch.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Patches
{
    using Colossal.Entities;
    using Game.UI.Editor;
    using HarmonyLib;
    using Unity.Collections;
    using Unity.Entities;
    using Water_Features.Components;
    using Water_Features.Systems;

    /// <summary>
    /// Patches WaterPanelSystem FetchWaterSources as water sources need to be reset before data is recorded.
    /// </summary>
    [HarmonyPatch(typeof(WaterPanelSystem), "FetchWaterSources")]
    public class WaterPanelSystemFetchWaterSourcesPatch
    {
        /// <summary>
        /// Patches WaterPanelSystem FetchWaterSources as water sources need to be reset before data is recorded.
        /// </summary>
        public static void Prefix()
        {
            SeasonalStreamsSystem seasonalStreamsSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<SeasonalStreamsSystem>();
            TidesAndWavesSystem wavesAndTidesSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TidesAndWavesSystem>();

            if (!seasonalStreamsSystem.SeasonalStreamsSourcesQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> seasonalStreamsEntities = seasonalStreamsSystem.SeasonalStreamsSourcesQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in seasonalStreamsEntities)
                {
                    if (!seasonalStreamsSystem.EntityManager.TryGetComponent(entity, out SeasonalStreamsData seasonalStreamsData) || !seasonalStreamsSystem.EntityManager.TryGetComponent(entity, out Game.Simulation.WaterSourceData waterSourceData))
                    {
                        continue;
                    }

                    waterSourceData.m_Amount = seasonalStreamsData.m_OriginalAmount;
                    seasonalStreamsSystem.EntityManager.SetComponentData(entity, waterSourceData);
                    seasonalStreamsSystem.EntityManager.RemoveComponent<SeasonalStreamsData>(entity);
                }
            }

            if (!wavesAndTidesSystem.WavesAndTidesDataQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> wavesAndTidesEntities = wavesAndTidesSystem.WavesAndTidesDataQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in wavesAndTidesEntities)
                {
                    if (!wavesAndTidesSystem.EntityManager.TryGetComponent(entity, out TidesAndWavesData tidesAndWavesData) || !wavesAndTidesSystem.EntityManager.TryGetComponent(entity, out Game.Simulation.WaterSourceData waterSourceData))
                    {
                        continue;
                    }

                    waterSourceData.m_Amount = tidesAndWavesData.m_OriginalAmount;
                    wavesAndTidesSystem.EntityManager.SetComponentData(entity, waterSourceData);
                    wavesAndTidesSystem.EntityManager.RemoveComponent<TidesAndWavesData>(entity);
                }
            }
        }

        /// <summary>
        /// Ensures find water sources system runs after this method.
        /// </summary>
        public static void Postfix()
        {
            FindWaterSourcesSystem findWaterSourcesSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<FindWaterSourcesSystem>();
            findWaterSourcesSystem.Enabled = true;
        }
    }
}
