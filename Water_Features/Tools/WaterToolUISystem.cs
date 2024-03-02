// <copyright file="WaterToolUISystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Tools
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;
    using cohtml.Net;
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
        private ValueBinding<int> m_RadiusStep;
        private ValueBinding<int> m_AmountStep;
        private ValueBinding<int> m_MinDepthStep;

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
            m_Amount.Update(Mathf.Round(elevation * 10f) / 10f);
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
            
            m_ChangeValueActions = new Dictionary<string, Action>()
            {
                { "YYWT-amount-down-arrow", (Action)DecreaseAmount },
                { "YYWT-amount-up-arrow", (Action)IncreaseAmount },
                { "YYWT-radius-down-arrow", (Action)DecreaseRadius },
                { "YYWT-radius-up-arrow", (Action)IncreaseRadius },
                { "YYWT-min-depth-down-arrow", (Action)DecreaseMinDepth },
                { "YYWT-min-depth-up-arrow", (Action)IncreaseMinDepth },
                { "YYWT-amount-rate-of-change", (Action)AmountRateOfChangePressed },
                { "YYWT-radius-rate-of-change", (Action)RadiusRateOfChangePressed },
                { "YYWT-min-depth-rate-of-change", (Action)MinDepthRateOfChangePressed },
            };

            m_WaterSourcePrefabValuesRepositories = new Dictionary<WaterSourcePrefab, WaterSourcePrefabValuesRepository>();
            base.OnCreate();
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
            float radiusRateOfChange = 1.0f; // Need a method or equation to convert from step int.
            float signaficantFigures = Mathf.Pow(10f, -1f * Mathf.Log(radiusRateOfChange, 2f));
            float tempRadius = m_Radius.value;
            if (tempRadius >= 1000f && tempRadius < 10000f)
            {
                tempRadius += 500f * radiusRateOfChange;
                tempRadius = Mathf.Round(tempRadius * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
            }
            else if (tempRadius >= 500f && tempRadius < 1000f)
            {
                tempRadius += 100f * radiusRateOfChange;
                tempRadius = Mathf.Round(tempRadius * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
            }
            else if (tempRadius >= 100f && tempRadius < 500f)
            {
                tempRadius += 50f * radiusRateOfChange;
                tempRadius = Mathf.Round(tempRadius * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (tempRadius >= 10f && tempRadius < 100f)
            {
                tempRadius += 10f * radiusRateOfChange;
                tempRadius = Mathf.Round(tempRadius * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (tempRadius < 10000)
            {
               tempRadius += 1f * radiusRateOfChange;
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
            float signaficantFigures = Mathf.Pow(10f, -1f * Mathf.Log(radiusRateOfChange, 2f));
            if (m_Radius <= 10f && m_Radius > 1f)
            {
                m_Radius -= 1f * radiusRateOfChange;
                m_Radius = Mathf.Round(m_Radius * signaficantFigures) / signaficantFigures;
            }
            else if (m_Radius <= 100f && m_Radius > 10f)
            {
                m_Radius -= 10f * radiusRateOfChange;
                m_Radius = Mathf.Round(m_Radius * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (m_Radius <= 500f && m_Radius > 100f)
            {
                m_Radius -= 50f * radiusRateOfChange;
                m_Radius = Mathf.Round(m_Radius * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (m_Radius <= 1000f && m_Radius > 500f)
            {
                m_Radius -= 100f * radiusRateOfChange;
                m_Radius = Mathf.Round(m_Radius * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
            }
            else if (m_Radius > 1000f)
            {
                m_Radius -= 500f * radiusRateOfChange;
                m_Radius = Mathf.Round(m_Radius * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
            }

            if (WaterFeaturesMod.Instance.Settings.TrySmallerRadii)
            {
                m_Radius = Mathf.Clamp(m_Radius, 1f, 10000f);
            }
            else
            {
                m_Radius = Mathf.Clamp(m_Radius, 5f, 10000f);
            }

            // This script sets the radius field to the desired radius;
            UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.radiusField = document.getElementById(\"YYWT-radius-field\"); if (yyWaterTool.radiusField) yyWaterTool.radiusField.innerHTML = \"{m_Radius} m\";");
        }

        private void IncreaseMinDepth()
        {
            float signaficantFigures = Mathf.Pow(10f, -1f * Mathf.Log(m_MinDepthRateOfChange, 2f));
            if (m_MinDepth >= 500f && m_MinDepth < 1000f)
            {
                m_MinDepth += 100f * m_MinDepthRateOfChange;
                m_MinDepth = Mathf.Round(m_MinDepth * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
            }
            else if (m_MinDepth >= 100f && m_MinDepth < 500f)
            {
                m_MinDepth += 50f * m_MinDepthRateOfChange;
                m_MinDepth = Mathf.Round(m_MinDepth * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (m_MinDepth < 100f && m_MinDepth >= 10f)
            {
                m_MinDepth += 10f * m_MinDepthRateOfChange;
                m_MinDepth = Mathf.Round(m_MinDepth * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (m_MinDepth < 10f && m_MinDepth >= 1f)
            {
                m_MinDepth += 1f * m_MinDepthRateOfChange;
                m_MinDepth = Mathf.Round(m_MinDepth * signaficantFigures) / signaficantFigures;
            }
            else if (m_MinDepth < 1f)
            {
                if (m_MinDepth == 0.01f && m_MinDepthRateOfChange == 1f)
                {
                    m_MinDepth = 0.1f;
                }
                else
                {
                    m_MinDepth += 0.1f * m_MinDepthRateOfChange;
                    m_MinDepth = Mathf.Round(m_MinDepth * 10f * signaficantFigures) / (10f * signaficantFigures);
                }
            }

            m_MinDepth = Mathf.Clamp(m_MinDepth, 0.01f, 1000f);

            if (m_MinDepth > m_Amount)
            {
                m_Amount = m_MinDepth;

                string unit = " m";

                if (m_CustomWaterToolSystem.GetPrefab() != null)
                {
                    WaterSourcePrefab waterSourcePrefab = m_CustomWaterToolSystem.GetPrefab() as WaterSourcePrefab;
                    if (waterSourcePrefab.m_SourceType == SourceType.Stream)
                    {
                        unit = string.Empty;
                    }
                }

                // This script sets the amount field to the desired amount.
                UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.amountField = document.getElementById(\"YYWT-amount-field\"); if (yyWaterTool.amountField) yyWaterTool.amountField.innerHTML = \"{m_Amount}{unit}\";");
            }

            // This script sets the min depth field to the desired min depth;
            UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.minDepthField = document.getElementById(\"YYWT-min-depth-field\"); if (yyWaterTool.minDepthField) yyWaterTool.minDepthField.innerHTML = \"{m_MinDepth} m\";");
        }

        private void DecreaseMinDepth()
        {
            float signaficantFigures = Mathf.Pow(10f, -1f * Mathf.Log(m_MinDepthRateOfChange, 2f));
            if (m_MinDepth <= 1f)
            {
                m_MinDepth -= 0.1f * m_MinDepthRateOfChange;
                m_MinDepth = Mathf.Round(m_MinDepth * 10f * signaficantFigures) / (10f * signaficantFigures);
            }
            else if (m_MinDepth <= 10f && m_MinDepth > 1f)
            {
                m_MinDepth -= 1f * m_MinDepthRateOfChange;
                m_MinDepth = Mathf.Round(m_MinDepth * signaficantFigures) / signaficantFigures;
            }
            else if (m_MinDepth <= 100f && m_MinDepth > 10f)
            {
                m_MinDepth -= 10f * m_MinDepthRateOfChange;
                m_MinDepth = Mathf.Round(m_MinDepth * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (m_MinDepth <= 500f && m_MinDepth > 100f)
            {
                m_MinDepth -= 50f * m_MinDepthRateOfChange;
                m_MinDepth = Mathf.Round(m_MinDepth * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (m_MinDepth > 500f)
            {
                m_MinDepth -= 100f * m_MinDepthRateOfChange;
                m_MinDepth = Mathf.Round(m_MinDepth * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
            }

            m_MinDepth = Mathf.Clamp(m_MinDepth, 0.01f, 1000f);

            // This script sets the radius field to the desired radius;
            UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.minDepthField = document.getElementById(\"YYWT-min-depth-field\"); if (yyWaterTool.minDepthField) yyWaterTool.minDepthField.innerHTML = \"{m_MinDepth} m\";");
        }

        private void IncreaseAmount()
        {
            float signaficantFigures = Mathf.Pow(10f, -1f * Mathf.Log(m_AmountRateOfChange, 2f));
            if (!m_AmountIsElevation)
            {
                if (m_Amount >= 500f && m_Amount < 1000f)
                {
                    m_Amount += 100f * m_AmountRateOfChange;
                    m_Amount = Mathf.Round(m_Amount * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
                }
                else if (m_Amount >= 100f && m_Amount < 500f)
                {
                    m_Amount += 50f * m_AmountRateOfChange;
                    m_Amount = Mathf.Round(m_Amount * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                }
                else if (m_Amount < 100f && m_Amount >= 10f)
                {
                    m_Amount += 10f * m_AmountRateOfChange;
                    m_Amount = Mathf.Round(m_Amount * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                }
                else if (m_Amount < 10f && m_Amount >= 1f)
                {
                    m_Amount += 1f * m_AmountRateOfChange;
                    m_Amount = Mathf.Round(m_Amount * signaficantFigures) / signaficantFigures;
                }
                else if (m_Amount < 1f)
                {
                    if (m_Amount == 0.01f && m_AmountRateOfChange == 1f)
                    {
                        m_Amount = 0.1f;
                    }
                    else
                    {
                        m_Amount += 0.1f * m_AmountRateOfChange;
                        m_Amount = Mathf.Round(m_Amount * 10f * signaficantFigures) / (10f * signaficantFigures);
                    }
                }

                m_Amount = Mathf.Clamp(m_Amount, 0.01f, 1000f);
            }
            else
            {
                m_Amount += 10f * m_AmountRateOfChange;
                m_Amount = Mathf.Round(m_Amount * 10f) / 10f;
                m_Amount = Mathf.Clamp(m_Amount, m_TerrainSystem.GetTerrainBounds().min.y, m_TerrainSystem.GetTerrainBounds().max.y);
            }

            string unit = " m";

            if (m_CustomWaterToolSystem.GetPrefab() != null)
            {
                WaterSourcePrefab waterSourcePrefab = m_CustomWaterToolSystem.GetPrefab() as WaterSourcePrefab;
                if (waterSourcePrefab.m_SourceType == SourceType.Stream)
                {
                    unit = string.Empty;
                }
            }

            // This script sets the amount field to the desired amount;
            UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.amountField = document.getElementById(\"YYWT-amount-field\"); if (yyWaterTool.amountField) yyWaterTool.amountField.innerHTML = \"{m_Amount}{unit}\";");
        }

        private void DecreaseAmount()
        {
            float signaficantFigures = Mathf.Pow(10f, -1f * Mathf.Log(m_AmountRateOfChange, 2f));
            if (!m_AmountIsElevation)
            {
                if (m_Amount <= 1f)
                {
                    m_Amount -= 0.1f * m_AmountRateOfChange;
                    m_Amount = Mathf.Round(m_Amount * 10f * signaficantFigures) / (10f * signaficantFigures);
                }
                else if (m_Amount <= 10f && m_Amount > 1f)
                {
                    m_Amount -= 1f * m_AmountRateOfChange;
                    m_Amount = Mathf.Round(m_Amount * signaficantFigures) / signaficantFigures;
                }
                else if (m_Amount <= 100f && m_Amount > 10f)
                {
                    m_Amount -= 10f * m_AmountRateOfChange;
                    m_Amount = Mathf.Round(m_Amount * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                }
                else if (m_Amount <= 500f && m_Amount > 100f)
                {
                    m_Amount -= 50f * m_AmountRateOfChange;
                    m_Amount = Mathf.Round(m_Amount * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
                }
                else if (m_Amount > 500f)
                {
                    m_Amount -= 100f * m_AmountRateOfChange;
                    m_Amount = Mathf.Round(m_Amount * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
                }

                m_Amount = Mathf.Clamp(m_Amount, 0.01f, 1000f);
            }
            else
            {
                m_Amount -= 10f * m_AmountRateOfChange;
                m_Amount = Mathf.Round(m_Amount * 10f) / 10f;
                m_Amount = Mathf.Clamp(m_Amount, m_TerrainSystem.GetTerrainBounds().min.y, m_TerrainSystem.GetTerrainBounds().max.y);
            }

            if (m_Amount < m_MinDepth)
            {
                m_MinDepth = m_Amount;

                // This script sets the min depth field to the desired min depth;
                UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.minDepthField = document.getElementById(\"YYWT-min-depth-field\"); if (yyWaterTool.minDepthField) yyWaterTool.minDepthField.innerHTML = \"{m_MinDepth} m\";");
            }

            string unit = " m";

            if (m_CustomWaterToolSystem.GetPrefab() != null)
            {
                WaterSourcePrefab waterSourcePrefab = m_CustomWaterToolSystem.GetPrefab() as WaterSourcePrefab;
                if (waterSourcePrefab.m_SourceType == SourceType.Stream)
                {
                    unit = string.Empty;
                }
            }

            // This script sets the amount field to the desired amount;
            UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.amountField = document.getElementById(\"YYWT-amount-field\"); if (yyWaterTool.amountField) yyWaterTool.amountField.innerHTML = \"{m_Amount}{unit}\";");
        }

        private void RadiusRateOfChangePressed()
        {
            radiusRateOfChange /= 2f;
            if (radiusRateOfChange < 0.125f)
            {
                radiusRateOfChange = 1.0f;
            }

            SetRateIcon(radiusRateOfChange, "radius");
        }

        private void AmountRateOfChangePressed()
        {
            m_AmountRateOfChange /= 2f;
            if (m_AmountRateOfChange < 0.125f)
            {
                m_AmountRateOfChange = 1.0f;
            }

            SetRateIcon(m_AmountRateOfChange, "amount");
        }

        private void MinDepthRateOfChangePressed()
        {
            m_MinDepthRateOfChange /= 2f;
            if (m_MinDepthRateOfChange < 0.125f)
            {
                m_MinDepthRateOfChange = 1.0f;
            }

            SetRateIcon(m_MinDepthRateOfChange, "min-depth");
        }

        private void SetRateIcon(float field, string id)
        {
            if (field == 1f)
            {
                // This script changes the fill color of one of the rate of change indicators.
                UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.rateOfChange = document.getElementById(\"YYWT-{id}-roc-1\"); if (yyWaterTool.rateOfChange) yyWaterTool.rateOfChange.setAttribute(\"fill\",\"#1e83aa\");");

                // This script changes the fill color of one of the rate of change indicators.
                UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.rateOfChange = document.getElementById(\"YYWT-{id}-roc-0pt5\"); if (yyWaterTool.rateOfChange) yyWaterTool.rateOfChange.setAttribute(\"fill\",\"#1e83aa\");");

                // This script changes the fill color of one of the rate of change indicators.
                UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.rateOfChange = document.getElementById(\"YYWT-{id}-roc-0pt25\"); if (yyWaterTool.rateOfChange) yyWaterTool.rateOfChange.setAttribute(\"fill\",\"#1e83aa\");");
            }
            else if (field == 0.5f)
            {
                // This script changes the fill color of one of the rate of change indicators.
                UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.rateOfChange = document.getElementById(\"YYWT-{id}-roc-1\"); if (yyWaterTool.rateOfChange) yyWaterTool.rateOfChange.setAttribute(\"fill\",\"#424242\");");
            }
            else if (field == 0.25f)
            {
                // This script changes the fill color of one of the rate of change indicators.
                UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.rateOfChange = document.getElementById(\"YYWT-{id}-roc-1\"); if (yyWaterTool.rateOfChange) yyWaterTool.rateOfChange.setAttribute(\"fill\",\"#424242\");");

                // This script changes the fill color of one of the rate of change indicators.
                UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.rateOfChange = document.getElementById(\"YYWT-{id}-roc-0pt5\"); if (yyWaterTool.rateOfChange) yyWaterTool.rateOfChange.setAttribute(\"fill\",\"#424242\");");
            }
            else
            {
                // This script changes the fill color of one of the rate of change indicators.
                UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.rateOfChange = document.getElementById(\"YYWT-{id}-roc-1\"); if (yyWaterTool.rateOfChange) yyWaterTool.rateOfChange.setAttribute(\"fill\",\"#424242\");");

                // This script changes the fill color of one of the rate of change indicators.
                UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.rateOfChange = document.getElementById(\"YYWT-{id}-roc-0pt5\"); if (yyWaterTool.rateOfChange) yyWaterTool.rateOfChange.setAttribute(\"fill\",\"#424242\");");

                // This script changes the fill color of one of the rate of change indicators.
                UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.rateOfChange = document.getElementById(\"YYWT-{id}-roc-0pt25\"); if (yyWaterTool.rateOfChange) yyWaterTool.rateOfChange.setAttribute(\"fill\",\"#424242\");");
            }
        }



        /// <summary>
        /// C# event handler for event callback from UI JavaScript. If element YYWT-amount-item is found then set value to flag.
        /// </summary>
        /// <param name="flag">A bool for whether to element was found.</param>
        private void ElementCheck(bool flag) => m_WaterToolPanelShown = flag;

        /// <summary>
        /// Handles cleaning up after the icons are no longer needed.
        /// </summary>
        private void UnshowWaterToolPanel()
        {
            if (m_UiView == null)
            {
                return;
            }

            // This script destroys the amount item if it exists.
            UIFileUtils.ExecuteScript(m_UiView, DestroyElementByID("YYWT-amount-item"));

            // This script destroys the radius item if it exists.
            UIFileUtils.ExecuteScript(m_UiView, DestroyElementByID("YYWT-radius-item"));

            // This script destroys the min depth item if it exists.
            UIFileUtils.ExecuteScript(m_UiView, DestroyElementByID("YYWT-min-depth-item"));

            // This unregisters the events.
            foreach (BoundEventHandle eventHandle in m_BoundEventHandles)
            {
                m_UiView.UnregisterFromEvent(eventHandle);
            }

            m_BoundEventHandles.Clear();

            // This records that everything is cleaned up.
            m_WaterToolPanelShown = false;
        }

        private void OnToolChanged(ToolBaseSystem tool)
        {
            if (tool != m_CustomWaterToolSystem)
            {
                if (m_WaterToolPanelShown)
                {
                    UnshowWaterToolPanel();
                }

                Enabled = false;
                return;
            }

            Enabled = true;
        }

        private void OnPrefabChanged(PrefabBase prefabBase)
        {
            m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(OnPrefabChanged)}");
            if (prefabBase is WaterSourcePrefab && m_UiView != null)
            {
                m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(OnPrefabChanged)} prefab is water source.");
                WaterSourcePrefab waterSourcePrefab = prefabBase as WaterSourcePrefab;

                // This script sets up the yyWaterTool object if it is not defined.
                UIFileUtils.ExecuteScript(m_UiView, "if (typeof yyWaterTool != 'object') var yyWaterTool = {};");

                // This script changes and translates the Amount label according to the active prefab.
                UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.amount = document.getElementById(\"YYWT-amount-label\"); if (yyWaterTool.amount) {{ yyWaterTool.amount.localeKey = \"{waterSourcePrefab.m_AmountLocaleKey}\"; yyWaterTool.amount.innerHTML = engine.translate(yyWaterTool.amount.localeKey); }}");

                m_Radius = waterSourcePrefab.m_DefaultRadius;
                m_Amount = waterSourcePrefab.m_DefaultAmount;
                TryGetDefaultValuesForWaterSource(waterSourcePrefab, ref m_Amount, ref m_Radius);
                m_AmountIsElevation = false;

                // This script sets the radius field to the desired radius;
                UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.radiusField = document.getElementById(\"YYWT-radius-field\"); if (yyWaterTool.radiusField) yyWaterTool.radiusField.innerHTML = \"{m_Radius} m\";");

                string unit = " m";
                if (waterSourcePrefab.m_SourceType == SourceType.Stream)
                {
                    unit = string.Empty;
                }

                // This script sets the amount field to the desired amount;
                UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.amountField = document.getElementById(\"YYWT-amount-field\"); if (yyWaterTool.amountField) yyWaterTool.amountField.innerHTML = \"{m_Amount}{unit}\";");

                if (waterSourcePrefab.m_SourceType == SourceType.RetentionBasin)
                {
                    m_WaterToolPanelShown = false;
                }
                else
                {
                    // This script destroys the min depth item if it exists.
                    UIFileUtils.ExecuteScript(m_UiView, DestroyElementByID("YYWT-min-depth-item"));
                }

                m_ResetValues = true;

                return;
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
