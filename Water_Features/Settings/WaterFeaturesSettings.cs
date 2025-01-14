// <copyright file="WaterFeaturesSettings.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Settings
{
    using Colossal.IO.AssetDatabase;
    using Colossal.PSI.Common;
    using Game;
    using Game.Input;
    using Game.Modding;
    using Game.Settings;
    using Game.Simulation;
    using Game.Tools;
    using Game.UI;
    using Unity.Entities;
    using Water_Features.Systems;
    using static Game.Prefabs.CompositionFlags;

    /// <summary>
    /// The mod settings for the Water Features Mod.
    /// </summary>
    [FileLocation("Mods_Yenyang_Water_Features")]
    [SettingsUITabOrder(SeasonalStreams, WaterToolGroup, WavesAndTides)]
    [SettingsUISection(SeasonalStreams, WaterToolGroup, WavesAndTides)]
    [SettingsUIShowGroupName(Stable, Experimental)]
    [SettingsUIGroupOrder(Stable, Experimental, Reset)]
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
        /// This is for settings for seasonal streams.
        /// </summary>
        public const string Stable = "Stable";

        /// <summary>
        /// This is for reset settings button group.
        /// </summary>
        public const string Reset = "Reset";

        /// <summary>
        /// This is for about section of settings.
        /// </summary>
        public const string About = "About";

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
        [SettingsUISection(WaterToolGroup, Stable)]
        public bool TrySmallerRadii { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to Include Detention Basins.
        /// </summary>
        [SettingsUISection(WaterToolGroup, Stable)]
        public bool IncludeDetentionBasins { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to Include Retention Basins.
        /// </summary>
        [SettingsUISection(WaterToolGroup, Stable)]
        public bool IncludeRetentionBasins { get; set; }

        /// <summary>
        /// Gets or sets the evaporatin rate for the whole map.
        /// </summary>
        [SettingsUISection(WaterToolGroup, Stable)]
        [SettingsUISlider(min = 0.01f, max = 1f, step = 0.01f, unit = Unit.kFloatTwoFractions, scalarMultiplier = 1000f)]
        public float EvaporationRate { get; set; }

        /// <summary>
        /// Sets a value indicating whether the toggle for applying a new evaporation rate is on.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(WaterToolGroup, Stable)]
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
        [SettingsUISection(SeasonalStreams, Stable)]
        [SettingsUISetter(typeof(WaterFeaturesSettings), nameof(SeasonalStreamsToggled))]
        public bool EnableSeasonalStreams { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to simulate snow melt with streams.
        /// </summary>
        [SettingsUISection(SeasonalStreams, Stable)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsSeasonalStreamsDisabled))]
        public bool SimulateSnowMelt { get; set; }

        /// <summary>
        /// Gets or sets a value with a slider indicating the multiplier for water always emitted from a stream.
        /// </summary>
        [SettingsUISlider(min = 0f, max = 100f, step = 1f, unit = "percentageSingleFraction", scalarMultiplier = 100f)]
        [SettingsUISection(SeasonalStreams, Stable)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsSeasonalStreamsDisabled))]
        public float ConstantFlowRate { get; set; }

        /// <summary>
        /// Gets or sets a value with a slider indicating the multiplier for water seaonally emitted from a stream.
        /// </summary>
        [SettingsUISlider(min = 0f, max = 100f, step = 5f, unit = "percentageSingleFraction", scalarMultiplier = 100f)]
        [SettingsUISection(SeasonalStreams, Stable)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsSeasonalStreamsDisabled))]
        public float StreamSeasonality { get; set; }

        /// <summary>
        /// Gets or sets a value with a slider indicating the multiplier for water emitted from a stream due to rain.
        /// </summary>
        [SettingsUISlider(min = 0f, max = 100f, step = 5f, unit = "percentageSingleFraction", scalarMultiplier = 100f)]
        [SettingsUISection(SeasonalStreams, Stable)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsSeasonalStreamsDisabled))]
        public float StreamStormwaterEffects { get; set; }

        /// <summary>
        /// Gets or sets a value with a slider indicating the minimum multiplier to apply to streams.
        /// </summary>
        [SettingsUISection(SeasonalStreams, Stable)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsSeasonalStreamsDisabled))]
        [SettingsUISlider(min = 0f, max = 1f, step = 0.1f, unit = "floatSingleFraction")]
        public float MinimumMultiplier { get; set; }

        /// <summary>
        /// Gets or sets a value with a slider indicating the maximum multiplier to apply to streams.
        /// </summary>
        [SettingsUISlider(min = 1f, max = 10f, step = 0.1f, unit = "floatSingleFraction")]
        [SettingsUISection(SeasonalStreams, Stable)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsSeasonalStreamsDisabled))]
        public float MaximumMultiplier { get; set; }

        /// <summary>
        /// Sets a value indicating whether: a button for Resetting the settings for the general tab.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(SeasonalStreams, Reset)]
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
        [SettingsUISection(WavesAndTides, Stable)]
        [SettingsUISetter(typeof(WaterFeaturesSettings), nameof(WavesAndTidesToggled))]
        public bool EnableWavesAndTides { get; set; }

        /// <summary>
        /// Gets or sets a value with a slider indicating the height of waves generated.
        /// </summary>
        [SettingsUISection(WavesAndTides, Stable)]
        [SettingsUISlider(min = 0f, max = 20f, step = 0.5f, unit = "floatSingleFraction")]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsWavesAndTidesDisabled))]
        [SettingsUISetter(typeof(WaterFeaturesSettings), nameof(ResetDummyWaterSource))]
        public float WaveHeight { get; set; }

        /// <summary>
        /// Gets or sets a value with a slider indicating the frequency of waves generated.
        /// </summary>
        [SettingsUISection(WavesAndTides, Stable)]
        [SettingsUISlider(min = 10f, max = 250f, step = 10f)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsWavesAndTidesDisabled))]
        public float WaveFrequency { get; set; }

        /// <summary>
        /// Gets or sets a value with a slider indicating the height of tides generated.
        /// </summary>
        [SettingsUISection(WavesAndTides, Stable)]
        [SettingsUISlider(min = 0f, max = 15f, step = 0.5f, unit = "floatSingleFraction")]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsWavesAndTidesDisabled))]
        [SettingsUISetter(typeof(WaterFeaturesSettings), nameof(ResetDummyWaterSource))]
        public float TideHeight { get; set; }

        /// <summary>
        /// Gets or sets an enum value indicating the tide classification.
        /// </summary>
        [SettingsUISection(WavesAndTides, Stable)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsWavesAndTidesDisabled))]
        public TideClassificationYYTAW TideClassification { get; set; }

        /// <summary>
        /// Gets or sets a value with a slider indicating the damping factor of the water system.
        /// </summary>
        [SettingsUISection(WavesAndTides, Stable)]
        [SettingsUISlider(min = 9950f, max = 9999f, step = 1f, unit = "floatSingleFraction", scalarMultiplier = 10000f)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsWavesAndTidesDisabled))]
        public float Damping { get; set; }

        /// <summary>
        /// Sets a value indicating whether: a button for Resetting the settings for the Waves and tides.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(WavesAndTides, Reset)]
        public bool ResetWavesAndTidesSettingsButton
        {
            set
            {
                ResetWavesAndTidesSettings();
            }
        }

        /// <summary>
        /// Gets or sets a value with a slider indicating the fluidness factor of the water system.
        /// </summary>
        [SettingsUISection(WaterToolGroup, Experimental)]
        [SettingsUISlider(min = 0.01f, max = 1.0f, step = 0.01f, unit = Unit.kFloatTwoFractions)]
        public float Fluidness { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Evaporation and Fluidness can be altered in Editor.
        /// </summary>
        [SettingsUISection(WaterToolGroup, Experimental)]
        public bool WaterToolSettingsAffectEditorSimulation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether seasonal streams affects the editor simulation.
        /// </summary>
        [SettingsUISection(SeasonalStreams, Experimental)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsSeasonalStreamsDisabled))]
        [SettingsUISetter(typeof(WaterFeaturesSettings), nameof(SeasonalStreamsAffectsEditorToggled))]
        public bool SeasonalStreamsAffectEditorSimulation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether waves and tides affects the editor simulation.
        /// </summary>
        [SettingsUISection(WavesAndTides, Experimental)]
        [SettingsUIHideByCondition(typeof(WaterFeaturesSettings), nameof(IsWavesAndTidesDisabled))]
        [SettingsUISetter(typeof(WaterFeaturesSettings), nameof(WavesAndTidesAffectsEditorSimulationToggled))]
        public bool WavesAndTidesAffectEditorSimulation { get; set; }

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
            SeasonalStreamsAffectEditorSimulation = false;
            SeasonalStreamsAffectsEditorToggled(false);
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
            WavesAndTidesAffectEditorSimulation = false;
            WavesAndTidesToggled(false);
        }

        /// <summary>
        /// Checks if seasonal streams feature is off or on.
        /// </summary>
        /// <returns>Opposite of Enable Seasonal Streams.</returns>
        public bool IsSeasonalStreamsDisabled() => !EnableSeasonalStreams;

        /// <summary>
        /// Checks if waves and tides feature is off or on.
        /// </summary>
        /// <returns>Opposite of Enable Waves and Tides.</returns>
        public bool IsWavesAndTidesDisabled() => !EnableWavesAndTides;

        /// <summary>
        /// Gets a value indicating the version.
        /// </summary>
        [SettingsUISection(SeasonalStreams, About)]
        public string Version => WaterFeaturesMod.Instance.Version;

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
            EnableSeasonalStreams = true;
            EnableWavesAndTides = false;
            Fluidness = 0.1f;
            WaterToolSettingsAffectEditorSimulation = false;
            SeasonalStreamsAffectEditorSimulation = false;
            WavesAndTidesAffectEditorSimulation = false;
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
            if (value && (toolSystem.actionMode.IsGame() || (toolSystem.actionMode.IsEditor() && SeasonalStreamsAffectEditorSimulation)))
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
        /// Handles toggling seasonal streams affects editor simulation.
        /// </summary>
        /// <param name="value">true if enabled, false if not.</param>
        public void SeasonalStreamsAffectsEditorToggled(bool value)
        {
            WaterFeaturesMod.Instance.Log.Debug($"{nameof(WaterFeaturesSettings)}.{nameof(SeasonalStreamsAffectsEditorToggled)}");
            SeasonalStreamsSystem seasonalStreamsSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<SeasonalStreamsSystem>();
            ToolSystem toolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            if (toolSystem.actionMode.IsGame() || (toolSystem.actionMode.IsEditor() && value))
            {
                WaterFeaturesMod.Instance.Log.Info($"{nameof(WaterFeaturesSettings)}.{nameof(SeasonalStreamsAffectsEditorToggled)} Enabled");
                seasonalStreamsSystem.Enabled = true;
                DisableSeasonalStreamSystem disableSeasonalStreamSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<DisableSeasonalStreamSystem>();
                FindWaterSourcesSystem findWaterSourcesSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<FindWaterSourcesSystem>();
                findWaterSourcesSystem.Enabled = true;
                disableSeasonalStreamSystem.Enabled = false;
            }
            else
            {
                WaterFeaturesMod.Instance.Log.Info($"{nameof(WaterFeaturesSettings)}.{nameof(SeasonalStreamsAffectsEditorToggled)} Disabled");
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
            if (value && (toolSystem.actionMode.IsGame() || (toolSystem.actionMode.IsEditor() && WavesAndTidesAffectEditorSimulation)))
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
        /// Handles toggling waves and tides either in game or editor.
        /// </summary>
        /// <param name="value">true if enabled, false if not.</param>
        public void WavesAndTidesAffectsEditorSimulationToggled(bool value)
        {
            WaterFeaturesMod.Instance.Log.Debug($"{nameof(WaterFeaturesSettings)}.{nameof(WavesAndTidesAffectsEditorSimulationToggled)}");
            TidesAndWavesSystem tidesAndWavesSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TidesAndWavesSystem>();
            ToolSystem toolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            if (toolSystem.actionMode.IsGame() || (toolSystem.actionMode.IsEditor() && value))
            {
                WaterFeaturesMod.Instance.Log.Info($"{nameof(WaterFeaturesSettings)}.{nameof(WavesAndTidesAffectsEditorSimulationToggled)} Enabled");
                tidesAndWavesSystem.Enabled = true;
                DisableWavesAndTidesSystem disableWavesAndTidesSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<DisableWavesAndTidesSystem>();
                FindWaterSourcesSystem findWaterSourcesSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<FindWaterSourcesSystem>();
                findWaterSourcesSystem.Enabled = true;
                disableWavesAndTidesSystem.Enabled = false;
            }
            else
            {
                WaterFeaturesMod.Instance.Log.Info($"{nameof(WaterFeaturesSettings)}.{nameof(WavesAndTidesAffectsEditorSimulationToggled)} Disabled");
                tidesAndWavesSystem.Enabled = false;
                DisableWavesAndTidesSystem disableWavesAndTidesSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<DisableWavesAndTidesSystem>();
                disableWavesAndTidesSystem.Enabled = true;
            }
        }
    }
}
