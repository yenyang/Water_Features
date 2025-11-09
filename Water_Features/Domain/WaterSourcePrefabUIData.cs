// <copyright file="WaterSourcePrefabData.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Domain
{
    using Colossal.UI.Binding;

    /// <summary>
    /// A struct for passing a list of water source prefabs to the UI.
    /// </summary>
    public struct WaterSourcePrefabUIData : IJsonWritable
    {
        /// <summary>
        /// Prefab Name.
        /// </summary>
        public string name;

        /// <summary>
        /// Image path.
        /// </summary>
        public string src;

        /// <inheritdoc/>
        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(GetType().FullName);
            writer.PropertyName(nameof(name));
            writer.Write(name);
            writer.PropertyName(nameof(src));
            writer.Write(src);
            writer.TypeEnd();
        }
    }
}
