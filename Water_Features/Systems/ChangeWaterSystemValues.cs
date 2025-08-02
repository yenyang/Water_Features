// <copyright file="ChangeWaterSystemValues.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Systems
{
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Events;
    using Game.Simulation;
    using Game.Tools;
    using Unity.Entities;
    using UnityEngine;
    using Water_Features.Settings;
    using Water_Features.Tools;

    /// <summary>
    /// Changes the various rates of the vanilla water system. Some or all of this could be incorporated into the Settings Apply method.
    /// </summary>
    public partial class ChangeWaterSystemValues : GameSystemBase, IDefaultSerializable, ISerializable
    {
        private readonly float m_ResetTimeLimit = 0.005f;
        private readonly float m_TemporaryEvaporation = 0.1f;
        private bool applyNewEvaporationRate = false;
        private TimeSystem m_TimeSystem;
        private WaterSystem m_WaterSystem;
        private float m_TimeLastChanged = 0f;
        private float m_DateLastChange = 0f;
        private ILog m_Log;
        private float m_OriginalDamping = 0.995f;
        private bool m_TemporarilyUseOriginalDamping = false;
        private ToolSystem m_ToolSystem;
        private CustomWaterToolSystem m_CustomWaterToolSystem;
        private WaterDamageSystem m_WaterDamageSystem;
        private SubmergeSystem m_SubmergeSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeWaterSystemValues"/> class.
        /// </summary>
        public ChangeWaterSystemValues()
        {
        }

        /// <inheritdoc/>
        public void SetDefaults(Context context)
        {
            WaterFeaturesMod.Instance.Settings.Fluidness = 0.1f;
            WaterFeaturesMod.Instance.Settings.EvaporationRate = 0.0001f;
            WaterFeaturesMod.Instance.Settings.ForceWaterSimulationSpeed = false;
            WaterFeaturesMod.Instance.Settings.WaterCausesDamage = true;
        }

        /// <inheritdoc/>
        public void Serialize<TWriter>(TWriter writer)
            where TWriter : IWriter
        {
            writer.Write(2);
            writer.Write(WaterFeaturesMod.Instance.Settings.EvaporationRate);
            writer.Write(WaterFeaturesMod.Instance.Settings.Fluidness);
            writer.Write(WaterFeaturesMod.Instance.Settings.ForceWaterSimulationSpeed);
            writer.Write(WaterFeaturesMod.Instance.Settings.WaterCausesDamage);
        }

        /// <inheritdoc/>
        public void Deserialize<TReader>(TReader reader)
            where TReader : IReader
        {
            reader.Read(out int version);
            reader.Read(out float evaporationRate);
            reader.Read(out float fluidness);
            reader.Read(out bool forceWaterSimulationSpeed);

            WaterFeaturesMod.Instance.Settings.EvaporationRate = evaporationRate;
            WaterFeaturesMod.Instance.Settings.Fluidness = fluidness;
            WaterFeaturesMod.Instance.Settings.ForceWaterSimulationSpeed = forceWaterSimulationSpeed;

            if (version >= 2)
            {
                reader.Read(out bool waterCausesDamage);
                WaterFeaturesMod.Instance.Settings.WaterCausesDamage = waterCausesDamage;
            }
            else
            {
                WaterFeaturesMod.Instance.Settings.WaterCausesDamage = true;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to apply a new evaporation rate. Toggled by WaterFeaturesSetting Button.
        /// </summary>
        public bool ApplyNewEvaporationRate { get => applyNewEvaporationRate; set => applyNewEvaporationRate = value; }

        /// <summary>
        /// Gets or Sets a value indicating whether to temporarily apply a new evaporation rate.
        /// </summary>
        public bool TemporarilyUseOriginalDamping { get => m_TemporarilyUseOriginalDamping; set => m_TemporarilyUseOriginalDamping = value; }

        /// <summary>
        /// Gets a value indicating the original damping value.
        /// </summary>
        public float OriginalDamping { get => m_OriginalDamping; }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = WaterFeaturesMod.Instance.Log;
            m_WaterSystem = World.GetOrCreateSystemManaged<WaterSystem>();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_CustomWaterToolSystem = World.GetOrCreateSystemManaged<CustomWaterToolSystem>();
            m_WaterDamageSystem = World.GetOrCreateSystemManaged<WaterDamageSystem>();
            m_SubmergeSystem = World.GetOrCreateSystemManaged<SubmergeSystem>();

            m_OriginalDamping = m_WaterSystem.m_Damping;
            m_Log.Info($"{nameof(ChangeWaterSystemValues)}.{nameof(OnCreate)} m_WaterSystem.m_Evaporation {m_WaterSystem.m_Evaporation}");
            m_Log.Info($"{nameof(ChangeWaterSystemValues)}.{nameof(OnCreate)} m_WaterSystem.m_Fluidness {m_WaterSystem.m_Fluidness}");
            m_TimeSystem = World.GetOrCreateSystemManaged<TimeSystem>();
            m_Log.Info($"[{nameof(ChangeWaterSystemValues)}] {nameof(OnCreate)}");
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            m_WaterDamageSystem.Enabled = WaterFeaturesMod.Instance.Settings.WaterCausesDamage;
            m_SubmergeSystem.Enabled = WaterFeaturesMod.Instance.Settings.WaterCausesDamage;
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (m_ToolSystem.actionMode.IsGame() &&
                WaterFeaturesMod.Instance.Settings.ForceWaterSimulationSpeed &&
                m_WaterSystem.WaterSimSpeed < 1 &&
                m_ToolSystem.activeTool != m_CustomWaterToolSystem)
            {
                m_WaterSystem.WaterSimSpeed = 1;
            }

            if ((m_ToolSystem.actionMode.IsEditor() && WaterFeaturesMod.Instance.Settings.WaterToolSettingsAffectEditorSimulation) || m_ToolSystem.actionMode.IsGame())
            {
                // This is for the water cleanup cycle.
                if (ApplyNewEvaporationRate)
                {
                    m_WaterSystem.m_Evaporation = m_TemporaryEvaporation;
                    m_Log.Info($"[{nameof(ChangeWaterSystemValues)}] {nameof(OnCreate)} changed evaporation rate to {m_TemporaryEvaporation}");
                    m_TimeLastChanged = m_TimeSystem.normalizedTime;
                    m_DateLastChange = m_TimeSystem.normalizedDate;
                    ApplyNewEvaporationRate = false;
                }

                // This is for changin the evaporation rate with the settings.
                if (!Mathf.Approximately(WaterFeaturesMod.Instance.Settings.EvaporationRate, m_WaterSystem.m_Evaporation))
                {
                    if (m_TimeSystem.normalizedTime > m_TimeLastChanged + m_ResetTimeLimit || m_DateLastChange > m_DateLastChange + m_ResetTimeLimit)
                    {
                        m_WaterSystem.m_Evaporation = WaterFeaturesMod.Instance.Settings.EvaporationRate;
                        m_Log.Info($"[{nameof(ChangeWaterSystemValues)}] {nameof(OnCreate)} changed evaporation rate back to {WaterFeaturesMod.Instance.Settings.EvaporationRate}");
                    }
                }

                if (!Mathf.Approximately(m_WaterSystem.m_Fluidness, WaterFeaturesMod.Instance.Settings.Fluidness))
                {
                    m_WaterSystem.m_Fluidness = WaterFeaturesMod.Instance.Settings.Fluidness;
                    m_Log.Info($"[{nameof(ChangeWaterSystemValues)}] {nameof(OnCreate)} changed Fluidness to {m_WaterSystem.m_Fluidness}.");
                }
            }

            if (m_ToolSystem.actionMode.IsGameOrEditor())
            {
                // This is for changing the damping constant with the settings.
                if (!Mathf.Approximately(m_WaterSystem.m_Damping, WaterFeaturesMod.Instance.Settings.Damping) && WaterFeaturesMod.Instance.Settings.EnableWavesAndTides && !m_TemporarilyUseOriginalDamping)
                {
                    m_WaterSystem.m_Damping = WaterFeaturesMod.Instance.Settings.Damping;
                }
                else if ((!Mathf.Approximately(m_WaterSystem.m_Damping, m_OriginalDamping) && !WaterFeaturesMod.Instance.Settings.EnableWavesAndTides) || m_TemporarilyUseOriginalDamping)
                {
                    m_WaterSystem.m_Damping = m_OriginalDamping;
                    if (m_WaterSystem.WaterSimSpeed == 0 && !WaterFeaturesMod.Instance.Settings.EnableWavesAndTides)
                    {
                        m_WaterSystem.WaterSimSpeed = 1;
                    }
                }
            }

        }
    }
}