// <copyright file="WaterFeaturesMod.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define VERBOSE
namespace Water_Features
{
    using System;
    using System.IO;
    using Colossal.IO.AssetDatabase;
    using Colossal.Logging;
    using Game;
    using Game.Modding;
    using HarmonyLib;
    using Water_Features.Settings;
    using Water_Features.Systems;
    using Water_Features.Tools;

    /// <summary>
    ///  A mod that adds Water Tool, Seasonal Streams, and Experimetnal Waves and Tides.
    /// </summary>
    public class WaterFeaturesMod : IMod
    {
        private Harmony m_Harmony;

        /// <summary>
        /// Gets the static reference to the mod instance.
        /// </summary>
        public static WaterFeaturesMod Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the log for the mod.
        /// </summary>
        internal ILog Log { get; private set; }

        /// <summary>
        /// Gets or sets the Settings for the mod.
        /// </summary>
        internal WaterFeaturesSettings Settings { get; set; }

        /// <inheritdoc/>
        public void OnLoad(UpdateSystem updateSystem)
        {
            Instance = this;
            Log = LogManager.GetLogger("Mods_Yenyang_Water_Features").SetShowsErrorsInUI(false);
            Log.Info($"[{nameof(WaterFeaturesMod)}] {nameof(OnLoad)}");
#if VERBOSE
            Log.effectivenessLevel = Level.Verbose;
#elif DEBUG
            Log.effectivenessLevel = Level.Debug;
#else
            Log.effectivenessLevel = Level.Info;
#endif
            Log.Info($"{nameof(WaterFeaturesMod)}.{nameof(OnLoad)} Initializing settings");
            Settings = new (this);
            Log.Info($"{nameof(WaterFeaturesMod)}.{nameof(OnLoad)} Loading localization");
            Localization.Localization.LoadTranslations(Settings, Log);
            Log.Info($"{nameof(WaterFeaturesMod)}.{nameof(OnLoad)} Registering settings");
            Settings.RegisterInOptionsUI();
            Log.Info($"{nameof(WaterFeaturesMod)}.{nameof(OnLoad)} Loading settings");
            AssetDatabase.global.LoadSettings("Mods_Yenyang_Water_Features", Settings, new WaterFeaturesSettings(this));
            Log.Info("Handling create world");
            Log.Info($"{nameof(WaterFeaturesMod)}.{nameof(OnLoad)} Injecting Harmony Patches.");
            m_Harmony = new Harmony("Mods_Yenyang_Water_Features");
            m_Harmony.PatchAll();
            Log.Info($"{nameof(WaterFeaturesMod)}.{nameof(OnLoad)} Injecting systems.");
            updateSystem.UpdateAt<AddPrefabsSystem>(SystemUpdatePhase.PrefabUpdate);

            updateSystem.UpdateAt<WaterToolUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<CustomWaterToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAfter<WaterTooltipSystem>(SystemUpdatePhase.UITooltip);

            updateSystem.UpdateBefore<FindWaterSourcesSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<AutofillingLakesSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<DetentionBasinSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<RetentionBasinSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<ChangeWaterSystemValues>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<TidesAndWavesSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<SeasonalStreamsSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<DisableSeasonalStreamSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<DisableWavesAndTidesSystem>(SystemUpdatePhase.GameSimulation);

            updateSystem.UpdateBefore<BeforeSerializeSystem>(SystemUpdatePhase.Serialize);
            updateSystem.UpdateAfter<TidesAndWavesSystem>(SystemUpdatePhase.Serialize);
            updateSystem.UpdateAfter<SeasonalStreamsSystem>(SystemUpdatePhase.Serialize);
            updateSystem.UpdateAfter<AutofillingLakesSystem>(SystemUpdatePhase.Serialize);
            updateSystem.UpdateAfter<DetentionBasinSystem>(SystemUpdatePhase.Serialize);
            updateSystem.UpdateAfter<RetentionBasinSystem>(SystemUpdatePhase.Serialize);

            updateSystem.UpdateBefore<ChangeWaterSystemValues>(SystemUpdatePhase.EditorSimulation);
            updateSystem.UpdateBefore<FindWaterSourcesSystem>(SystemUpdatePhase.EditorSimulation);
            updateSystem.UpdateAt<AutofillingLakesSystem>(SystemUpdatePhase.EditorSimulation);
            updateSystem.UpdateAt<DetentionBasinSystem>(SystemUpdatePhase.EditorSimulation);
            updateSystem.UpdateAt<RetentionBasinSystem>(SystemUpdatePhase.EditorSimulation);
            updateSystem.UpdateAt<TidesAndWavesSystem>(SystemUpdatePhase.EditorSimulation);
            updateSystem.UpdateAt<SeasonalStreamsSystem>(SystemUpdatePhase.EditorSimulation);
            updateSystem.UpdateAt<DisableSeasonalStreamSystem>(SystemUpdatePhase.EditorSimulation);
            updateSystem.UpdateAt<DisableWavesAndTidesSystem>(SystemUpdatePhase.EditorSimulation);

            Log.Info($"{nameof(WaterFeaturesMod)}.{nameof(OnLoad)} Completed.");
        }

        /// <inheritdoc/>
        public void OnDispose()
        {
            Log.Info($"[{nameof(WaterFeaturesMod)}] {nameof(OnDispose)}");
            m_Harmony.UnpatchAll();
            if (Settings != null)
            {
                Settings.UnregisterInOptionsUI();
                Settings = null;
            }
        }
    }
}