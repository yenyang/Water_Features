import { ModRegistrar } from "cs2/modding";
import { WaterToolComponent } from "mods/waterToolSections/waterToolSections";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import { EditorWaterToolPrefabSelectionComponent } from "mods/editorWaterToolPrefabSelection/editorWaterToolPrefabSelection";
import mod from "../mod.json";
import { ToolOptionsVisibility } from "mods/ToolOptionsVisible/toolOptionsVisible";

const register: ModRegistrar = (moduleRegistry) => {
      // The vanilla component resolver is a singleton that helps extrant and maintain components from game that were not specifically exposed.
      VanillaComponentResolver.setRegistry(moduleRegistry);
      
     // console.log('mr', moduleRegistry);
     moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', WaterToolComponent);

     // This appends the right bottom floating menu with a chirper image that is just floating above the vanilla chirper image. Hopefully noone moves it.
     moduleRegistry.append('Editor', EditorWaterToolPrefabSelectionComponent);


     // Ensures tool option is visible for Yenyang's custom water tool Tool.
      moduleRegistry.extend("game-ui/game/components/tool-options/tool-options-panel.tsx", 'useToolOptionsVisible', ToolOptionsVisibility);

     // This is just to verify using UI console that all the component registriations was completed.
     console.log(mod.id + " UI module registrations completed.");
}

export default register;