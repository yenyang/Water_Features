// <copyright file="LocaleEN.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Settings
{
    using System.Collections.Generic;
    using Colossal;
    using Game.Tools;

    /// <summary>
    /// Localization for Anarchy Mod in English.
    /// </summary>
    public class LocaleEN : IDictionarySource
    {
        private readonly WaterFeaturesSettings m_Setting;

        private Dictionary<string, string> m_Localization;

        /// <inheritdoc/>
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public void Unload()
        {
            throw new System.NotImplementedException();
        }
    }
}
