// <copyright file="WaterSourcePrefabList.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Domain
{
    using System.Collections.Generic;
    using Colossal.UI.Binding;
    using Water_Features.Prefabs;

    /// <summary>
    /// A struct for passing a list of water source prefabs to the UI.
    /// </summary>
    public struct WaterSourcePrefabList : IJsonWritable
    {
        /// <summary>
        /// List of water source prefabs names and images.
        /// </summary>
        public List<WaterSourcePrefabUIData> waterSourcePrefabUIDatas;

        /// <inheritdoc/>
        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(GetType().FullName);
            writer.PropertyName(nameof(waterSourcePrefabUIDatas));
            writer.ArrayBegin(waterSourcePrefabUIDatas.Count);
            foreach (WaterSourcePrefabUIData waterSourcePrefabData in waterSourcePrefabUIDatas)
            {
                writer.Write(waterSourcePrefabData);
            }

            writer.ArrayEnd();
            writer.TypeEnd();
        }

        /// <summary>
        /// Adds a water source prefab to the list if not included.
        /// </summary>
        /// <param name="prefab">Water source prefab.</param>
        /// <param name="src">Icon path.</param>
        public void Add(WaterSourcePrefab prefab, string src)
        {
            if (!Contains(prefab.name))
            {
                waterSourcePrefabUIDatas.Add(new WaterSourcePrefabUIData() { name = prefab.name, src = src });
            }
        }

        /// <summary>
        /// Removes a water source prefab from the list if included.
        /// </summary>
        /// <param name="prefab">Water source prefab to remove.</param>
        public void Remove(WaterSourcePrefab prefab)
        {
            for (int i = 0; i < waterSourcePrefabUIDatas.Count; i++)
            {
                if (waterSourcePrefabUIDatas[i].name == prefab.name)
                {
                   waterSourcePrefabUIDatas.RemoveAt(i);
                   break;
                }
            }
        }

        /// <summary>
        /// Clears the list of names.
        /// </summary>
        public void Clear()
        {
            waterSourcePrefabUIDatas.Clear();
        }

        /// <summary>
        /// Gets the count of water source prefabs.
        /// </summary>
        /// <returns>Int count of water source Prefab UI datas.</returns>
        public int Count()
        {
            return waterSourcePrefabUIDatas.Count;
        }

        private bool Contains(string name)
        {
            for (int i = 0; i < waterSourcePrefabUIDatas.Count; i++)
            {
                if (waterSourcePrefabUIDatas[i].name == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
