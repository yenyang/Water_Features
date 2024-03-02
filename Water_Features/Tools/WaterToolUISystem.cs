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
    using Water_Features.Utils;

    /// <summary>
    /// UI system for Custom Water Tool.
    /// </summary>
    public partial class WaterToolUISystem : UISystemBase
    {
        private View m_UiView;
        private ToolSystem m_ToolSystem;
        private string m_InjectedJS = string.Empty;
        private string m_AmountItemScript = string.Empty;
        private string m_RadiusItemScript = string.Empty;
        private string m_MinDepthItemScript = string.Empty;
        private CustomWaterToolSystem m_CustomWaterToolSystem;
        private TerrainSystem m_TerrainSystem;
        private ILog m_Log;
        private bool m_WaterToolPanelShown;
        private List<BoundEventHandle> m_BoundEventHandles;
        private float m_Radius = 10f;
        private float m_Amount = 5f;
        private float m_MinDepth = 10f;
        private Dictionary<string, Action> m_ChangeValueActions;
        private bool m_ButtonPressed = false;
        private float m_AmountRateOfChange = 1f;
        private float m_RadiusRateOfChange = 1f;
        private float m_MinDepthRateOfChange = 1f;
        private bool m_ResetValues = true;
        private bool m_FirstTimeInjectingJS = true;
        private bool m_AmountIsElevation = false;
        private string m_ContentFolder;
        private Dictionary<WaterSourcePrefab, WaterSourcePrefabValuesRepository> m_WaterSourcePrefabValuesRepositories;

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
            get { return m_Radius; }
        }

        /// <summary>
        /// Gets the amount.
        /// </summary>
        public float Amount
        {
            get { return m_Amount; }
        }

        /// <summary>
        /// Gets the min depth.
        /// </summary>
        public float MinDepth
        {
            get { return m_MinDepth; }
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
            m_Amount = Mathf.Round(elevation * 10f) / 10f;
            m_AmountIsElevation = true;
            string localeKey = "YY_WATER_FEATURES.Elevation";

            UIFileUtils.ExecuteScript(m_UiView, "if (typeof yyWaterTool != 'object') var yyWaterTool = {};");

            // This script changes and translates the Amount label according to the active prefab.
            UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.amount = document.getElementById(\"YYWT-amount-label\"); if (yyWaterTool.amount) {{ yyWaterTool.amount.localeKey = \"{localeKey}\"; yyWaterTool.amount.innerHTML = engine.translate(yyWaterTool.amount.localeKey); }}");

            // This script sets the amount field to the desired amount;
            UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.amountField = document.getElementById(\"YYWT-amount-field\"); if (yyWaterTool.amountField) yyWaterTool.amountField.innerHTML = \"{m_Amount} m\";");
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
            m_UiView = GameManager.instance.userInterface.view.View;
            ToolSystem toolSystem = m_ToolSystem; // I don't know why vanilla game did this.
            m_ToolSystem.EventToolChanged = (Action<ToolBaseSystem>)Delegate.Combine(toolSystem.EventToolChanged, new Action<ToolBaseSystem>(OnToolChanged));
            ToolSystem toolSystem2 = m_ToolSystem; // I don't know why vanilla game did this.
            m_ToolSystem.EventPrefabChanged = (Action<PrefabBase>)Delegate.Combine(toolSystem2.EventPrefabChanged, new Action<PrefabBase>(OnPrefabChanged));
            m_ContentFolder = Path.Combine(EnvPath.kUserDataPath, "ModsData", "Mods_Yenyang_Water_Features");
            Directory.CreateDirectory(m_ContentFolder);
            m_BoundEventHandles = new ();

            m_InjectedJS = UIFileUtils.ReadJS(Path.Combine(UIFileUtils.AssemblyPath, "ui.js"));
            m_AmountItemScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYWT-Amount-Item.html"), "if (document.getElementById(\"YYWT-amount-item\") == null) { yyWaterTool.div.className = \"item_bZY\"; yyWaterTool.div.id = \"YYWT-amount-item\"; yyWaterTool.entities = document.getElementsByClassName(\"tool-options-panel_Se6\"); if (yyWaterTool.entities[0] != null) { yyWaterTool.entities[0].insertAdjacentElement('afterbegin', yyWaterTool.div); } }");
            m_RadiusItemScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYWT-Radius-Item.html"), "if (document.getElementById(\"YYWT-radius-item\") == null) { yyWaterTool.div.className = \"item_bZY\"; yyWaterTool.div.id = \"YYWT-radius-item\"; yyWaterTool.amountItem = document.getElementById(\"YYWT-amount-item\"); if (yyWaterTool.amountItem != null) { yyWaterTool.amountItem.insertAdjacentElement('afterend', yyWaterTool.div); } }");
            m_MinDepthItemScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYWT-Min-Depth-Item.html"), "if (document.getElementById(\"YYWT-min-depth-item\") == null) { yyWaterTool.div.className = \"item_bZY\"; yyWaterTool.div.id = \"YYWT-min-depth-item\"; yyWaterTool.amountItem = document.getElementById(\"YYWT-amount-item\"); if (yyWaterTool.amountItem != null) { yyWaterTool.amountItem.insertAdjacentElement('afterend', yyWaterTool.div); } }");

            if (m_UiView == null)
            {
                m_Log.Warn($"{nameof(WaterToolUISystem)}.{nameof(OnCreate)} m_UiView == null");
            }

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
        protected override void OnUpdate()
        {
            if (m_UiView == null)
            {
                return;
            }

            if (m_ToolSystem.activeTool != m_CustomWaterToolSystem)
            {
                if (m_WaterToolPanelShown)
                {
                    UnshowWaterToolPanel();
                    Enabled = false;
                }

                return;
            }

            m_ButtonPressed = false;

            if (!m_WaterToolPanelShown)
            {
                UIFileUtils.ExecuteScript(m_UiView, "if (typeof yyWaterTool != 'object') var yyWaterTool = {};");

                if (m_InjectedJS == string.Empty)
                {
                    m_Log.Warn($"{nameof(WaterToolUISystem)}.{nameof(OnUpdate)} m_InjectedJS was empty. Did you put the ui.js file in the mod install folder?");
                    m_InjectedJS = UIFileUtils.ReadJS(Path.Combine(UIFileUtils.AssemblyPath, "ui.js"));
                    m_AmountItemScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYWT-Amount-Item.html"), "if (document.getElementById(\"YYWT-amount-item\") == null) { yyWaterTool.div.className = \"item_bZY\"; yyWaterTool.div.id = \"YYWT-amount-item\"; yyWaterTool.entities = document.getElementsByClassName(\"tool-options-panel_Se6\"); if (yyWaterTool.entities[0] != null) { yyWaterTool.entities[0].insertAdjacentElement('afterbegin', yyWaterTool.div); } }");
                    m_RadiusItemScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYWT-Radius-Item.html"), "if (document.getElementById(\"YYWT-radius-item\") == null) { yyWaterTool.div.className = \"item_bZY\"; yyWaterTool.div.id = \"YYWT-radius-item\"; yyWaterTool.amountItem = document.getElementById(\"YYWT-amount-item\"); if (yyWaterTool.amountItem != null) { yyWaterTool.amountItem.insertAdjacentElement('afterend', yyWaterTool.div); } }");
                    m_MinDepthItemScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYWT-Min-Depth-Item.html"), "if (document.getElementById(\"YYWT-min-depth-item\") == null) { yyWaterTool.div.className = \"item_bZY\"; yyWaterTool.div.id = \"YYWT-min-depth-item\"; yyWaterTool.amountItem = document.getElementById(\"YYWT-amount-item\"); if (yyWaterTool.amountItem != null) { yyWaterTool.amountItem.insertAdjacentElement('afterend', yyWaterTool.div); } }");
                }

                UIFileUtils.ExecuteScript(m_UiView, m_AmountItemScript);

                UIFileUtils.ExecuteScript(m_UiView, m_RadiusItemScript);

                // This script defines the JS functions and sets up typical buttons.
                UIFileUtils.ExecuteScript(m_UiView, m_InjectedJS);

                // Waiting until the next frame gives some extra time to ensure JS functions are injected.
                if (m_FirstTimeInjectingJS)
                {
                    m_FirstTimeInjectingJS = false;
                    return;
                }

                string unit = " m";

                if (m_CustomWaterToolSystem.GetPrefab() != null)
                {
                    WaterSourcePrefab waterSourcePrefab = m_CustomWaterToolSystem.GetPrefab() as WaterSourcePrefab;

                    string localeKey = waterSourcePrefab.m_AmountLocaleKey;

                    if (m_AmountIsElevation)
                    {
                        localeKey = "YY_WATER_FEATURES.Elevation";
                    }

                    // This script changes and translates the Amount label according to the active prefab.
                    UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.amount = document.getElementById(\"YYWT-amount-label\"); if (yyWaterTool.amount) {{ yyWaterTool.amount.localeKey = \"{localeKey}\"; yyWaterTool.amount.innerHTML = engine.translate(yyWaterTool.amount.localeKey); }}");

                    if (m_ResetValues)
                    {
                        m_Radius = waterSourcePrefab.m_DefaultRadius;
                        m_Amount = waterSourcePrefab.m_DefaultAmount;
                        TryGetDefaultValuesForWaterSource(waterSourcePrefab, ref m_Amount, ref m_Radius);
                        m_AmountIsElevation = false;
                    }

                    if (waterSourcePrefab.m_SourceType == SourceType.Stream)
                    {
                        unit = string.Empty;
                    }

                    if (waterSourcePrefab.m_SourceType == SourceType.RetentionBasin)
                    {
                        UIFileUtils.ExecuteScript(m_UiView, m_MinDepthItemScript);

                        if (m_ResetValues)
                        {
                            m_MinDepth = 10f;
                        }

                        // This script sets the min depth field to the desired min depth;
                        UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.minDepthField = document.getElementById(\"YYWT-min-depth-field\"); if (yyWaterTool.minDepthField) yyWaterTool.minDepthField.innerHTML = \"{m_MinDepth} m\";");

                        // This script sets up the up and down buttons for min depth and applies localization to the row.
                        UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.applyLocalization(document.getElementById(\"YYWT-min-depth-item\")); yyWaterTool.setupButton(\"YYWT-min-depth-down-arrow\", \"min-depth-down-arrow\"); yyWaterTool.setupButton(\"YYWT-min-depth-up-arrow\", \"min-depth-up-arrow\");  yyWaterTool.setupButton(\"YYWT-min-depth-rate-of-change\", \"min-depth-rate-of-change\");");

                        SetRateIcon(m_MinDepthRateOfChange, "min-depth");
                    }

                    m_ResetValues = false;
                }

                // This script sets the radius field to the desired radius;
                UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.radiusField = document.getElementById(\"YYWT-radius-field\"); if (yyWaterTool.radiusField) yyWaterTool.radiusField.innerHTML = \"{m_Radius} m\";");

                // This script sets the amount field to the desired amount;
                UIFileUtils.ExecuteScript(m_UiView, $"yyWaterTool.amountField = document.getElementById(\"YYWT-amount-field\"); if (yyWaterTool.amountField) yyWaterTool.amountField.innerHTML = \"{m_Amount}{unit}\";");

                SetRateIcon(m_RadiusRateOfChange, "radius");
                SetRateIcon(m_AmountRateOfChange, "amount");

                m_BoundEventHandles.Add(m_UiView.RegisterForEvent("YYWT-log", (Action<string>)LogFromJS));
                m_BoundEventHandles.Add(m_UiView.RegisterForEvent("CheckForElement-YYWT-amount-item", (Action<bool>)ElementCheck));

                foreach (KeyValuePair<string, Action> kvp in m_ChangeValueActions)
                {
                    m_BoundEventHandles.Add(m_UiView.RegisterForEvent("Change-Value", (Action<string>)ChangeValue));
                }

                m_WaterToolPanelShown = true;
            }
            else
            {
                // This script checks if water tool panel exists. If it doesn't it triggers water tool panel item being recreated.
                UIFileUtils.ExecuteScript(m_UiView, $"if (document.getElementById(\"YYWT-amount-item\") == null) engine.trigger('CheckForElement-YYWT-amount-item', false);");
            }

            base.OnUpdate();
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
        }

        /// <summary>
        /// Get a script for Destroing element by id if that element exists.
        /// </summary>
        /// <param name="id">The id from HTML or JS.</param>
        /// <returns>a script for Destroing element by id if that element exists.</returns>
        private string DestroyElementByID(string id)
        {
            return $"yyWaterTool.itemElement = document.getElementById(\"{id}\"); if (yyWaterTool.itemElement) yyWaterTool.itemElement.parentElement.removeChild(yyWaterTool.itemElement);";
        }

        /// <summary>
        /// Logs a string from JS.
        /// </summary>
        /// <param name="log">A string from JS to log.</param>
        private void LogFromJS(string log) => m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(LogFromJS)} {log}");

        /// <summary>
        /// Converts a C# bool to JS string.
        /// </summary>
        /// <param name="flag">a bool.</param>
        /// <returns>"true" or "false".</returns>
        private string BoolToString(bool flag)
        {
            if (flag)
            {
                return "true";
            }

            return "false";
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

            if (m_ButtonPressed)
            {
                return;
            }

            m_Log.Debug($"{nameof(WaterToolUISystem)}.{nameof(ChangeValue)} buttonID = {buttonID}");
            if (m_ChangeValueActions.ContainsKey(buttonID))
            {
                m_ChangeValueActions[buttonID].Invoke();
            }

            m_ButtonPressed = true;
        }

        private void IncreaseRadius()
        {

            float signaficantFigures = Mathf.Pow(10f, -1f * Mathf.Log(m_RadiusRateOfChange, 2f));
            if (m_Radius >= 1000f && m_Radius < 10000f)
            {
                m_Radius += 500f * m_RadiusRateOfChange;
                m_Radius = Mathf.Round(m_Radius * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
            }
            else if (m_Radius >= 500f && m_Radius < 1000f)
            {
                m_Radius += 100f * m_RadiusRateOfChange;
                m_Radius = Mathf.Round(m_Radius * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
            }
            else if (m_Radius >= 100f && m_Radius < 500f)
            {
                m_Radius += 50f * m_RadiusRateOfChange;
                m_Radius = Mathf.Round(m_Radius * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (m_Radius >= 10f && m_Radius < 100f)
            {
                m_Radius += 10f * m_RadiusRateOfChange;
                m_Radius = Mathf.Round(m_Radius * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (m_Radius < 10000)
            {
               m_Radius += 1f * m_RadiusRateOfChange;
               m_Radius = Mathf.Round(m_Radius * signaficantFigures) / signaficantFigures;
            }

            if (WaterFeaturesMod.Settings.TrySmallerRadii)
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

        private void DecreaseRadius()
        {
            float signaficantFigures = Mathf.Pow(10f, -1f * Mathf.Log(m_RadiusRateOfChange, 2f));
            if (m_Radius <= 10f && m_Radius > 1f)
            {
                m_Radius -= 1f * m_RadiusRateOfChange;
                m_Radius = Mathf.Round(m_Radius * signaficantFigures) / signaficantFigures;
            }
            else if (m_Radius <= 100f && m_Radius > 10f)
            {
                m_Radius -= 10f * m_RadiusRateOfChange;
                m_Radius = Mathf.Round(m_Radius * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (m_Radius <= 500f && m_Radius > 100f)
            {
                m_Radius -= 50f * m_RadiusRateOfChange;
                m_Radius = Mathf.Round(m_Radius * 0.1f * signaficantFigures) / (0.1f * signaficantFigures);
            }
            else if (m_Radius <= 1000f && m_Radius > 500f)
            {
                m_Radius -= 100f * m_RadiusRateOfChange;
                m_Radius = Mathf.Round(m_Radius * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
            }
            else if (m_Radius > 1000f)
            {
                m_Radius -= 500f * m_RadiusRateOfChange;
                m_Radius = Mathf.Round(m_Radius * 0.01f * signaficantFigures) / (0.01f * signaficantFigures);
            }

            if (WaterFeaturesMod.Settings.TrySmallerRadii)
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
            m_RadiusRateOfChange /= 2f;
            if (m_RadiusRateOfChange < 0.125f)
            {
                m_RadiusRateOfChange = 1.0f;
            }

            SetRateIcon(m_RadiusRateOfChange, "radius");
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
