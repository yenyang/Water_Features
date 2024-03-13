// <copyright file="CustomEditorWaterTool.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Water_Features.Tools
{
    using Game.UI.Editor;
    using Unity.Entities;

    /// <summary>
    /// A custom water tool for editor.
    /// </summary>
    public class CustomEditorWaterTool : EditorTool
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomEditorWaterTool"/> class.
        /// </summary>
        /// <param name="world">Default Game injection world.</param>
        public CustomEditorWaterTool(World world)
            : base(world)
        {
            id = "Yenyang's Water Tool";
            icon = "coui://ui-mods/images/water_features_icon.svg";
            tool = world.GetOrCreateSystemManaged<CustomWaterToolSystem>();
        }
    }
}