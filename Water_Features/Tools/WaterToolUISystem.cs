// <copyright file="WaterToolUISystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using Colossal.Logging;
    using Colossal.PSI.Environment;
    using Colossal.Serialization.Entities;
    using Colossal.UI.Binding;
    using Game;
    using Game.Prefabs;
    using Game.SceneFlow;
    using Game.Simulation;
    using Game.Tools;
    using Game.UI;
    using Game.UI.Editor;
    using Unity.Entities;
    using UnityEngine;
    using Water_Features;
    using Water_Features.Domain;
    using Water_Features.Prefabs;
    using Water_Features.Settings;
    using Water_Features.Utils;

    /// <summary>
    /// UI system for Custom Water Tool.
    /// </summary>
    public partial class WaterToolUISystem : UISystemBase
    {
        private const string ModId = "Water_Features";

        private ToolSystem m_ToolSystem;
        private CustomWaterToolSystem m_CustomWaterToolSystem;
        private TerrainSystem m_TerrainSystem;
        private ILog m_Log;
        private Dictionary<string, Action> m_ChangeValueActions;
        private bool m_ResetValues = true;
        private string m_ContentFolder;
        private Dictionary<WaterSourcePrefab, WaterSourcePrefabValuesRepository> m_WaterSourcePrefabValuesRepositories;
        private ValueBinding<float> m_Radius;
        private ValueBinding<float> m_Amount;
        private ValueBinding<float> m_MinDepth;
        private ValueBinding<string> m_AmountLocaleKey;
        private ValueBinding<float> m_RadiusStep;
        private ValueBinding<float> m_AmountStep;
        private ValueBinding<float> m_MinDepthStep;
        private ValueBinding<int> m_AmountScale;
        private ValueBinding<int> m_MinDepthScale;
        private ValueBinding<int> m_RadiusScale;
        private ValueBinding<bool> m_ShowMinDepth;
        private ValueBinding<string> m_ActivePrefabName;
        private ValueBinding<WaterSourcePrefabList> m_WaterSourcePrefabList;
        private ValueBinding<bool> m_AmountIsElevation;
        private EditorToolUISystem m_EditorToolUISystem;
        private ValueBinding<int> m_ToolMode;
        private PrefabSystem m_PrefabSystem;

        /// <summary>
        /// Types of water sources.
        /// </summary>
        public enum SourceType
        {
            /// <summary>
            /// Constant Rate Water Sources that may vary with season and precipitation.
            /// </summary>
            Stream,

            /// <summary>
            /// Constant level water sources.
            /// </summary>
            VanillaLake,

            /// <summary>
            /// Border River water sources.
            /// </summary>
            River,

            /// <summary>
            /// Border Sea water sources. Level may change with waves and tides.
            /// </summary>
            Sea,

            /// <summary>
            /// Starts as a stream and settles into a vanilla lake.
            /// </summary>
            Lake,

            /// <summary>
            /// Pond that fills when its rainy but will empty completely eventually.
            /// </summary>
            DetentionBasin,

            /// <summary>
            /// Pond that fills when its raining and has a minimum water level.
            /// </summary>
            RetentionBasin,

            /// <summary>
            /// All in one water source.
            /// </summary>
            Automated,

            /// <summary>
            /// Vanilla water source for v2.0.
            /// </summary>
            Generic,

            /// <summary>
            /// Generic water source the rises and falls with weather and season.
            /// </summary>
            Seasonal,
        }

        /// <summary>
        /// Gets the radius.
        /// </summary>
        public float Radius { get => m_Radius.value;  }

        /// <summary>
        /// Gets the amount.
        /// </summary>
        public float Height { get => m_Amount.value; }

        /// <summary>
        /// Gets the min depth.
        /// </summary>
        public float MinDepth { get => m_MinDepth.value; }

        /// <summary>
        /// Gets the tool mode for customw ater tool.
        /// </summary>
        public CustomWaterToolSystem.ToolModes ToolMode { get => (CustomWaterToolSystem.ToolModes)m_ToolMode.value; }

        /// <summary>
        /// Gets a value indicating whether the amount is an elevation.
        /// </summary>
        public bool HeightIsAnElevation { get => m_AmountIsElevation.value; }

        /// <summary>
        /// Sets the amount value equal to elevation parameter. And sets the label for that row to Elevation.
        /// </summary>
        /// <param name="elevation">The y coordinate from the raycast hit position.</param>
        public void SetElevation(float elevation)
        {
            elevation = Mathf.Clamp(elevation, m_TerrainSystem.GetTerrainBounds().min.y, m_TerrainSystem.GetTerrainBounds().max.y);
            elevation = Mathf.Round(elevation * 10f) * 0.1f;
            m_Amount.Update(elevation);
            m_AmountScale.Update(1);
            m_AmountIsElevation.Update(true);
            m_AmountLocaleKey.Update("YY_WATER_FEATURES.Elevation");
        }

        /// <summary>
        /// Tries to save the new default values for a water source for the next time they are placed.
        /// </summary>
        /// <param name="waterSource">Generally the active prefab for custom water tool.</param>
        /// <param name="amount">The next default amount that will be saved.</param>
        /// <param name="radius">The next default radius that will be saved.</param>
        /// <returns>True if the information is saved. False if an exception is encountered.</returns>
        public bool TrySaveDefaultValuesForWaterSource(WaterSourcePrefab waterSource, float amount, float radius)
        {
            string fileName = Path.Combine(m_ContentFolder, $"{waterSource.m_SourceType}.xml");
            WaterSourcePrefabValuesRepository repository = new WaterSourcePrefabValuesRepository() { Amount = amount, Radius = radius };
            try
            {
                XmlSerializer serTool = new XmlSerializer(typeof(WaterSourcePrefabValuesRepository)); // Create serializer
                using (System.IO.FileStream file = System.IO.File.Create(fileName)) // Create file
                {
                    serTool.Serialize(file, repository); // Serialize whole properties
                }

                if (m_WaterSourcePrefabValuesRepositories.ContainsKey(waterSource))
                {
                    m_WaterSourcePrefabValuesRepositories[waterSource].Amount = amount;
                    m_WaterSourcePrefabValuesRepositories[waterSource].Radius = radius;
                    m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(TrySaveDefaultValuesForWaterSource)} updating repository for {waterSource.m_SourceType}.");
                }
                else
                {
                    m_WaterSourcePrefabValuesRepositories.Add(waterSource, repository);
                    m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(TrySaveDefaultValuesForWaterSource)} adding repository for {waterSource.m_SourceType}.");
                }

                return true;
            }
            catch (Exception ex)
            {
                m_Log.Warn($"{nameof(WaterToolUISystem)}.{nameof(TryGetDefaultValuesForWaterSource)} Could not save values for water source WaterSource {waterSource.m_SourceType}. Encountered exception {ex}");
                return false;
            }
        }

        /// <summary>
        /// Tries to save the new default values for a water source for the next time they are placed.
        /// </summary>
        /// <param name="waterSource">Generally the active prefab for custom water tool.</param>
        /// <param name="radius">The next default radius that will be saved.</param>
        /// <returns>True if the information is saved. False if an exception is encountered.</returns>
        public bool TrySaveDefaultValuesForWaterSource(WaterSourcePrefab waterSource, float radius)
        {
            if (m_WaterSourcePrefabValuesRepositories.ContainsKey(waterSource))
            {
                float amount = m_WaterSourcePrefabValuesRepositories[waterSource].Amount;
                return TrySaveDefaultValuesForWaterSource(waterSource, amount, radius);
            }

            return false;
        }

        /// <summary>
        /// Sets the list of water source prefabs for the editor.
        /// </summary>
        /// <param name="waterSourcePrefabList">List of prefab names.</param>
        public void SetPrefabList(WaterSourcePrefabList waterSourcePrefabList)
        {
            m_WaterSourcePrefabList.Update(waterSourcePrefabList);
            m_WaterSourcePrefabList.TriggerUpdate();
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = WaterFeaturesMod.Instance.Log;
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_CustomWaterToolSystem = World.GetOrCreateSystemManaged<CustomWaterToolSystem>();
            m_TerrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();
            m_ToolSystem.EventPrefabChanged += OnPrefabChanged;
            m_ContentFolder = Path.Combine(EnvPath.kUserDataPath, "ModsData", "Mods_Yenyang_Water_Features");
            Directory.CreateDirectory(m_ContentFolder);
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_EditorToolUISystem = World.GetOrCreateSystemManaged<EditorToolUISystem>();
            IEditorTool[] existingTools = m_EditorToolUISystem.tools;
            IEditorTool[] newTools = new IEditorTool[existingTools.Length + 1];
            int i = 0;
            foreach (IEditorTool currentTool in existingTools)
            {
                newTools[i++] = currentTool;
            }

            newTools[i] = new CustomEditorWaterTool(World.DefaultGameObjectInjectionWorld);
            m_EditorToolUISystem.tools = newTools;

            // This binding communicates the value for Amount.
            AddBinding(m_Amount = new ValueBinding<float>(ModId, "AmountValue", 1f));

            // This binding communicates the value for Radius.
            AddBinding(m_Radius = new ValueBinding<float>(ModId, "RadiusValue", 5f));

            // This binding communicates the value for Min Depth.
            AddBinding(m_MinDepth = new ValueBinding<float>(ModId, "MinDepthValue", 10f));

            // This binding communicates the Locale Key for the Amount section.
            AddBinding(m_AmountLocaleKey = new ValueBinding<string>(ModId, "AmountLocaleKey", "YY_WATER_FEATURES.Depth"));

            // This binding communicates the value of the selected Radius Step.
            AddBinding(m_RadiusStep = new ValueBinding<float>(ModId, "RadiusStep", 1f));

            // This binding communicates the value of the selected Amount Step.
            AddBinding(m_AmountStep = new ValueBinding<float>(ModId, "AmountStep", 1f));

            // This binding communicates the value of the selected Min Depth step.
            AddBinding(m_MinDepthStep = new ValueBinding<float>(ModId, "MinDepthStep", 1f));

            // This binding communicates the value of the selected Amount scale.
            AddBinding(m_AmountScale = new ValueBinding<int>(ModId, "AmountScale", 0));

            // This binding communicates the value of the selected Min Depth Scale.
            AddBinding(m_MinDepthScale = new ValueBinding<int>(ModId, "MinDepthScale", 0));

            // This binding communicates the value of the selected Radius scale.
            AddBinding(m_RadiusScale = new ValueBinding<int>(ModId, "RadiusScale", 0));

            // This binding communicates whether Min Depth section should be shown.
            AddBinding(m_ShowMinDepth = new ValueBinding<bool>(ModId, "ShowMinDepth", false));

            // This binding communicates the ActivePrefabName when using Custom Water tool in editor.
            AddBinding(m_ActivePrefabName = new ValueBinding<string>(ModId, "ActivePrefabName", "WaterSource Stream"));

            // This binding communicates the ActivePrefabName when using Custom Water tool in editor.
            AddBinding(m_ToolMode = new ValueBinding<int>(ModId, "ToolMode", (int)CustomWaterToolSystem.ToolModes.PlaceWaterSource));

            // This binding communicates a list of water source prefab names to be used in the editor.
            AddBinding(m_WaterSourcePrefabList = new ValueBinding<WaterSourcePrefabList>(ModId, "WaterSourcePrefabList", new WaterSourcePrefabList() { waterSourcePrefabUIDatas = new List<WaterSourcePrefabUIData>() }));

            // This binding communicates whether amount is an elevation.
            AddBinding(m_AmountIsElevation = new ValueBinding<bool>(ModId, "AmountIsElevation", false));

            // This binding listens for whether the amount-up-arrow button was clicked.
            AddBinding(new TriggerBinding(ModId, "amount-up-arrow", IncreaseAmount));

            // This binding listens for whether the amount-down-arrow button was clicked.
            AddBinding(new TriggerBinding(ModId, "amount-down-arrow", DecreaseAmount));

            // This binding listens for whether the min-depth-up-arrow button was clicked.
            AddBinding(new TriggerBinding(ModId, "min-depth-up-arrow", IncreaseMinDepth));

            // This binding listens for whether the min-depth-down-arrow button was clicked.
            AddBinding(new TriggerBinding(ModId, "min-depth-down-arrow", DecreaseMinDepth));

            // This binding listens for whether the radius-up-arrow button was clicked.
            AddBinding(new TriggerBinding(ModId, "radius-up-arrow", IncreaseRadius));

            // This binding listens for whether the radius-down-arrow button was clicked.
            AddBinding(new TriggerBinding(ModId, "radius-down-arrow", DecreaseRadius));

            // This binding listens for whether the amount-rate-of-change button was clicked.
            AddBinding(new TriggerBinding(ModId, "amount-rate-of-change", AmountStepPressed));

            // This binding listens for whether the radius-rate-of-change button was clicked.
            AddBinding(new TriggerBinding(ModId, "min-depth-rate-of-change", MinDepthStepPressed));

            // This binding listens for whether the min-depth-rate-of-change button was clicked.
            AddBinding(new TriggerBinding(ModId, "radius-rate-of-change", RadiusStepPressed));

            // This binding handles changing prefabs for editor version of custom water tool.
            AddBinding(new TriggerBinding<string>(ModId, "PrefabChange", ChangePrefab));

            // This binding hanldes changing tool mode for water tool.
            AddBinding(new TriggerBinding<int>(ModId, "ChangeToolMode", ChangeToolMode));

            // This binding handles toggle amount is elevation.
            AddBinding(new TriggerBinding(ModId, "AmountIsElevationToggled", AmountIsElevationToggled));

            m_WaterSourcePrefabValuesRepositories = new Dictionary<WaterSourcePrefab, WaterSourcePrefabValuesRepository>();
        }


        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

#if DUMP_VANILLA_LOCALIZATION && DEBUG
            var strings = GameManager.instance.localizationManager.activeDictionary.entries
                .OrderBy(kv => kv.Key)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            var json = Colossal.Json.JSON.Dump(strings);

            var filePath = Path.Combine(Application.persistentDataPath, "locale-dictionary.json");

            File.WriteAllText(filePath, json);
#endif
        }

        /// <summary>
        /// C# Event handler for event callback form UI Javascript. Exectutes an action depending on button pressed.
        /// </summary>
        /// <param name="buttonID">The id of the button pressed.</param>
        private void ChangeValue(string buttonID)
        {
            if (buttonID == null)
            {
                m_Log.Warn($"{nameof(WaterToolUISystem)}.{nameof(ChangeValue)} buttonID was null.");
                return;
            }


            m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(ChangeValue)} buttonID = {buttonID}");
            if (m_ChangeValueActions.ContainsKey(buttonID))
            {
                m_ChangeValueActions[buttonID].Invoke();
            }

        }

        private void IncreaseRadius()
        {
            float signaficantFigures = Mathf.Pow(10f, -1f * Mathf.Log(m_RadiusStep.value, 2f));
            float tempRadius = m_Radius.value;
            if (tempRadius >= 1000f && tempRadius < 10000f)
            {
                tempRadius += 500f * m_RadiusStep.value;
                tempRadius = Mathf.Round(tempRadius * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
                m_RadiusScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_RadiusStep.value, 2f)) - 2));
            }
            else if (tempRadius >= 500f && tempRadius < 1000f)
            {
                tempRadius += 100f * m_RadiusStep.value;
                tempRadius = Mathf.Round(tempRadius * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
                m_RadiusScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_RadiusStep.value, 2f)) - 2));
            }
            else if (tempRadius >= 100f && tempRadius < 500f)
            {
                tempRadius += 50f * m_RadiusStep.value;
                tempRadius = Mathf.Round(tempRadius * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                m_RadiusScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_RadiusStep.value, 2f)) - 1));
            }
            else if (tempRadius >= 10f && tempRadius < 100f)
            {
                tempRadius += 10f * m_RadiusStep.value;
                tempRadius = Mathf.Round(tempRadius * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                m_RadiusScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_RadiusStep.value, 2f)) - 1));
            }
            else if (tempRadius < 10000)
            {
               tempRadius += 1f * m_RadiusStep.value;
               tempRadius = Mathf.Round(tempRadius * signaficantFigures) / signaficantFigures;
               m_RadiusScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_RadiusStep.value, 2f))));
            }

            if (WaterFeaturesMod.Instance.Settings.TrySmallerRadii)
            {
                tempRadius = Mathf.Clamp(tempRadius, 1f, 10000f);
            }
            else
            {
                tempRadius = Mathf.Clamp(tempRadius, 5f, 10000f);
            }

            // This updates the binding with the new value after all changes have occured.
            m_Radius.Update(tempRadius);
        }

        private void DecreaseRadius()
        {
            float signaficantFigures = Mathf.Pow(10f, -1f * Mathf.Log(m_RadiusStep.value, 2f));
            float tempRadius = m_Radius.value;
            if (tempRadius <= 10f && tempRadius > 1f)
            {
                tempRadius -= 1f * m_RadiusStep.value;
                tempRadius = Mathf.Round(tempRadius * signaficantFigures) / signaficantFigures;
                m_RadiusScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_RadiusStep.value, 2f))));
            }
            else if (tempRadius <= 100f && tempRadius > 10f)
            {
                tempRadius -= 10f * m_RadiusStep.value;
                tempRadius = Mathf.Round(tempRadius * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                m_RadiusScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_RadiusStep.value, 2f)) - 1));
            }
            else if (tempRadius <= 500f && tempRadius > 100f)
            {
                tempRadius -= 50f * m_RadiusStep.value;
                tempRadius = Mathf.Round(tempRadius * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                m_RadiusScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_RadiusStep.value, 2f)) - 1));
            }
            else if (tempRadius <= 1000f && tempRadius > 500f)
            {
                tempRadius -= 100f * m_RadiusStep.value;
                tempRadius = Mathf.Round(tempRadius * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
                m_RadiusScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_RadiusStep.value, 2f)) - 2));
            }
            else if (tempRadius > 1000f)
            {
                tempRadius -= 500f * m_RadiusStep.value;
                tempRadius = Mathf.Round(tempRadius * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
                m_RadiusScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_RadiusStep.value, 2f)) - 2));
            }

            if (WaterFeaturesMod.Instance.Settings.TrySmallerRadii)
            {
                tempRadius = Mathf.Clamp(tempRadius, 1f, 10000f);
            }
            else
            {
                tempRadius = Mathf.Clamp(tempRadius, 5f, 10000f);
            }

            // This updates the binding with the new value after all changes have occured.
            m_Radius.Update(tempRadius);
        }

        private void IncreaseMinDepth()
        {
            float signaficantFigures = Mathf.Pow(10f, -1f * Mathf.Log(m_MinDepthStep.value, 2f));
            float tempValue = m_MinDepth.value;
            if (tempValue >= 500f && tempValue < 1000f)
            {
                tempValue += 100f * m_MinDepthStep.value;
                tempValue = Mathf.Round(tempValue * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
                m_MinDepthScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_MinDepthStep.value, 2f)) - 2));
            }
            else if (tempValue >= 100f && tempValue < 500f)
            {
                tempValue += 50f * m_MinDepthStep.value;
                tempValue = Mathf.Round(tempValue * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                m_MinDepthScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_MinDepthStep.value, 2f)) - 1));
            }
            else if (tempValue < 100f && tempValue >= 10f)
            {
                tempValue += 10f * m_MinDepthStep.value;
                tempValue = Mathf.Round(tempValue * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                m_MinDepthScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_MinDepthStep.value, 2f)) - 1));
            }
            else if (tempValue < 10f && tempValue >= 1f)
            {
                tempValue += 1f * m_MinDepthStep.value;
                tempValue = Mathf.Round(tempValue * signaficantFigures) / signaficantFigures;
                m_MinDepthScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_MinDepthStep.value, 2f))));
            }
            else if (tempValue < 1f)
            {
                if (tempValue == 0.01f && m_MinDepthStep.value == 1f)
                {
                    tempValue = 0.1f;
                    m_MinDepthScale.Update(1);
                }
                else
                {
                    tempValue += 0.1f * m_MinDepthStep.value;
                    tempValue = Mathf.Round(tempValue * 10f * signaficantFigures) / (10f * signaficantFigures);
                    m_MinDepthScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_MinDepthStep.value, 2f)) + 1));
                }
            }

            tempValue = Mathf.Clamp(tempValue, 0.01f, 1000f);

            if (tempValue > m_Amount.value)
            {
                // This updates the binding with the new value so that max Depth is not smaller than min depth.
                m_Amount.Update(tempValue);
                m_AmountScale.Update(m_MinDepthScale.value);
            }

            // This updates the binding with the new value after all changes have occured.
            m_MinDepth.Update(tempValue);
        }

        private void DecreaseMinDepth()
        {
            float signaficantFigures = Mathf.Pow(10f, -1f * Mathf.Log(m_MinDepthStep.value, 2f));
            float tempValue = m_MinDepth.value;
            if (tempValue <= 1f)
            {
                tempValue -= 0.1f * m_MinDepthStep.value;
                tempValue = Mathf.Round(tempValue * 10f * signaficantFigures) / (10f * signaficantFigures);
                m_MinDepthScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_MinDepthStep.value, 2f)) + 1));
            }
            else if (tempValue <= 10f && tempValue > 1f)
            {
                tempValue -= 1f * m_MinDepthStep.value;
                tempValue = Mathf.Round(tempValue * signaficantFigures) / signaficantFigures;
                m_MinDepthScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_MinDepthStep.value, 2f))));
            }
            else if (tempValue <= 100f && tempValue > 10f)
            {
                tempValue -= 10f * m_MinDepthStep.value;
                tempValue = Mathf.Round(tempValue * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                m_MinDepthScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_MinDepthStep.value, 2f)) - 1));
            }
            else if (tempValue <= 500f && tempValue > 100f)
            {
                tempValue -= 50f * m_MinDepthStep.value;
                tempValue = Mathf.Round(tempValue * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                m_MinDepthScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_MinDepthStep.value, 2f)) - 1));
            }
            else if (tempValue > 500f)
            {
                tempValue -= 100f * m_MinDepthStep.value;
                tempValue = Mathf.Round(tempValue * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
                m_MinDepthScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_MinDepthStep.value, 2f)) - 2));
            }

            tempValue = Mathf.Clamp(tempValue, 0.01f, 1000f);

            if (tempValue == 0.01f)
            {
                m_MinDepthScale.Update(2);
            }

            // This updates the binding with the new value after all changes have occured.
            m_MinDepth.Update(tempValue);
        }

        private void IncreaseAmount()
        {
            float signaficantFigures = Mathf.Pow(10f, -1f * Mathf.Log(m_AmountStep.value, 2f));
            float tempValue = m_Amount.value;
            if (!m_AmountIsElevation.value)
            {
                if (tempValue >= 500f && tempValue < 1000f)
                {
                    tempValue += 100f * m_AmountStep.value;
                    tempValue = Mathf.Round(tempValue * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
                    m_AmountScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_AmountStep.value, 2f)) - 2));
                }
                else if (tempValue >= 100f && tempValue < 500f)
                {
                    tempValue += 50f * m_AmountStep.value;
                    tempValue = Mathf.Round(tempValue * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                    m_AmountScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_AmountStep.value, 2f)) - 1));
                }
                else if (tempValue < 100f && tempValue >= 10f)
                {
                    tempValue += 10f * m_AmountStep.value;
                    tempValue = Mathf.Round(tempValue * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                    m_AmountScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_AmountStep.value, 2f)) - 1));
                }
                else if (tempValue < 10f && tempValue >= 1f)
                {
                    tempValue += 1f * m_AmountStep.value;
                    tempValue = Mathf.Round(tempValue * signaficantFigures) / signaficantFigures;
                    m_AmountScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_AmountStep.value, 2f))));
                }
                else if (tempValue < 1f)
                {
                    if (tempValue == 0.01f && m_AmountStep.value == 1f)
                    {
                        tempValue = 0.1f;
                        m_AmountScale.Update(1);
                    }
                    else
                    {
                        tempValue += 0.1f * m_AmountStep.value;
                        tempValue = Mathf.Round(tempValue * 10f * signaficantFigures) / (10f * signaficantFigures);
                        m_AmountScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_AmountStep.value, 2f)) + 1));
                    }
                }

                tempValue = Mathf.Clamp(tempValue, 0.01f, 1000f);
            }
            else
            {
                tempValue += 10f * m_AmountStep.value;
                tempValue = Mathf.Round(tempValue * 10f) / 10f;
                m_AmountScale.Update(1);
                tempValue = Mathf.Clamp(tempValue, m_TerrainSystem.GetTerrainBounds().min.y, m_TerrainSystem.GetTerrainBounds().max.y);
            }

            // This updates the binding with the new value after all changes have occured.
            m_Amount.Update(tempValue);
        }

        private void DecreaseAmount()
        {
            float signaficantFigures = Mathf.Pow(10f, -1f * Mathf.Log(m_AmountStep.value, 2f));
            float tempValue = m_Amount.value;
            if (!m_AmountIsElevation.value)
            {
                if (tempValue <= 1f)
                {
                    tempValue -= 0.1f * m_AmountStep.value;
                    tempValue = Mathf.Round(tempValue * 10f * signaficantFigures) / (10f * signaficantFigures);
                    m_AmountScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_AmountStep.value, 2f)) + 1));
                }
                else if (tempValue <= 10f && tempValue > 1f)
                {
                    tempValue -= 1f * m_AmountStep.value;
                    tempValue = Mathf.Round(tempValue * signaficantFigures) / signaficantFigures;
                    m_AmountScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_AmountStep.value, 2f))));
                }
                else if (tempValue <= 100f && tempValue > 10f)
                {
                    tempValue -= 10f * m_AmountStep.value;
                    tempValue = Mathf.Round(tempValue * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                    m_AmountScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_AmountStep.value, 2f)) - 1));
                }
                else if (tempValue <= 500f && tempValue > 100f)
                {
                    tempValue -= 50f * m_AmountStep.value;
                    tempValue = Mathf.Round(tempValue * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                    m_AmountScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_AmountStep.value, 2f)) - 1));
                }
                else if (tempValue > 500f)
                {
                    tempValue -= 100f * m_AmountStep.value;
                    tempValue = Mathf.Round(tempValue * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
                    m_AmountScale.Update(Math.Max(0, Mathf.RoundToInt(-1f * Mathf.Log(m_AmountStep.value, 2f)) - 2));
                }

                tempValue = Mathf.Clamp(tempValue, 0.01f, 1000f);
                if (tempValue == 0.01f)
                {
                    m_AmountScale.Update(2);
                }
            }
            else
            {
                tempValue -= 10f * m_AmountStep.value;
                tempValue = Mathf.Round(tempValue * 10f) / 10f;
                m_AmountScale.Update(1);
                tempValue = Mathf.Clamp(tempValue, m_TerrainSystem.GetTerrainBounds().min.y, m_TerrainSystem.GetTerrainBounds().max.y);
            }

            if (tempValue < m_MinDepth.value)
            {
                // This updates the binding with the new value so that max Depth is not smaller than min depth.
                m_MinDepth.Update(tempValue);
                m_MinDepthScale.Update(m_AmountScale.value);
            }

            // This updates the binding with the new value after all changes have occured.
            m_Amount.Update(tempValue);
        }

        private void RadiusStepPressed()
        {
            float tempValue = m_RadiusStep.value;
            tempValue /= 2f;
            if (tempValue < 0.125f)
            {
                tempValue = 1.0f;
            }

            m_RadiusStep.Update(tempValue);
        }

        private void AmountStepPressed()
        {
            float tempValue = m_AmountStep.value;
            tempValue /= 2f;
            if (tempValue < 0.125f)
            {
                tempValue = 1.0f;
            }

            m_AmountStep.Update(tempValue);
        }

        private void MinDepthStepPressed()
        {
            float tempValue = m_MinDepthStep.value;
            tempValue /= 2f;
            if (tempValue < 0.125f)
            {
                tempValue = 1.0f;
            }

            m_MinDepthStep.Update(tempValue);
        }

        private void ChangePrefab(string prefabName)
        {
            if (m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(WaterSourcePrefab), prefabName), out PrefabBase prefabBase))
            {
                if (m_ToolSystem.ActivatePrefabTool(prefabBase) || (m_ToolSystem.activeTool == m_CustomWaterToolSystem && prefabBase is WaterSourcePrefab))
                {
                    m_ActivePrefabName.Update(prefabName);
                }
            }
        }

        private void ChangeToolMode(int toolMode) => m_ToolMode.Update(toolMode);

        private void AmountIsElevationToggled()
        {
            m_AmountIsElevation.Update(!m_AmountIsElevation.value);
            PrefabBase prefab = m_CustomWaterToolSystem.GetPrefab();
            if (prefab is not null &&
                prefab is WaterSourcePrefab)
            {
                WaterSourcePrefab waterSourcePrefab = prefab as WaterSourcePrefab;
                float tempRadius = waterSourcePrefab.m_DefaultRadius;
                float tempAmount = waterSourcePrefab.m_DefaultHeight;
                TryGetDefaultValuesForWaterSource(waterSourcePrefab, ref tempAmount, ref tempRadius);
                m_Amount.Update(tempAmount);
                m_AmountLocaleKey.Update(waterSourcePrefab.m_HeightLocaleKey);
            }
        }

        private void OnPrefabChanged(PrefabBase prefabBase)
        {
            m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(OnPrefabChanged)}");
            if (prefabBase is WaterSourcePrefab)
            {
                m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(OnPrefabChanged)} prefab is water source.");
                WaterSourcePrefab waterSourcePrefab = prefabBase as WaterSourcePrefab;

                float tempRadius = waterSourcePrefab.m_DefaultRadius;
                float tempAmount = waterSourcePrefab.m_DefaultHeight;
                TryGetDefaultValuesForWaterSource(waterSourcePrefab, ref tempAmount, ref tempRadius);
                m_AmountIsElevation.Update(false);
                m_Radius.Update(tempRadius);
                m_Amount.Update(tempAmount);
                m_AmountScale.Update(Math.Max(0, CalculateScale(tempAmount)));
                m_RadiusScale.Update(Math.Max(0, CalculateScale(tempRadius)));
                m_AmountLocaleKey.Update(waterSourcePrefab.m_HeightLocaleKey);
                bool flag = waterSourcePrefab.m_SourceType == SourceType.RetentionBasin;
                if (m_ShowMinDepth.value != flag)
                {
                    m_ShowMinDepth.Update(flag);
                }

                if (flag)
                {
                    m_MinDepth.Update(tempAmount / 2f);
                    m_MinDepthScale.Update(Math.Max(0, CalculateScale(tempAmount / 2f)));
                }

                m_ToolMode.Update((int)CustomWaterToolSystem.ToolModes.PlaceWaterSource);
            }
        }

        private int CalculateScale(float value)
        {
            value = Mathf.Abs(value);
            m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(CalculateScale)} value = {value}");
            int afterTheDecimal = Mathf.FloorToInt(value * 10000);
            int significantFiguresBeforeTheDecimal = 0;
            if (value > 1f)
            {
                afterTheDecimal = Mathf.FloorToInt((value % Mathf.Floor(value)) * 10000);
                significantFiguresBeforeTheDecimal = Mathf.CeilToInt(Mathf.Log10(value));
            }

            m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(CalculateScale)} afterTheDecimal = {afterTheDecimal}");
            m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(CalculateScale)} significantFiguresBeforeTheDecimal = {significantFiguresBeforeTheDecimal}");
            int significantFiguresAfterTheDecimal = 0;
            for (int i = 10; i <= 10000; i *= 10)
            {
                if (Mathf.FloorToInt(afterTheDecimal % i) > 1)
                {
                    significantFiguresAfterTheDecimal++;
                }
            }

            m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(CalculateScale)} significantFiguresAfterTheDecimal = {significantFiguresAfterTheDecimal}");

            int maxSignificantFiguresAfterTheDecimal = Math.Max(4 - significantFiguresBeforeTheDecimal, 1);
            if (value > 100f && value <= 500f)
            {
                maxSignificantFiguresAfterTheDecimal++;
            }

            m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(CalculateScale)} maxSignificantFiguresAfterTheDecimal = {maxSignificantFiguresAfterTheDecimal}");

            m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(CalculateScale)} result = {Math.Min(significantFiguresAfterTheDecimal, maxSignificantFiguresAfterTheDecimal)}");

            return Math.Min(significantFiguresAfterTheDecimal, maxSignificantFiguresAfterTheDecimal);
        }

        /// <summary>
        /// Tries to deserialize an xml with the amount and radius information for a specific water source.
        /// </summary>
        /// <param name="waterSource">Generally the active prefab for custom water tool.</param>
        /// <param name="amount">The default amount will be changed if previous entry was serialized in xml.</param>
        /// <param name="radius">The default radius will be changed if previous entry was serialized in xml.</param>
        /// <returns>True if loaded from xml. False if nothing changed.</returns>
        private bool TryGetDefaultValuesForWaterSource(WaterSourcePrefab waterSource, ref float amount, ref float radius)
        {
            string fileName = Path.Combine(m_ContentFolder, $"{waterSource.m_SourceType}.xml");
            if (m_WaterSourcePrefabValuesRepositories.ContainsKey(waterSource))
            {
                amount = m_WaterSourcePrefabValuesRepositories[waterSource].Amount;
                radius = m_WaterSourcePrefabValuesRepositories[waterSource].Radius;
                m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(TryGetDefaultValuesForWaterSource)} found repository for {waterSource.m_SourceType}.");
                return true;
            }

            if (File.Exists(fileName))
            {
                try
                {
                    XmlSerializer serTool = new XmlSerializer(typeof(WaterSourcePrefabValuesRepository)); // Create serializer
                    using System.IO.FileStream readStream = new System.IO.FileStream(fileName, System.IO.FileMode.Open); // Open file
                    WaterSourcePrefabValuesRepository result = (WaterSourcePrefabValuesRepository)serTool.Deserialize(readStream); // Des-serialize to new Properties
                    if (result.Amount >= 0.125f && result.Amount <= 1000f)
                    {
                        amount = result.Amount;
                    }

                    if (result.Radius >= 5f && result.Radius <= 10000f)
                    {
                        radius = result.Radius;
                    }

                    if (!m_WaterSourcePrefabValuesRepositories.ContainsKey(waterSource))
                    {
                        m_WaterSourcePrefabValuesRepositories.Add(waterSource, result);
                        m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(TryGetDefaultValuesForWaterSource)} adding repository for {waterSource.m_SourceType}.");
                    }

                    m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(TryGetDefaultValuesForWaterSource)} loaded repository for {waterSource.m_SourceType}.");
                    return true;
                }
                catch (Exception ex)
                {
                    m_Log.Warn($"{nameof(WaterToolUISystem)}.{nameof(TryGetDefaultValuesForWaterSource)} Could not get default values for WaterSource {waterSource.m_SourceType}. Encountered exception {ex}");
                    return false;
                }
            }

            if (TrySaveDefaultValuesForWaterSource(waterSource, amount, radius))
            {
                m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(TryGetDefaultValuesForWaterSource)} Saved {waterSource.m_SourceType}'s default values because the file didn't exist.");
            }

            return false;
        }
    }
}
