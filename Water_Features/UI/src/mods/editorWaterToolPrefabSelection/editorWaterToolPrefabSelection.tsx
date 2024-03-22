import { bindValue, trigger, useValue } from "cs2/api";
import { tool } from "cs2/bindings";
import styles from "./editorWaterToolPrefabSelection.module.scss";
import { Button, FOCUS_DISABLED, Icon, Panel, Portal } from "cs2/ui";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import StreamSrc from "./WaterSourceStream.svg";
import LakeSrc from "./WaterSourceLake.svg";
import RiverSrc from "./WaterSourceRiver.svg";
import SeaSrc from "./WaterSourceSea.svg";
import mod from "../../../mod.json";
import { useLocalization } from "cs2/l10n";

// This functions trigger an event on C# side and C# designates the method to implement.
export function changePrefab(prefab: string) {
    trigger(mod.id, eventName, prefab);
}

// These establishes the binding with C# side. Without C# side game ui will crash.
export const ActivePrefabName$ =        bindValue<string> (mod.id, 'ActivePrefabName');

// defines trigger event names.
export const eventName = "PrefabChange";
export const streamPrefab = "WaterSource Stream";
export const lakePrefab = "WaterSource Lake";
export const riverPrefab = "WaterSource River";
export const seaPrefab = "WaterSource Sea";

export const EditorWaterToolPrefabSelectionComponent = () => {
    
    // These get the value of the bindings.
    const toolActive = useValue(tool.activeTool$).id == "Yenyang's Water Tool";
    const ActivePrefabName = useValue(ActivePrefabName$);

    // translation handling. Translates using locale keys that are defined in C# or fallback string here.
    const { translate } = useLocalization();
    const title =  "Yenyang's " + translate("SubServices.NAME[WaterTool]",      "Water Tool");
    
    const UnSelectedButtonTheme = VanillaComponentResolver.instance.assetGridTheme.item;
    const SelectedButtonTheme = VanillaComponentResolver.instance.assetGridTheme.item + " selected";

    // This either returns an empty JSX component or the flaming chirper image. Sass is used to determine absolute position, size, and to set z-index. Setting pointer events to none was precautionary. 
    return (
        <>
            {toolActive && (
                <Portal>
                    <Panel
                        className={styles.panel}
                        header={(
                            <div className={styles.header}>
                                {title}
                            </div>
                        )}>
                        <div className={styles.panelSection}>
                            <Button className={ActivePrefabName === streamPrefab ? SelectedButtonTheme : UnSelectedButtonTheme} variant="icon" onSelect={() => changePrefab(streamPrefab)} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}>
                                <img src={StreamSrc} className={VanillaComponentResolver.instance.assetGridTheme.thumbnail}></img>
                            </Button>
                            <Button className={ActivePrefabName === lakePrefab ? SelectedButtonTheme : UnSelectedButtonTheme} variant="icon" onSelect={() => changePrefab(lakePrefab)} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}>
                                <img src={RiverSrc} className={VanillaComponentResolver.instance.assetGridTheme.thumbnail}></img>
                            </Button>
                            <Button className={ActivePrefabName === riverPrefab ? SelectedButtonTheme : UnSelectedButtonTheme} variant="icon" onSelect={() => changePrefab(riverPrefab)} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}>
                                <img src={LakeSrc} className={VanillaComponentResolver.instance.assetGridTheme.thumbnail}></img>
                            </Button>
                            <Button className={ActivePrefabName === seaPrefab ? SelectedButtonTheme : UnSelectedButtonTheme} variant="icon" onSelect={() => changePrefab(seaPrefab)} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}>
                                <img src={SeaSrc} className={VanillaComponentResolver.instance.assetGridTheme.thumbnail}></img>
                            </Button>
                        </div>
                    </Panel>
                </Portal>
            )}
        </>
    );
}