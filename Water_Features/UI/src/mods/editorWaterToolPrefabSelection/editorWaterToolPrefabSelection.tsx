import { bindValue, trigger, useValue } from "cs2/api";
import styles from "./editorWaterToolPrefabSelection.module.scss";
import { Button, Panel, Portal, Tooltip } from "cs2/ui";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import mod from "../../../mod.json";
import { useLocalization } from "cs2/l10n";
import { getModule } from "cs2/modding";
import { WaterSourcePrefabList } from "Domain/WaterSourcePrefabList";
import { tool } from "cs2/bindings";
import { CommonWaterToolSections } from "mods/CommonWaterToolSections/commonWaterToolSections";

// This functions trigger an event on C# side and C# designates the method to implement.
function changePrefab(prefab: string) {
    trigger(mod.id, eventName, prefab);
}


// These establishes the binding with C# side. Without C# side game ui will crash.
const WaterSourcePrefabList$ = bindValue<WaterSourcePrefabList>(mod.id, "WaterSourcePrefabList");
// These establishes the binding with C# side. Without C# side game ui will crash.
const ActivePrefabName$ =        bindValue<string> (mod.id, 'ActivePrefabName');

// defines trigger event names.
const eventName = "PrefabChange";
// This functions trigger an event on C# side and C# designates the method to implement.
function handleClick(eventName: string) {
    trigger(mod.id, eventName);
}


const descriptionToolTipStyle = getModule("game-ui/common/tooltip/description-tooltip/description-tooltip.module.scss", "classes");
const assetItem = getModule("game-ui/game/components/item-grid/item-grid.module.scss", "classes");

// This is working, but it's possible a better solution is possible.
export function descriptionTooltip(tooltipTitle: string | null, tooltipDescription: string | null) : JSX.Element {
    return (
        <>
            <div className={descriptionToolTipStyle.title}>{tooltipTitle}</div>
            <div className={descriptionToolTipStyle.content}>{tooltipDescription}</div>
        </>
    );
}

export const EditorWaterToolPrefabSelectionComponent = () => {

    // These get the value of the bindings.
    const ActivePrefabName = useValue(ActivePrefabName$);
    const WaterSourcePrefabList = useValue(WaterSourcePrefabList$);    
    const toolActive = useValue(tool.activeTool$).id == "Yenyang's Water Tool";

    // translation handling. Translates using locale keys that are defined in C# or fallback string here.
    
  const { translate } = useLocalization();
         
        const title =  "Yenyang's " + translate("SubServices.NAME[WaterTool]",      "Water Tool");
   
        
    // This either returns an empty JSX component or the flaming chirper image. Sass is used to determine absolute position, size, and to set z-index. Setting pointer events to none was precautionary. 
    return (
        <>
            <Portal>
                <Panel
                    className={ toolActive? styles.panel: ""}
                    header={(
                        <>
                            {toolActive && (
                                <div className={styles.header}>
                                    {title}
                                </div>
                            )}
                         </>
                    )}>
                    <>
                        {toolActive && (
                            <div className={styles.panelSection}>
                                { WaterSourcePrefabList.waterSourcePrefabUIDatas.map((prefab) => 
                                (
                                    <Tooltip tooltip={descriptionTooltip(translate("Assets.NAME["+prefab.name +"]", prefab.name), translate("Assets.DESCRIPTION["+prefab.name +"]"))}>
                                        <Button className={assetItem.item} selected={ActivePrefabName === prefab.name} variant="icon" onSelect={() => changePrefab(prefab.name)} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}>
                                            <img src={prefab.src} className={assetItem.thumbnail}></img>
                                        </Button>
                                    </Tooltip>
                                ))}
                            </div>
                        )}
                        <CommonWaterToolSections></CommonWaterToolSections>                        
                    </>
                </Panel>
            </Portal>
        </>
    );
}