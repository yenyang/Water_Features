// <copyright file="WaterFeaturesMod.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features
{
    using System;
    using System.IO;
    using System.Linq;
    using Colossal.IO.AssetDatabase;
    using Colossal.Localization;
    using Colossal.Logging;
    using Game;
    using Game.Modding;
    using Game.SceneFlow;
    using Game.UI;
    using Water_Features.Settings;
    using Water_Features.Systems;
    using Water_Features.Tools;
    using Water_Features.Utils;

    /// <summary>
    ///  A mod that adds Water Tool, Seasonal Streams, and Experimetnal Waves and Tides.
    /// </summary>
    public class WaterFeaturesMod : IMod
    {
        /// <summary>
        /// Gets the install folder for the mod.
        /// </summary>
        private static string m_modInstallFolder;

        /// <summary>
        /// Gets the static reference to the mod instance.
        /// </summary>
        public static WaterFeaturesMod Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the Install Folder for the mod as a string.
        /// </summary>
        public static string ModInstallFolder
        {
            get
            {
                if (m_modInstallFolder is null)
                {
                    var thisFullName = Instance.GetType().Assembly.FullName;
                    ExecutableAsset thisInfo = AssetDatabase.global.GetAsset(SearchFilter<ExecutableAsset>.ByCondition(x => x.definition?.FullName == thisFullName)) ?? throw new Exception("This mod info was not found!!!!");
                    m_modInstallFolder = Path.GetDirectoryName(thisInfo.GetMeta().path);
                }

                return m_modInstallFolder;
            }
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
#if DEBUG
            Log.effectivenessLevel = Level.Debug;
#elif VERBOSE
            Log.effectivenessLevel = Level.Verbose;
#else
            Log.effectivenessLevel = Level.Info;
#endif
            Log.Info($"{nameof(WaterFeaturesMod)}.{nameof(OnLoad)} Initializing settings");
            Settings = new (this);
            Log.Info($"{nameof(WaterFeaturesMod)}.{nameof(OnLoad)} Loading localization");
            LoadLocales();
            Log.Info($"{nameof(WaterFeaturesMod)}.{nameof(OnLoad)} Registering settings");
            Settings.RegisterInOptionsUI();
            Log.Info($"{nameof(WaterFeaturesMod)}.{nameof(OnLoad)} Loading settings");
            AssetDatabase.global.LoadSettings("Mods_Yenyang_Water_Features", Settings, new WaterFeaturesSettings(this));
            Settings.Contra = false;
            Log.Info("Handling create world");
            Log.Info("ModInstallFolder = " + ModInstallFolder);
            Log.Info($"{nameof(WaterFeaturesMod)}.{nameof(OnLoad)} Injecting systems.");
            updateSystem.UpdateAt<CustomWaterToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAfter<WaterTooltipSystem>(SystemUpdatePhase.UITooltip);
            updateSystem.UpdateBefore<FindWaterSourcesSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<AutofillingLakesSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<DetentionBasinSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<RetentionBasinSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<ChangeWaterSystemValues>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<TidesAndWavesSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<BeforeSerializeSystem>(SystemUpdatePhase.Serialize);
            updateSystem.UpdateAfter<TidesAndWavesSystem>(SystemUpdatePhase.Serialize);
            updateSystem.UpdateAt<SeasonalStreamsSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<DisableSeasonalStreamSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<DisableWavesAndTidesSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<SeasonalStreamsSystem>(SystemUpdatePhase.Serialize);
            updateSystem.UpdateAfter<AutofillingLakesSystem>(SystemUpdatePhase.Serialize);
            updateSystem.UpdateAfter<DetentionBasinSystem>(SystemUpdatePhase.Serialize);
            updateSystem.UpdateAfter<RetentionBasinSystem>(SystemUpdatePhase.Serialize);
            updateSystem.UpdateAt<AddPrefabsSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateAt<WaterToolUISystem>(SystemUpdatePhase.UIUpdate);
            Log.Info($"{nameof(WaterFeaturesMod)}.{nameof(OnLoad)} Completed.");
        }

        /// <inheritdoc/>
        public void OnDispose()
        {
            Log.Info($"[{nameof(WaterFeaturesMod)}] {nameof(OnDispose)}");
            if (Settings != null)
            {
                Settings.UnregisterInOptionsUI();
                Settings = null;
            }
        }

        private void LoadLocales()
        {
            LocaleEN defaultLocale = new LocaleEN(Settings);

            // defaultLocale.ExportLocalizationCSV(Path.Combine(ModInstallFolder, "l10n"), GameManager.instance.localizationManager.GetSupportedLocales());
            var file = Path.Combine(ModInstallFolder, "l10n", $"l10n.csv");
            if (File.Exists(file))
            {
                var fileLines = File.ReadAllLines(file).Select(x => x.Split('\t'));
                var enColumn = Array.IndexOf(fileLines.First(), "en-US");
                var enMemoryFile = new MemorySource(fileLines.Skip(1).ToDictionary(x => x[0], x => x.ElementAtOrDefault(enColumn)));
                foreach (var lang in GameManager.instance.localizationManager.GetSupportedLocales())
                {
                    try
                    {
                        GameManager.instance.localizationManager.AddSource(lang, enMemoryFile);
                        if (lang != "en-US")
                        {
                            var valueColumn = Array.IndexOf(fileLines.First(), lang);
                            if (valueColumn > 0)
                            {
                                var i18nFile = new MemorySource(fileLines.Skip(1).ToDictionary(x => x[0], x => x.ElementAtOrDefault(valueColumn)));
                                GameManager.instance.localizationManager.AddSource(lang, i18nFile);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warn($"{nameof(WaterFeaturesMod)}.{nameof(LoadLocales)} Encountered exception {ex} while trying to localize {lang}.");
                    }
                }
            }
            else
            {
                Log.Warn($"{nameof(WaterFeaturesMod)}.{nameof(LoadLocales)} couldn't find localization file and loaded default for every language.");
                foreach (var lang in GameManager.instance.localizationManager.GetSupportedLocales())
                {
                    GameManager.instance.localizationManager.AddSource(lang, defaultLocale);
                }
            }
        }
    }
}