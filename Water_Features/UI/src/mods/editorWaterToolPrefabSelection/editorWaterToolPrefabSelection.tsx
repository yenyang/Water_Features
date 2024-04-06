import { bindValue, trigger, useValue } from "cs2/api";
import { tool } from "cs2/bindings";
import styles from "./editorWaterToolPrefabSelection.module.scss";
import { Button, FOCUS_DISABLED, Icon, Panel, Portal, Tooltip } from "cs2/ui";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import StreamSrc from "./WaterSourceStream.svg";
import LakeSrc from "./WaterSourceLake.svg";
import RiverSrc from "./WaterSourceRiver.svg";
import SeaSrc from "./WaterSourceSea.svg";
import mod from "../../../mod.json";
import { useLocalization } from "cs2/l10n";
import { descriptionTooltip } from "mods/waterToolSections/waterToolSections";

// This functions trigger an event on C# side and C# designates the method to implement.
function changePrefab(prefab: string) {
    trigger(mod.id, eventName, prefab);
}

// These establishes the binding with C# side. Without C# side game ui will crash.
const ActivePrefabName$ =        bindValue<string> (mod.id, 'ActivePrefabName');

// defines trigger event names.
const eventName = "PrefabChange";
const streamPrefab = "WaterSource Stream";
const lakePrefab = "WaterSource VanillaLake";
const riverPrefab = "WaterSource River";
const seaPrefab = "WaterSource Sea";

export const EditorWaterToolPrefabSelectionComponent = () => {

    // These get the value of the bindings.
    const toolActive = useValue(tool.activeTool$).id == "Yenyang's Water Tool";
    const ActivePrefabName = useValue(ActivePrefabName$);

    // translation handling. Translates using locale keys that are defined in C# or fallback string here.
    const { translate } = useLocalization();
    const title =  "Yenyang's " + translate("SubServices.NAME[WaterTool]",      "Water Tool");
    const streamTooltipTitle = translate("Assets.NAME[WaterSource Stream]", "Stream - Constant or Variable Rate Water Source" );
    const streamTooltipDescription = translate("Assets.DESCRIPTION[WaterSource Stream]", "Emits water depending on the settings for this mod. With Seasonal Streams disabled, the flow rate will be constant. With Seasonal Streams enabled the flow rate will vary with season, precipitation, and snowmelt depending on your settings. Left click to place within playable area. Hover over and right click to remove." );
    const riverTooltipTitle = translate("Assets.NAME[WaterSource River]", "River - Border River Water Source");
    const riverTooltipDescription = translate("Assets.DESCRIPTION[WaterSource River]", "Has a constant level and controls water flowing into or out of the border. While near the border, the source will snap to the border. Right click to designate the target elevation. Left click to place. Hover over and right click to remove.");
    const seaTooltipTitle = translate("Assets.NAME[WaterSource Sea]", "Sea - Border Sea Water Source");
    const seaTooltipDescription = translate("Assets.DESCRIPTION[WaterSource Sea]", "Controls water flowing into or out of the border and the lowest sea controls sea level. With Waves and Tides disabled, it will maintain constant level. With Waves and Tides enabled the sea level rises and falls below the original sea level. Right click to designate the elevation. Left click to place if the radius touches a border. Hover over and right click to remove.");
    const lakeTooltipTitle = translate("Assets.NAME[WaterSource Lake]", "Lake - Constant Level Water Source");
    const lakeTooltipDescription = translate("Assets.DESCRIPTION[WaterSource Lake]", "Fills quickly until it gets to the desired level and then maintains that level. If it has a target elevation below the ground level, it can drain water faster than evaporation. Right click to designate the target elevation. Left click to place within playable area. Hover over and right click to remove.");

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
                            <Tooltip tooltip={descriptionTooltip(streamTooltipTitle, streamTooltipDescription)}>
                                <Button className={VanillaComponentResolver.instance.assetGridTheme.item} selected={ActivePrefabName === streamPrefab} variant="icon" onSelect={() => changePrefab(streamPrefab)} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}>
                                    <img src={StreamSrc} className={VanillaComponentResolver.instance.assetGridTheme.thumbnail}></img>
                                </Button>
                            </Tooltip>
                            <Tooltip tooltip={descriptionTooltip(riverTooltipTitle, riverTooltipDescription)}>
                                <Button className={VanillaComponentResolver.instance.assetGridTheme.item} selected={ActivePrefabName === riverPrefab} variant="icon" onSelect={() => changePrefab(riverPrefab)} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}>
                                    <img src={RiverSrc} className={VanillaComponentResolver.instance.assetGridTheme.thumbnail}></img>
                                </Button>
                            </Tooltip>
                            <Tooltip tooltip={descriptionTooltip(lakeTooltipTitle, lakeTooltipDescription)}>
                                <Button className={VanillaComponentResolver.instance.assetGridTheme.item} selected={ActivePrefabName === lakePrefab} variant="icon" onSelect={() => changePrefab(lakePrefab)} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}>
                                    <img src={LakeSrc} className={VanillaComponentResolver.instance.assetGridTheme.thumbnail}></img>
                                </Button>
                            </Tooltip>
                            <Tooltip tooltip={descriptionTooltip(seaTooltipTitle, seaTooltipDescription)}>
                                <Button className={VanillaComponentResolver.instance.assetGridTheme.item} selected={ActivePrefabName === seaPrefab} variant="icon" onSelect={() => changePrefab(seaPrefab)} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}>
                                    <img src={SeaSrc} className={VanillaComponentResolver.instance.assetGridTheme.thumbnail}></img>
                                </Button>
                            </Tooltip>
                        </div>
                    </Panel>
                </Portal>
            )}
        </>
    );
}