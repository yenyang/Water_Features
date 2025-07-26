// <copyright file="WaterFeaturesSettings.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Settings
{
    using Colossal.IO.AssetDatabase;
    using Game;
    using Game.Modding;
    using Game.Settings;
    using Game.Simulation;
    using Game.Tools;
    using Game.UI;
    using Unity.Entities;
    using Water_Features.Systems;

    /// <summary>
    /// The mod settings for the Water Features Mod.
    /// </summary>
    [FileLocation("Mods_Yenyang_Water_Features")]
    [SettingsUITabOrder(WaterToolGroup, SeasonalStreams, WavesAndTides)]
    [SettingsUISection(WaterToolGroup, SeasonalStreams, WavesAndTides)]
    [SettingsUIShowGroupName(General, Editor, SaveGame, Stable, Experimental)]
    [SettingsUIGroupOrder(Warnings, General, Editor, SaveGame, Stable, Experimental, Reset)]
    public class WaterFeaturesSettings : ModSetting
    {
        /// <summary>
        /// This is for settings for seasonal streams.
        /// </summary>
        public const string SeasonalStreams = "Seasonal Streams";

        /// <summary>
        /// This is for options related to water tool.
        /// </summary>
        public const string WaterToolGroup = "Water Tool";

        /// <summary>
        /// This is for options related to waves and tides.
        /// </summary>
        public const string WavesAndTides = "Waves and Tides";

        /// <summary>
        /// This is for experimental settings.
        /// </summary>
        public const string Experimental = "Experimental";

        /// <summary>
        /// This is for stable settings for.
        /// </summary>
        public const string Stable = "Stable";

        /// <summary>
        /// This is for general settings.
        /// </summary>
        public const string General = "General";

        /// <summary>
        /// This is for save game settings.
        /// </summary>
        public const string SaveGame = "SaveGame";

        /// <summary>
        /// This is for settings for Editor.
        /// </summary>
        public const string Editor = "Editor";

        /// <summary>
        /// This is for reset settings button group.
        /// </summary>
        public const string Reset = "Reset";

        /// <summary>
        /// This is for about section of settings.
        /// </summary>
        public const string About = "About";

        /// <summary>
        /// This is for about section of settings.
        /// </summary>
        public const string Warnings = "Warnings";

        /// <summary>
        /// Initializes a new instance of the <see cref="WaterFeaturesSettings"/> class.
        /// </summary>
        /// <param name="mod">Water Features mod.</param>
        public WaterFeaturesSettings(IMod mod)
            : base(mod)
        {
            SetDefaults();
        }

        /// <summary>
        /// An enum with the types of tides that can be simulated.
        /// </summary>
        public enum TideClassificationYYTAW
        {
            /// <summary>
            /// Diurnal tides have one high and one low tide per day.
            /// </summary>
            Diurnal = 12,

            /// <summary>
            /// Semidirurnal tides have two high and two low tides per day.
            /// </summary>
            Semidiurnal = 24,
        }

        /// <summary>
        /// Gets or sets a value indicating whether to Try Smaller Radii.
        /// </summary>
        [SettingsUISection(WaterToolGroup, General)]
        public bool TrySmallerRadii { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to Include Detention Basins.
        /// </summary>
        [SettingsUISection(WaterToolGroup, General)]
        public bool IncludeDetentionBasins { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to Include Retention Basins.
        /// </summary>
        [SettingsUISection(WaterToolGroup, General)]
        public bool IncludeRetentionBasins { get; set; }

        /// <summary>
        /// Gets or sets the evaporatin rate for the whole map.
        /// </summary>
        [SettingsUISection(WaterToolGroup, SaveGame)]
        [SettingsUISlider(min = 0.01f, max = 1f, step = 0.01f, unit = Unit.kFloatTwoFractions, scalarMultiplier = 1000f)]
        [SettingsUIDisableByCondition(typeof(WaterFeaturesSettings), nameof(DisableWaterToolSetting))]
        public float EvaporationRate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether water causes damage.
        /// </summary>
        [SettingsUISection(WaterToolGroup, SaveGame)]
        [SettingsUIDisableByCondition(typeof(WaterFeaturesSettings), nameof(DisableWaterToolSetting))]
        [SettingsUISetter(typeof(WaterFeaturesSettings), nameof(WaterCausesDamageToggled))]
        public bool WaterCausesDamage { get; set; }

        /// <summary>
        /// Sets a value indicating whether the toggle for applying a new evaporation rate is on.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(WaterToolGroup, SaveGame)]
        [SettingsUIDisableByCondition(typeof(WaterFeaturesSettings), nameof(IsGameOrEditor), invert: true)]
        public bool WaterCleanUpCycleButton
        {
            set
            {
                ChangeWaterSystemValues changeWaterSystemValues = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ChangeWaterSystemValues>();
                changeWaterSystemValues.ApplyNewEvaporationRate = true;
            }
        }

        /// <summary>
        /// Sets a value indicating whether: a button for Resetting the settings for the general tab.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(WaterToolGroup, Reset)]
        public bool ResetWaterToolGroupButton
        {
            set
            {
                ResetWaterToolSettings();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to have Seasonal Streams.
        /// </summary>
        [SettingsUISection(SeasonalStreams, SaveGame)]
        [SettingsUISetter(typeof(WaterFeaturesSettings), nameof(SeasonalStreamsToggled))]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsGameOrEditor), invert: true)]
        public bool EnableSeasonalStreams { get; set; }

        /// <summary>
        /// Gets a value indicating that seasonal streams settings are only available in game.
        /// </summary>
        [SettingsUIMultilineText]
        [SettingsUISection(SeasonalStreams, Warnings)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsGameOrEditor))]
        public string SeasonalStreamsSettingsAvailableInGame { get; }

        /// <summary>
        /// Gets or sets a value indicating whether to simulate snow melt with streams.
        /// </summary>
        [SettingsUISection(SeasonalStreams, SaveGame)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsSeasonalStreamsDisabled))]
        public bool SimulateSnowMelt { get; set; }

        /// <summary>
        /// Gets or sets a value with a slider indicating the multiplier for water always emitted from a stream.
        /// </summary>
        [SettingsUISlider(min = 0f, max = 100f, step = 1f, unit = "percentageSingleFraction", scalarMultiplier = 100f)]
        [SettingsUISection(SeasonalStreams, SaveGame)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsSeasonalStreamsDisabled))]
        public float ConstantFlowRate { get; set; }

        /// <summary>
        /// Gets or sets a value with a slider indicating the multiplier for water seaonally emitted from a stream.
        /// </summary>
        [SettingsUISlider(min = 0f, max = 100f, step = 5f, unit = "percentageSingleFraction", scalarMultiplier = 100f)]
        [SettingsUISection(SeasonalStreams, SaveGame)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsSeasonalStreamsDisabled))]
        public float StreamSeasonality { get; set; }

        /// <summary>
        /// Gets or sets a value with a slider indicating the multiplier for water emitted from a stream due to rain.
        /// </summary>
        [SettingsUISlider(min = 0f, max = 100f, step = 5f, unit = "percentageSingleFraction", scalarMultiplier = 100f)]
        [SettingsUISection(SeasonalStreams, SaveGame)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsSeasonalStreamsDisabled))]
        public float StreamStormwaterEffects { get; set; }

        /// <summary>
        /// Gets or sets a value with a slider indicating the minimum multiplier to apply to streams.
        /// </summary>
        [SettingsUISection(SeasonalStreams, SaveGame)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsSeasonalStreamsDisabled))]
        [SettingsUISlider(min = 0f, max = 1f, step = 0.1f, unit = "floatSingleFraction")]
        public float MinimumMultiplier { get; set; }

        /// <summary>
        /// Gets or sets a value with a slider indicating the maximum multiplier to apply to streams.
        /// </summary>
        [SettingsUISlider(min = 1f, max = 10f, step = 0.1f, unit = "floatSingleFraction")]
        [SettingsUISection(SeasonalStreams, SaveGame)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsSeasonalStreamsDisabled))]
        public float MaximumMultiplier { get; set; }

        /// <summary>
        /// Sets a value indicating whether: a button for Resetting the settings for the general tab.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(SeasonalStreams, Reset)]
        [SettingsUIDisableByCondition(typeof(WaterFeaturesSettings), nameof(IsGameOrEditor), invert: true)]
        public bool ResetSeasonalStreamsSettingsButton
        {
            set
            {
                ResetSeasonalStreamsSettings();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to have Waves and Tides.
        /// </summary>
        [SettingsUISection(WavesAndTides, SaveGame)]
        [SettingsUISetter(typeof(WaterFeaturesSettings), nameof(WavesAndTidesToggled))]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsGameOrEditor), invert: true)]
        public bool EnableWavesAndTides { get; set; }

        /// <summary>
        /// Gets a value indicating that waves and tides settings are only available in game.
        /// </summary>
        [SettingsUIMultilineText]
        [SettingsUISection(WavesAndTides, Warnings)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsGameOrEditor))]
        public string WavesAndTidesSettingsAvailableInGame { get; }

        /// <summary>
        /// Gets or sets a value with a slider indicating the height of waves generated.
        /// </summary>
        [SettingsUISection(WavesAndTides, SaveGame)]
        [SettingsUISlider(min = 0f, max = 20f, step = 0.5f, unit = "floatSingleFraction")]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsWavesAndTidesDisabled))]
        [SettingsUISetter(typeof(WaterFeaturesSettings), nameof(ResetDummyWaterSource))]
        public float WaveHeight { get; set; }

        /// <summary>
        /// Gets or sets a value with a slider indicating the frequency of waves generated.
        /// </summary>
        [SettingsUISection(WavesAndTides, SaveGame)]
        [SettingsUISlider(min = 10f, max = 250f, step = 10f)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsWavesAndTidesDisabled))]
        public float WaveFrequency { get; set; }

        /// <summary>
        /// Gets or sets a value with a slider indicating the height of tides generated.
        /// </summary>
        [SettingsUISection(WavesAndTides, SaveGame)]
        [SettingsUISlider(min = 0f, max = 15f, step = 0.5f, unit = "floatSingleFraction")]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsWavesAndTidesDisabled))]
        [SettingsUISetter(typeof(WaterFeaturesSettings), nameof(ResetDummyWaterSource))]
        public float TideHeight { get; set; }

        /// <summary>
        /// Gets or sets an enum value indicating the tide classification.
        /// </summary>
        [SettingsUISection(WavesAndTides, SaveGame)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsWavesAndTidesDisabled))]
        public TideClassificationYYTAW TideClassification { get; set; }

        /// <summary>
        /// Gets or sets a value with a slider indicating the damping factor of the water system.
        /// </summary>
        [SettingsUISection(WavesAndTides, SaveGame)]
        [SettingsUISlider(min = 9950f, max = 9999f, step = 1f, unit = "floatSingleFraction", scalarMultiplier = 10000f)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsWavesAndTidesDisabled))]
        public float Damping { get; set; }

        /// <summary>
        /// Sets a value indicating whether: a button for Resetting the settings for the Waves and tides.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(WavesAndTides, Reset)]
        [SettingsUIDisableByCondition(typeof(WaterFeaturesSettings), nameof(IsGameOrEditor), invert: true)]
        public bool ResetWavesAndTidesSettingsButton
        {
            set
            {
                ResetWavesAndTidesSettings();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to force water simulation.
        /// </summary>
        [SettingsUISection(WaterToolGroup, SaveGame)]
        [SettingsUIDisableByCondition(typeof(WaterFeaturesSettings), nameof(DisableWaterToolSetting))]
        public bool ForceWaterSimulationSpeed { get; set; }

        /// <summary>
        /// Gets or sets a value with a slider indicating the fluidness factor of the water system.
        /// </summary>
        [SettingsUISection(WaterToolGroup, Experimental)]
        [SettingsUISlider(min = 0.01f, max = 1.0f, step = 0.01f, unit = Unit.kFloatTwoFractions)]
        [SettingsUIDisableByCondition(typeof(WaterFeaturesSettings), nameof(DisableWaterToolSetting))]
        public float Fluidness { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Evaporation and Fluidness can be altered in Editor.
        /// </summary>
        [SettingsUISection(WaterToolGroup, Editor)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsEditor), invert: true)]
        public bool WaterToolSettingsAffectEditorSimulation { get; set; }

        /// <summary>
        /// Gets a value indicating the version.
        /// </summary>
        [SettingsUISection(WaterToolGroup, About)]
        public string Version => WaterFeaturesMod.Instance.Version;

        /// <summary>
        /// Resets only the water tool settings.
        /// </summary>
        public void ResetWaterToolSettings()
        {
            EvaporationRate = 0.0001f;
            TrySmallerRadii = false;
            IncludeDetentionBasins = false;
            IncludeRetentionBasins = false;
            Fluidness = 0.1f;
            WaterToolSettingsAffectEditorSimulation = false;
            ForceWaterSimulationSpeed = false;
            WaterCausesDamage = true;
        }

        /// <summary>
        /// Resets only the Seasonal streams settings.
        /// </summary>
        public void ResetSeasonalStreamsSettings()
        {
            ConstantFlowRate = 0f;
            StreamSeasonality = 0.5f;
            StreamStormwaterEffects = 0.75f;
            MinimumMultiplier = 0f;
            MaximumMultiplier = 1.0f;
            SimulateSnowMelt = true;
            EnableSeasonalStreams = false;
            SeasonalStreamsToggled(false);
        }

        /// <summary>
        /// Resets only the waves and tides settings tab.
        /// </summary>
        public void ResetWavesAndTidesSettings()
        {
            WaveHeight = 15f;
            TideHeight = 0f;
            WaveFrequency = 200f;
            TideClassification = TideClassificationYYTAW.Semidiurnal;
            Damping = 0.9999f;
            EnableWavesAndTides = false;
            WavesAndTidesToggled(false);
        }

        /// <summary>
        /// Checks if seasonal streams feature is off or on.
        /// </summary>
        /// <returns>Opposite of Enable Seasonal Streams.</returns>
        public bool IsSeasonalStreamsDisabled() => !EnableSeasonalStreams || !IsGameOrEditor();

        /// <summary>
        /// Checks if waves and tides feature is off or on.
        /// </summary>
        /// <returns>Opposite of Enable Waves and Tides.</returns>
        public bool IsWavesAndTidesDisabled() => !EnableWavesAndTides || !IsGameOrEditor();

        /// <inheritdoc/>
        public override void SetDefaults()
        {
            TrySmallerRadii = false;
            EvaporationRate = 0.0001f;
            IncludeDetentionBasins = false;
            IncludeRetentionBasins = false;
            ConstantFlowRate = 0f;
            StreamSeasonality = 0.5f;
            StreamStormwaterEffects = 0.75f;
            MinimumMultiplier = 0f;
            MaximumMultiplier = 1.0f;
            SimulateSnowMelt = true;
            WaveHeight = 15f;
            TideHeight = 0f;
            WaveFrequency = 200f;
            TideClassification = TideClassificationYYTAW.Semidiurnal;
            Damping = 0.9999f;
            EnableSeasonalStreams = false;
            EnableWavesAndTides = false;
            Fluidness = 0.1f;
            ForceWaterSimulationSpeed = false;
            WaterToolSettingsAffectEditorSimulation = false;
            WaterCausesDamage = true;
        }

        /// <summary>
        /// Resets the dummy water source from waves and tides.
        /// </summary>
        /// <param name="value">not used.</param>
        public void ResetDummyWaterSource(float value)
        {
            TidesAndWavesSystem tidesAndWavesSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TidesAndWavesSystem>();
            tidesAndWavesSystem.ResetDummySeaWaterSource();
        }

        /// <summary>
        /// Handles toggling seasonal streams either in game or editor.
        /// </summary>
        /// <param name="value">true if enabled, false if not.</param>
        public void SeasonalStreamsToggled(bool value)
        {
            WaterFeaturesMod.Instance.Log.Info($"{nameof(WaterFeaturesSettings)}.{nameof(SeasonalStreamsToggled)}");
            SeasonalStreamsSystem seasonalStreamsSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<SeasonalStreamsSystem>();
            ToolSystem toolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            if (value && toolSystem.actionMode.IsGameOrEditor())
            {
                WaterFeaturesMod.Instance.Log.Info($"{nameof(WaterFeaturesSettings)}.{nameof(SeasonalStreamsToggled)} Enabled");
                seasonalStreamsSystem.Enabled = true;
                DisableSeasonalStreamSystem disableSeasonalStreamSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<DisableSeasonalStreamSystem>();
                FindWaterSourcesSystem findWaterSourcesSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<FindWaterSourcesSystem>();
                findWaterSourcesSystem.Enabled = true;
                disableSeasonalStreamSystem.Enabled = false;
            }
            else
            {
                WaterFeaturesMod.Instance.Log.Info($"{nameof(WaterFeaturesSettings)}.{nameof(SeasonalStreamsToggled)} Disabled");
                seasonalStreamsSystem.Enabled = false;
                DisableSeasonalStreamSystem disableSeasonalStreamSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<DisableSeasonalStreamSystem>();
                disableSeasonalStreamSystem.Enabled = true;
            }
        }

        /// <summary>
        /// Handles toggling waves and tides either in game or editor.
        /// </summary>
        /// <param name="value">true if enabled, false if not.</param>
        public void WavesAndTidesToggled(bool value)
        {
            WaterFeaturesMod.Instance.Log.Debug($"{nameof(WaterFeaturesSettings)}.{nameof(WavesAndTidesToggled)}");
            TidesAndWavesSystem tidesAndWavesSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TidesAndWavesSystem>();
            ToolSystem toolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            if (value && toolSystem.actionMode.IsGameOrEditor())
            {
                WaterFeaturesMod.Instance.Log.Info($"{nameof(WaterFeaturesSettings)}.{nameof(WavesAndTidesToggled)} Enabled");
                tidesAndWavesSystem.Enabled = true;
                DisableWavesAndTidesSystem disableWavesAndTidesSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<DisableWavesAndTidesSystem>();
                FindWaterSourcesSystem findWaterSourcesSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<FindWaterSourcesSystem>();
                findWaterSourcesSystem.Enabled = true;
                disableWavesAndTidesSystem.Enabled = false;
            }
            else
            {
                WaterFeaturesMod.Instance.Log.Info($"{nameof(WaterFeaturesSettings)}.{nameof(WavesAndTidesToggled)} Disabled");
                tidesAndWavesSystem.Enabled = false;
                DisableWavesAndTidesSystem disableWavesAndTidesSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<DisableWavesAndTidesSystem>();
                disableWavesAndTidesSystem.Enabled = true;
            }
        }

        /// <summary>
        /// Checks whether it is game or editor.
        /// </summary>
        /// <returns>True if in game or editor. false if not.</returns>
        public bool IsGameOrEditor()
        {
            ToolSystem toolsystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            return toolsystem.actionMode.IsGameOrEditor();
        }

        /// <summary>
        /// Checks whether it is editor.
        /// </summary>
        /// <returns>True if in editor. false if not.</returns>
        public bool IsEditor()
        {
            ToolSystem toolsystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            return toolsystem.actionMode.IsEditor();
        }

        /// <summary>
        /// Checks whether to hide a water tool setting.
        /// </summary>
        /// <returns>True if hide, false if not.</returns>
        public bool DisableWaterToolSetting()
        {
            if (IsEditor() && !WaterToolSettingsAffectEditorSimulation)
            {
                return true;
            }

            if (!IsGameOrEditor())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Enables or disables water damage system based on toggle value.
        /// </summary>
        /// <param name="value">True to enable, false to disable.</param>
        public void WaterCausesDamageToggled(bool value)
        {
            WaterDamageSystem waterDamageSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WaterDamageSystem>();
            waterDamageSystem.Enabled = value;
        }
    }
}
