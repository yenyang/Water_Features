// <copyright file="LocaleEN.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Settings
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Colossal;
    using Colossal.IO.AssetDatabase.Internal;

    /// <summary>
    /// Localization for <see cref="WaterFeaturesMod"/> in English.
    /// </summary>
    public class LocaleEN : IDictionarySource
    {
        private readonly WaterFeaturesSettings m_Setting;

        private Dictionary<string, string> m_Localization;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocaleEN"/> class.
        /// </summary>
        /// <param name="setting">Settings class.</param>
        public LocaleEN(WaterFeaturesSettings setting)
        {
            m_Setting = setting;

            m_Localization = new Dictionary<string, string>()
            {
                { m_Setting.GetSettingsLocaleID(), "Water Features" },
                { m_Setting.GetOptionTabLocaleID(WaterFeaturesSettings.SeasonalStreams), "Seasonal Streams" },
                { m_Setting.GetOptionTabLocaleID(WaterFeaturesSettings.WaterToolGroup), "Water Tool" },
                { m_Setting.GetOptionTabLocaleID(WaterFeaturesSettings.WavesAndTides), "Waves and Tides" },
                { m_Setting.GetOptionGroupLocaleID(WaterFeaturesSettings.SeasonalStreams), "Seasonal Streams" },
                { m_Setting.GetOptionGroupLocaleID(WaterFeaturesSettings.WaterToolGroup), "Water Tool" },
                { m_Setting.GetOptionGroupLocaleID(WaterFeaturesSettings.WavesAndTides), "Waves and Tides" },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.IncludeRetentionBasins)), "Add Retention Basins (Restart Required)" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.IncludeRetentionBasins)), "Custom modded water source that rises with precipitation and snowmelt and slowly drains when the weather is dry. They have a maximum water surface elevation and a minimum water surface elevation. You may need to adjust the global evaporation rate in the settings for desirable infiltration of the pond water." },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.IncludeDetentionBasins)), "Add Detention Basins (Restart Required)" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.IncludeDetentionBasins)), "Custom modded water source that rises with precipitation and snowmelt and slowly drains when the weather is dry. They have a maximum water surface elevation but no minimum water surface elevation. You may need to adjust the global evaporation rate in the settings for desirable infiltration of the pond water." },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.TrySmallerRadii)), "Try Smaller Radii" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.TrySmallerRadii)), "Lets you try to make a water source with a radius smaller than 5m. It will not always work, but will be increased to a radius that does work." },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.EvaporationRate)), "Evaporation Rate" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.EvaporationRate)), "Should probably leave at default unless you are using detention or retention basins. Actual evaporation rate is 1000x smaller but this gives you control over the rate. This is global and you may want to rebalance and increase flows of water sources after increasing it." },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.EnableSeasonalStreams)), "Enable Seasonal Streams" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.EnableSeasonalStreams)), "Seasonal streams takes Streams (Modified Constant Rate Water Sources) and ties them to the climate and weather for the map. For example, if your map features a dry summer, then these water sources will decrease during the summer. Seasonal streams by it-self should not cause flooding since it treats the map's default water source amount as a maximum unless you change it. All aspects are optional and adjustable in the mod's settings." },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.SimulateSnowMelt)), "Simulate Snow Melt" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.SimulateSnowMelt)), "Simulate snow accumulation and snow melt. Snow melt is not currently tied to snow visuals. This affects Constant Rate Water Sources, Detention and Retention basins." },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.ConstantFlowRate)), "Constant Flow Rate" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.ConstantFlowRate)), "Constant flow rate from  Streams (Modified Constant Rate Water Sources) unaffected by season or weather." },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.StreamSeasonality)), "Seasonality" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.StreamSeasonality)), "Streams (Modified Constant Rate Water Sources) flowrates will increase by this percentage during the season with the highest mean seasonal precipitation. Other seasons will be proportional." },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.StreamStormwaterEffects)), "Stormwater Effects" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.StreamStormwaterEffects)), "Streams (Modified Constant Rate Water Sources) flowrates will increase by this percentage when current precipitation (rain) is at a maximum. Less precipitation is proportional." },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.MinimumMultiplier)), "Minimum Multiplier" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.MinimumMultiplier)), "Minimum multiplier applied to stream flowrates." },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.MaximumMultiplier)), "Maximum Multiplier" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.MaximumMultiplier)), "Maximum multiplier applied to stream flowrates." },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.EnableWavesAndTides)), "Enable Waves and Tides" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.EnableWavesAndTides)), "This feature is dependent on map design. Maps with a sea water source and a single shoreline work best. The point of the waves feature is to make the shore move in and out and make sand along the shoreline. A better way to make beaches is to just paint them with surface painter instead. Waves exacerbate the magnitude of the water surface. Tides happen once or twice a day. Waves and tides are always lower than the original sea level and do not cause flooding." },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.WaveHeight)), "Wave Height" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.WaveHeight)), "Waves are generated at the map boundary where there is a Sea water source. Once generated they head towards shore. Maps were not necessarily designed for these waves, and the waves exacerbate the water surface. Waves are lower than the sea level from the original map and do not cause flooding. Maps such as San Francisco with shallow seas will need waves and tides with smaller heights to avoid large swathes of non-playable area becoming dry sand." },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.WaveFrequency)), "Wave Frequency" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.WaveFrequency)), "Frequency for waves per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.TideHeight)), "Tide Height" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.TideHeight)), "Tides are the biggest waves and they cause the sea to rise and fall along the shore. Tides can add sandy graphics along shorelines but the sand may not persist the entire time between low tide and high tide. Tides are lower than the sea level from the original map and do not cause flooding. Maps such as San Francisco with shallow seas will need waves and tides with smaller heights to avoid large swathes of non-playable area becoming dry sand. " },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.TideClassification)), "Tide Classification" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.TideClassification)), "Diurnal tides have one high and one low tide per day. Semidiurnal has two high tides and two low tides per day." },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.Damping)), "Damping" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.Damping)), "A higher value makes waves stronger while a lower value makes waves weaker. Stronger waves reach farther into the map. Weak waves may disperse before reaching shore. Vanilla is 9950 Recommended 9980-9999. The actual value is 10000x less." },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.WaterCleanUpCycleButton)), "Water Clean Up Cycle" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.WaterCleanUpCycleButton)), "This will increase the rate of evaporation by 1000x FOR THE WHOLE MAP for a short amount of time before returning to normal. For cleaning up water messes but USE AT YOUR OWN RISK! Lakes with an target elevation below the ground level are a safer way to remove water." },
                { m_Setting.GetOptionWarningLocaleID(nameof(WaterFeaturesSettings.WaterCleanUpCycleButton)), "Start Water Clean Up Cycle?" },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.ResetWaterToolGroupButton)), "Reset Water Tool Settings" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.ResetWaterToolGroupButton)), "On confirmation, resets Water Tool Settings." },
                { m_Setting.GetOptionWarningLocaleID(nameof(WaterFeaturesSettings.ResetWaterToolGroupButton)), "Reset Water Tool Settings?" },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.ResetSeasonalStreamsSettingsButton)), "Reset Seasonal Streams Settings" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.ResetSeasonalStreamsSettingsButton)), "On confirmation, resets Seasonal Streams Settings." },
                { m_Setting.GetOptionWarningLocaleID(nameof(WaterFeaturesSettings.ResetSeasonalStreamsSettingsButton)), "Reset Seasonal Streams Settings?" },
                { m_Setting.GetOptionLabelLocaleID(nameof(WaterFeaturesSettings.ResetWavesAndTidesSettingsButton)), "Reset Waves and Tides Settings" },
                { m_Setting.GetOptionDescLocaleID(nameof(WaterFeaturesSettings.ResetWavesAndTidesSettingsButton)), "On confirmation, resets Waves and Tides Settings." },
                { m_Setting.GetOptionWarningLocaleID(nameof(WaterFeaturesSettings.ResetWavesAndTidesSettingsButton)), "Reset Waves and Tides Settings?" },
                { m_Setting.GetEnumValueLocaleID(WaterFeaturesSettings.TideClassificationYYTAW.Diurnal), "Diurnal" },
                { m_Setting.GetEnumValueLocaleID(WaterFeaturesSettings.TideClassificationYYTAW.Semidiurnal), "Semidiurnal" },
                { "YY_WATER_FEATURES.Flow", "Flow" },
                { "YY_WATER_FEATURES.Depth", "Depth" },
                { "YY_WATER_FEATURES.MaxDepth", "Max Depth" },
                { "YY_WATER_FEATURES.Elevation", "Elevation" },
                { "YY_WATER_FEATURES.MinElevation", "Min Elevation" },
                { "YY_WATER_FEATURES.MaxElevation", "Max Elevation" },
                { "YY_WATER_FEATURES.SnowAccumulation", "Snow Accumulation" },
                { "YY_WATER_FEATURES.OriginalFlow", "Original Flow" },
                { "YY_WATER_FEATURES.MinDepth", "Min Depth" },
                { "YY_WATER_FEATURES.Radius", "Radius" },
                { "YY_WATER_FEATURES.amount-down-arrow", "Reduce Flow/Depth/Elevation" },
                { "YY_WATER_FEATURES_DESCRIPTION.amount-down-arrow", "Reduces the flow for Streams. Decreases the depth or elevation for rivers, seas, and lakes. Reduces the max depth for retention and detention basins." },
                { "YY_WATER_FEATURES.amount-up-arrow", "Increase Flow/Depth/Elevation" },
                { "YY_WATER_FEATURES_DESCRIPTION.amount-up-arrow", "Increases the flow for Streams. Increases the depth or elevation for rivers, seas, and lakes. Increases the max depth for retention and detention basins." },
                { "YY_WATER_FEATURES.radius-down-arrow", "Reduce Radius" },
                { "YY_WATER_FEATURES_DESCRIPTION.radius-down-arrow", "Reduces the radius." },
                { "YY_WATER_FEATURES.radius-up-arrow", "Increase Radius" },
                { "YY_WATER_FEATURES_DESCRIPTION.radius-up-arrow", "Increases the radius.." },
                { "YY_WATER_FEATURES.min-depth-down-arrow", "Reduce Min Depth" },
                { "YY_WATER_FEATURES_DESCRIPTION.min-depth-down-arrow", "Reduces the minimum depth." },
                { "YY_WATER_FEATURES.min-depth-up-arrow", "Increase Min Depth" },
                { "YY_WATER_FEATURES_DESCRIPTION.min-depth-up-arrow", "Increases the minimum depth." },
                { "YY_WATER_FEATURES.amount-rate-of-change", "Flow/Depth/Elevation Rate of Change" },
                { "YY_WATER_FEATURES_DESCRIPTION.amount-rate-of-change", "Changes the rate in which the increase and decrease buttons work for Flow, Depth and Elevation." },
                { "YY_WATER_FEATURES.radius-rate-of-change", "Radius Rate of Change" },
                { "YY_WATER_FEATURES_DESCRIPTION.radius-rate-of-change", "Changes the rate in which the increase and decrease buttons work for Radius." },
                { "YY_WATER_FEATURES.min-depth-rate-of-change", "Minimum Depth Rate of Change" },
                { "YY_WATER_FEATURES_DESCRIPTION.min-depth-rate-of-change", "Changes the rate in which the increase and decrease buttons work for minimum depth." },
                { "SubServices.NAME[WaterTool]", "Water Tool" },
                { "Assets.SUB_SERVICE_DESCRIPTION[WaterTool]", "Water tool allows you to add and remove water sources from your map." },
                { "Assets.NAME[WaterSource Stream]", "Stream - Constant or Variable Rate Water Source" },
                { "Assets.DESCRIPTION[WaterSource Stream]", "Emits water depending on the settings for this mod. With Seasonal Streams disabled, the flow rate will be constant. With Seasonal Streams enabled the flow rate will vary with season, precipitation, and snowmelt depending on your settings. Left click to place within playable area. Hover over and right click to remove." },
                { "Assets.NAME[WaterSource River]", "River - Border River Water Source" },
                { "Assets.DESCRIPTION[WaterSource River]", "Has a constant level and controls water flowing into or out of the border. While near the border, the source will snap to the border. Right click to designate the target elevation. Left click to place. Hover over and right click to remove." },
                { "Assets.NAME[WaterSource Sea]", "Sea - Border Sea Water Source" },
                { "Assets.DESCRIPTION[WaterSource Sea]", "Controls water flowing into or out of the border and the lowest sea controls sea level. With Waves and Tides disabled, it will maintain constant level. With Waves and Tides enabled the sea level rises and falls below the original sea level. Right click to designate the elevation. Left click to place if the radius touches a border. Hover over and right click to remove." },
                { "Assets.NAME[WaterSource Lake]", "Lake - Constant Level Water Source" },
                { "Assets.DESCRIPTION[WaterSource Lake]", "Fills quickly until it gets to the desired level and then maintains that level. If it has a target elevation below the ground level, it can drain water faster than evaporation. Right click to designate the target elevation. Left click to place within playable area. Hover over and right click to remove." },
                { "Assets.NAME[WaterSource DetentionBasin]", "Detention Basin" },
                { "Assets.DESCRIPTION[WaterSource DetentionBasin]", "Custom modded water source that rises with precipitation and snowmelt and slowly drains when the weather is dry. They have a maximum water surface elevation but no minimum water surface elevation. Right click to designate the maximum elevation. Left click to place within playable area. Hover over and right click to remove." },
                { "Assets.NAME[WaterSource RetentionBasin]", "Retention Basin" },
                { "Assets.DESCRIPTION[WaterSource RetentionBasin]", "Custom modded water source that rises with precipitation and snowmelt and slowly drains when the weather is dry. They have a maximum water surface elevation and a minimum water surface elevation. Right click to designate the maximum elevation. Left click to place within playable area. Hover over and right click to remove." },
                { "Tooltip.LABEL[YY.WT.PlaceNearBorder]", "Rivers must be placed near map border." },
                { "Tooltip.LABEL[YY.WT.RadiusTooSmall]", "The radius is too small and has been automically increased." },
                { "Tooltip.LABEL[YY.WT.RemoveWaterSource]", "Right click to remove water source." },
                { "Tooltip.LABEL[YY.WT.PlaceInsideBorder]", "This water source must be placed inside the playable map." },
                { "Tooltip.LABEL[YY.WT.MustTouchBorder]", "Sea water sources must touch the map border." },
                { "Tooltip.LABEL[YY.WT.LockElevation]", "Right click to designate the water surface elevation." },
            };
        }


        /// <inheritdoc/>
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return m_Localization;
        }

        /// <inheritdoc/>
        public void Unload()
        {
        }

        /// <summary>
        /// Exports a localization CSV template with this files dictionary as default entries.
        /// </summary>
        /// <param name="folderPath">the path of where the file should be created.</param>
        /// <param name="langCodes">the language codes to be included in the template file.</param>
        /// <returns>True if the file is created. False if not.</returns>
        public bool ExportLocalizationCSV(string folderPath, string[] langCodes)
        {
            System.IO.Directory.CreateDirectory(folderPath);
            string localizationFilePath = Path.Combine(folderPath, $"l10n.csv");
            if (!File.Exists(localizationFilePath))
            {
                try
                {
                    using (StreamWriter streamWriter = new(File.Create(localizationFilePath)))
                    {
                        StringBuilder topLine = new StringBuilder();
                        topLine.Append("key\t");
                        foreach (string langCode in langCodes)
                        {
                            topLine.Append(langCode);
                            topLine.Append("\t");
                        }

                        streamWriter.WriteLine(topLine.ToString());

                        foreach (KeyValuePair<string, string> kvp in m_Localization)
                        {
                            StringBuilder currentLine = new StringBuilder();
                            currentLine.Append(kvp.Key);
                            currentLine.Append("\t");
                            foreach (string langCode in langCodes)
                            {
                                currentLine.Append(kvp.Value);
                                currentLine.Append("\t");
                            }

                            streamWriter.WriteLine(currentLine.ToString());
                        }

                        return true;
                    }
                }
                catch (Exception e)
                {
                    WaterFeaturesMod.Instance.Log.Warn($"{typeof(LocaleEN)}.{nameof(ExportLocalizationCSV)} Encountered Exception {e} while trying to export localization csv.");
                    return false;
                }
            }

            return false;
        }
    }
}
