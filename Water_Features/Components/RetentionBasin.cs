// <copyright file="RetentionBasin.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Components
{
    using Colossal.Serialization.Entities;
    using Unity.Entities;

    /// <summary>
    /// A custom component for Retention Basins.
    /// </summary>
    public struct RetentionBasin : IComponentData, IQueryTypeParameter, ISerializable
    {
        /// <summary>
        /// The maximum water surface elevation that it can raise to.
        /// </summary>
        public float m_MaximumWaterHeight;

        /// <summary>
        /// The minimum water height that will always be there.
        /// </summary>
        public float m_MinimumWaterHeight;

        /// <summary>
        /// Unmelted snow for the basin.
        /// </summary>
        public float m_SnowAccumulation;

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
            writer.Write(m_MinimumWaterHeight);
            writer.Write(m_SnowAccumulation);
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
            reader.Read(out m_MinimumWaterHeight);
            reader.Read(out m_SnowAccumulation);
        }
    }
}
