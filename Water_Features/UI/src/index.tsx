import { ModRegistrar } from "modding/types";
import { WaterToolComponent } from "mods/waterToolSections";

const register: ModRegistrar = (moduleRegistry) => {
     // console.log('mr', moduleRegistry);
     moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', WaterToolComponent(moduleRegistry));
}

export default register;