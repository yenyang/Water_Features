// <copyright file="WaterSourcePrefabValuesRepository.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Settings
{
    using Water_Features.Tools;

    /// <summary>
    /// A class to use for XML serialization and deserialization for storing default amounts and radius for different water sources.
    /// </summary>
    public class WaterSourcePrefabValuesRepository
    {
        private float m_Amount;
        private float m_Radius;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaterSourcePrefabValuesRepository"/> class.
        /// Needed for Deserializer to work.
        /// </summary>
        public WaterSourcePrefabValuesRepository()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaterSourcePrefabValuesRepository"/> class.
        /// </summary>
        /// <param name="sourceType">The type of water source.</param>
        /// <param name="amount">the amount that should be used by default when using this source.</param>
        /// <param name="radius">the radius that should be used by default when using this source.</param>
        public WaterSourcePrefabValuesRepository(WaterToolUISystem.SourceType sourceType, float amount, float radius)
        {
            m_Amount = amount;
            m_Radius = radius;
        }

        /// <summary>
        /// Gets or sets or a sets a value indicating the amount that should be used by default when using this source.
        /// </summary>
        public float Amount
        {
            get { return m_Amount; } set { m_Amount = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating the radius that should be used by default when using this source.
        /// </summary>
        public float Radius
        {
            get { return m_Radius; } set { m_Radius = value; }
        }
    }
}
