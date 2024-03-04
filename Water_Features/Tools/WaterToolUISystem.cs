// <copyright file="WaterToolUISystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Tools
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;
    using Colossal.Logging;
    using Colossal.PSI.Environment;
    using Colossal.Serialization.Entities;
    using Colossal.UI.Binding;
    using Game;
    using Game.Prefabs;
    using Game.Simulation;
    using Game.Tools;
    using Game.UI;
    using Unity.Entities;
    using UnityEngine;
    using Water_Features;
    using Water_Features.Prefabs;
    using Water_Features.Settings;

    /// <summary>
    /// UI system for Custom Water Tool.
    /// </summary>
    public partial class WaterToolUISystem : UISystemBase
    {
        private ToolSystem m_ToolSystem;
        private CustomWaterToolSystem m_CustomWaterToolSystem;
        private TerrainSystem m_TerrainSystem;
        private ILog m_Log;
        private Dictionary<string, Action> m_ChangeValueActions;
        private bool m_ResetValues = true;
        private string m_ContentFolder;
        private Dictionary<WaterSourcePrefab, WaterSourcePrefabValuesRepository> m_WaterSourcePrefabValuesRepositories;
        private bool m_AmountIsElevation;
        private ValueBinding<float> m_Radius;
        private ValueBinding<float> m_Amount;
        private ValueBinding<float> m_MinDepth;
        private ValueBinding<string> m_AmountLocaleKey;
        private ValueBinding<float> m_RadiusStep;
        private ValueBinding<float> m_AmountStep;
        private ValueBinding<float> m_MinDepthStep;
        private ValueBinding<bool> m_ToolActive;

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
        }

        /// <summary>
        /// Gets the radius.
        /// </summary>
        public float Radius
        {
            get { return m_Radius.value; }
        }

        /// <summary>
        /// Gets the amount.
        /// </summary>
        public float Amount
        {
            get { return m_Amount.value; }
        }

        /// <summary>
        /// Gets the min depth.
        /// </summary>
        public float MinDepth
        {
            get { return m_MinDepth.value; }
        }

        /// <summary>
        /// Gets a value indicating whether the amount is an elevation.
        /// </summary>
        public bool AmountIsAnElevation
        {
            get { return m_AmountIsElevation; }
        }

        /// <summary>
        /// Sets the amount value equal to elevation parameter. And sets the label for that row to Elevation.
        /// </summary>
        /// <param name="elevation">The y coordinate from the raycast hit position.</param>
        public void SetElevation(float elevation)
        {
            elevation = Mathf.Round(elevation * 10f) / 10f;
            elevation = Mathf.Clamp(elevation, m_TerrainSystem.GetTerrainBounds().min.y, m_TerrainSystem.GetTerrainBounds().max.y);
            m_Amount.Update(elevation);
            m_AmountIsElevation = true;
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

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = WaterFeaturesMod.Instance.Log;
            m_ToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            m_CustomWaterToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<CustomWaterToolSystem>();
            m_TerrainSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TerrainSystem>();
            ToolSystem toolSystem = m_ToolSystem; // I don't know why vanilla game did this.
            m_ToolSystem.EventToolChanged = (Action<ToolBaseSystem>)Delegate.Combine(toolSystem.EventToolChanged, new Action<ToolBaseSystem>(OnToolChanged));
            ToolSystem toolSystem2 = m_ToolSystem; // I don't know why vanilla game did this.
            m_ToolSystem.EventPrefabChanged = (Action<PrefabBase>)Delegate.Combine(toolSystem2.EventPrefabChanged, new Action<PrefabBase>(OnPrefabChanged));
            m_ContentFolder = Path.Combine(EnvPath.kUserDataPath, "ModsData", "Mods_Yenyang_Water_Features");
            Directory.CreateDirectory(m_ContentFolder);

            // This binding communicates whether Water Tool is active.
            AddBinding(m_ToolActive = new ValueBinding<bool>("WaterTool", "ToolActive", false));

            // This binding communicates the value for Amount.
            AddBinding(m_Amount = new ValueBinding<float>("WaterTool", "AmountValue", 1f));

            // This binding communicates the value for Radius.
            AddBinding(m_Radius = new ValueBinding<float>("WaterTool", "RadiusValue", 5f));

            // This binding communicates the value for Min Depth.
            AddBinding(m_MinDepth = new ValueBinding<float>("WaterTool", "MinDepthValue", 10f));

            // This binding communicates the Locale Key for the Amount section.
            AddBinding(m_AmountLocaleKey = new ValueBinding<string>("WaterTool", "AmountLocaleKey", "YY_WATER_FEATURES.Depth"));

            // This binding communicates the value of the selected Radius Step.
            AddBinding(m_RadiusStep = new ValueBinding<float>("WaterTool", "RadiusStep", 1f));

            // This binding communicates the value of the selected Amount Step.
            AddBinding(m_AmountStep = new ValueBinding<float>("WaterTool", "AmountStep", 1f));

            // This binding communicates the value of the selected Min Depth step.
            AddBinding(m_MinDepthStep = new ValueBinding<float>("WaterTool", "MinDepthStep", 1f));

            // This binding listens for whether the Increase Amount button was clicked.
            AddBinding(new TriggerBinding("WaterTool", "IncreaseAmount", IncreaseAmount));

            // This binding listens for whether the Decrease Amount button was clicked.
            AddBinding(new TriggerBinding("WaterTool", "DecreaseAmount", DecreaseAmount));

            // This binding listens for whether the IncreaseMinDepth button was clicked.
            AddBinding(new TriggerBinding("WaterTool", "IncreaseMinDepth", IncreaseMinDepth));

            // This binding listens for whether the DecreaseMinDepth button was clicked.
            AddBinding(new TriggerBinding("WaterTool", "DecreaseMinDepth", DecreaseMinDepth));

            // This binding listens for whether the IncreaseRadius button was clicked.
            AddBinding(new TriggerBinding("WaterTool", "IncreaseRadius", IncreaseRadius));

            // This binding listens for whether the DecreaseRadius button was clicked.
            AddBinding(new TriggerBinding("WaterTool", "DecreaseRadius", DecreaseRadius));

            // This binding listens for whether the AmountStepPressed button was clicked.
            AddBinding(new TriggerBinding("WaterTool", "AmountStepPressed", AmountStepPressed));

            // This binding listens for whether the MinDepthStepPressed button was clicked.
            AddBinding(new TriggerBinding("WaterTool", "MinDepthStepPressed", MinDepthStepPressed));

            // This binding listens for whether the RadiusStepPressed button was clicked.
            AddBinding(new TriggerBinding("WaterTool", "RadiusStepPressed", RadiusStepPressed));

            m_WaterSourcePrefabValuesRepositories = new Dictionary<WaterSourcePrefab, WaterSourcePrefabValuesRepository>();
        }


        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
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
            }
            else if (tempRadius >= 500f && tempRadius < 1000f)
            {
                tempRadius += 100f * m_RadiusStep.value;
                tempRadius = Mathf.Round(tempRadius * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
            }
            else if (tempRadius >= 100f && tempRadius < 500f)
            {
                tempRadius += 50f * m_RadiusStep.value;
                tempRadius = Mathf.Round(tempRadius * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (tempRadius >= 10f && tempRadius < 100f)
            {
                tempRadius += 10f * m_RadiusStep.value;
                tempRadius = Mathf.Round(tempRadius * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (tempRadius < 10000)
            {
               tempRadius += 1f * m_RadiusStep.value;
               tempRadius = Mathf.Round(tempRadius * signaficantFigures) / signaficantFigures;
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
            }
            else if (tempRadius <= 100f && tempRadius > 10f)
            {
                tempRadius -= 10f * m_RadiusStep.value;
                tempRadius = Mathf.Round(tempRadius * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (tempRadius <= 500f && tempRadius > 100f)
            {
                tempRadius -= 50f * m_RadiusStep.value;
                tempRadius = Mathf.Round(tempRadius * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (tempRadius <= 1000f && tempRadius > 500f)
            {
                tempRadius -= 100f * m_RadiusStep.value;
                tempRadius = Mathf.Round(tempRadius * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
            }
            else if (tempRadius > 1000f)
            {
                tempRadius -= 500f * m_RadiusStep.value;
                tempRadius = Mathf.Round(tempRadius * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
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
            }
            else if (tempValue >= 100f && tempValue < 500f)
            {
                tempValue += 50f * m_MinDepthStep.value;
                tempValue = Mathf.Round(tempValue * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (tempValue < 100f && tempValue >= 10f)
            {
                tempValue += 10f * m_MinDepthStep.value;
                tempValue = Mathf.Round(tempValue * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (tempValue < 10f && tempValue >= 1f)
            {
                tempValue += 1f * m_MinDepthStep.value;
                tempValue = Mathf.Round(tempValue * signaficantFigures) / signaficantFigures;
            }
            else if (tempValue < 1f)
            {
                if (tempValue == 0.01f && m_MinDepthStep.value == 1f)
                {
                    tempValue = 0.1f;
                }
                else
                {
                    tempValue += 0.1f * m_MinDepthStep.value;
                    tempValue = Mathf.Round(tempValue * 10f * signaficantFigures) / (10f * signaficantFigures);
                }
            }

            tempValue = Mathf.Clamp(tempValue, 0.01f, 1000f);

            if (tempValue > m_Amount.value)
            {
                // This updates the binding with the new value so that max Depth is not smaller than min depth.
                m_Amount.Update(tempValue);
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
            }
            else if (tempValue <= 10f && tempValue > 1f)
            {
                tempValue -= 1f * m_MinDepthStep.value;
                tempValue = Mathf.Round(tempValue * signaficantFigures) / signaficantFigures;
            }
            else if (tempValue <= 100f && tempValue > 10f)
            {
                tempValue -= 10f * m_MinDepthStep.value;
                tempValue = Mathf.Round(tempValue * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (tempValue <= 500f && tempValue > 100f)
            {
                tempValue -= 50f * m_MinDepthStep.value;
                tempValue = Mathf.Round(tempValue * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (tempValue > 500f)
            {
                tempValue -= 100f * m_MinDepthStep.value;
                tempValue = Mathf.Round(tempValue * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
            }

            tempValue = Mathf.Clamp(tempValue, 0.01f, 1000f);

            // This updates the binding with the new value after all changes have occured.
            m_MinDepth.Update(tempValue);
        }

        private void IncreaseAmount()
        {
            float signaficantFigures = Mathf.Pow(10f, -1f * Mathf.Log(m_AmountStep.value, 2f));
            float tempValue = m_Amount.value;
            if (!m_AmountIsElevation)
            {
                if (tempValue >= 500f && tempValue < 1000f)
                {
                    tempValue += 100f * m_AmountStep.value;
                    tempValue = Mathf.Round(tempValue * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
                }
                else if (tempValue >= 100f && tempValue < 500f)
                {
                    tempValue += 50f * m_AmountStep.value;
                    tempValue = Mathf.Round(tempValue * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                }
                else if (tempValue < 100f && tempValue >= 10f)
                {
                    tempValue += 10f * m_AmountStep.value;
                    tempValue = Mathf.Round(tempValue * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                }
                else if (tempValue < 10f && tempValue >= 1f)
                {
                    tempValue += 1f * m_AmountStep.value;
                    tempValue = Mathf.Round(tempValue * signaficantFigures) / signaficantFigures;
                }
                else if (tempValue < 1f)
                {
                    if (tempValue == 0.01f && m_AmountStep.value == 1f)
                    {
                        tempValue = 0.1f;
                    }
                    else
                    {
                        tempValue += 0.1f * m_AmountStep.value;
                        tempValue = Mathf.Round(tempValue * 10f * signaficantFigures) / (10f * signaficantFigures);
                    }
                }

                tempValue = Mathf.Clamp(tempValue, 0.01f, 1000f);
            }
            else
            {
                tempValue += 10f * m_AmountStep.value;
                tempValue = Mathf.Round(tempValue * 10f) / 10f;
                tempValue = Mathf.Clamp(tempValue, m_TerrainSystem.GetTerrainBounds().min.y, m_TerrainSystem.GetTerrainBounds().max.y);
            }

            // This updates the binding with the new value after all changes have occured.
            m_Amount.Update(tempValue);
        }

        private void DecreaseAmount()
        {
            float signaficantFigures = Mathf.Pow(10f, -1f * Mathf.Log(m_AmountStep.value, 2f));
            float tempValue = m_Amount.value;
            if (!m_AmountIsElevation)
            {
                if (tempValue <= 1f)
                {
                    tempValue -= 0.1f * m_AmountStep.value;
                    tempValue = Mathf.Round(tempValue * 10f * signaficantFigures) / (10f * signaficantFigures);
                }
                else if (tempValue <= 10f && tempValue > 1f)
                {
                    tempValue -= 1f * m_AmountStep.value;
                    tempValue = Mathf.Round(tempValue * signaficantFigures) / signaficantFigures;
                }
                else if (tempValue <= 100f && tempValue > 10f)
                {
                    tempValue -= 10f * m_AmountStep.value;
                    tempValue = Mathf.Round(tempValue * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                }
                else if (tempValue <= 500f && tempValue > 100f)
                {
                    tempValue -= 50f * m_AmountStep.value;
                    tempValue = Mathf.Round(tempValue * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                }
                else if (tempValue > 500f)
                {
                    tempValue -= 100f * m_AmountStep.value;
                    tempValue = Mathf.Round(tempValue * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
                }

                tempValue = Mathf.Clamp(tempValue, 0.01f, 1000f);
            }
            else
            {
                tempValue -= 10f * m_AmountStep.value;
                tempValue = Mathf.Round(tempValue * 10f) / 10f;
                tempValue = Mathf.Clamp(tempValue, m_TerrainSystem.GetTerrainBounds().min.y, m_TerrainSystem.GetTerrainBounds().max.y);
            }

            if (tempValue < m_MinDepth.value)
            {
                // This updates the binding with the new value so that max Depth is not smaller than min depth.
                m_MinDepth.Update(tempValue);
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
            float tempValue = m_MinDepth.value;
            tempValue /= 2f;
            if (tempValue < 0.125f)
            {
                tempValue = 1.0f;
            }

            m_MinDepth.Update(tempValue);
        }

        private void OnToolChanged(ToolBaseSystem tool)
        {
            bool flag = tool == m_CustomWaterToolSystem;
            m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(OnToolChanged)}");
            if (m_ToolActive.value != flag)
            {
                m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(OnToolChanged)} tool active.");
                m_ToolActive.Update(flag);
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
                float tempAmount = waterSourcePrefab.m_DefaultAmount;
                TryGetDefaultValuesForWaterSource(waterSourcePrefab, ref tempAmount, ref tempRadius);
                m_AmountIsElevation = false;
                m_Radius.Update(tempRadius);
                m_Amount.Update(tempAmount);
                m_AmountLocaleKey.Update(waterSourcePrefab.m_AmountLocaleKey);
            }
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
