// <copyright file="AutomatedWaterSource.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Components
{
    using Colossal.Serialization.Entities;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// A custom component for Autofilling Lakes.
    /// </summary>
    public struct AutomatedWaterSource : IComponentData, IQueryTypeParameter, ISerializable
    {
        /// <summary>
        /// The maximum Water Surface Elevation that the water will raise to.
        /// </summary>
        public float m_MaximumWaterHeight;

        /// <summary>
        /// A record of last 4 water heights.
        /// </summary>
        public float4 m_PreviousWaterHeights;

        /// <summary>
        /// A record of previous flow rate.
        /// </summary>
        public float m_PreviousFlowRate;

        /// <summary>
        /// Saves the custom component onto the save file. First item written is the version number.
        /// </summary>
        /// <typeparam name="TWriter">Used by game.</typeparam>
        /// <param name="writer">This is part of the game.</param>
        public void Serialize<TWriter>(TWriter writer)
            where TWriter : IWriter
        {
            writer.Write(1); // Version Number for Component.
            writer.Write(m_MaximumWaterHeight);
            writer.Write(m_PreviousWaterHeights);
            writer.Write(m_PreviousFlowRate);
        }

        /// <summary>
        /// Loads the custom component from the save file. First item read is the version number.
        /// </summary>
        /// <typeparam name="TReader">Used by game.</typeparam>
        /// <param name="reader">This is part of the game.</param>
        public void Deserialize<TReader>(TReader reader)
            where TReader : IReader
        {
            reader.Read(out int version);
            reader.Read(out m_MaximumWaterHeight);
            reader.Read(out m_PreviousWaterHeights);
            reader.Read(out m_PreviousFlowRate);
        }

    }
}
