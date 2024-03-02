// <copyright file="WaterSourcePrefab.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Prefabs
{
    using System;
    using System.Collections.Generic;
    using Game.Prefabs;
    using Unity.Entities;
    using Water_Features.Tools;

    /// <summary>
    /// A custom prefab for water sources to put in the menu.
    /// </summary>
    [ComponentMenu("Tools/", new Type[] { })]
    public class WaterSourcePrefab : PrefabBase
    {
        /// <summary>
        /// Type of water source.
        /// </summary>
        public WaterToolUISystem.SourceType m_SourceType;

        /// <summary>
        /// The locale key for this type of water source for the amount row.
        /// </summary>
        public string m_AmountLocaleKey;

        /// <summary>
        /// The default value for the radius field.
        /// </summary>
        public float m_DefaultRadius;

        /// <summary>
        /// The default value for the amount field.
        /// </summary>
        public float m_DefaultAmount;

        /// <inheritdoc/>
        public override void GetPrefabComponents(HashSet<ComponentType> components)
        {
            base.GetPrefabComponents(components);
        }

        /// <inheritdoc/>
        public override void GetArchetypeComponents(HashSet<ComponentType> components)
        {
            base.GetArchetypeComponents(components);
        }
    }
}
