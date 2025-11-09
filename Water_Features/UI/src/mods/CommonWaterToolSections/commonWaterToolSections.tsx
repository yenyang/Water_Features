import { bindValue, trigger, useValue } from "cs2/api";

import mod from "../../../mod.json";
import { getModule } from "cs2/modding";
import { tool } from "cs2/bindings";
import { useLocalization } from "cs2/l10n";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import locale from "../../lang/en-US.json";


// These contain the coui paths to Unified Icon Library svg assets
const uilStandard =                         "coui://uil/Standard/";
const uilColored =                         "coui://uil/Colored/";

const arrowDownSrc =         uilStandard +  "ArrowDownThickStroke.svg";
const arrowUpSrc =           uilStandard +  "ArrowUpThickStroke.svg";
const radiusChangeSrc =      uilStandard + "WaterSourceChangeRadius.svg";
const moveWaterSourceSrc =   uilStandard + "WaterSourceMove.svg";
const placeWaterSourceSrc = uilStandard + "WaterSourcePlacement.svg";
const elevationChangeSrc = uilStandard + "WaterSourceRaiseOrLower.svg";
const amountIsElevationSrc = uilStandard + "ArrowDownTriangle.svg";

// These establishes the binding with C# side. Without C# side game ui will crash.
const AmountValue$ =        bindValue<number> (mod.id, 'AmountValue');
const RadiusValue$ =        bindValue<number> (mod.id, 'RadiusValue');
const MinDepthValue$ =      bindValue<number> (mod.id, 'MinDepthValue');
const AmountLocaleKey$ =    bindValue<string> (mod.id, 'AmountLocaleKey');
const AmountStep$ =         bindValue<number> (mod.id, 'AmountStep');
const RadiusStep$ =         bindValue<number> (mod.id, 'RadiusStep');
const MinDepthStep$ =       bindValue<number> (mod.id, 'MinDepthStep');
const AmountScale$ =        bindValue<number> (mod.id, 'AmountScale');
const RadiusScale$ =        bindValue<number> (mod.id, 'RadiusScale');
const MinDepthScale$ =      bindValue<number> (mod.id, 'MinDepthScale');
const ShowMinDepth$ =       bindValue<number> (mod.id, 'ShowMinDepth');
const ToolMode$ =           bindValue<number> (mod.id, "ToolMode");
const AmountIsElevation$ =  bindValue<boolean>(mod.id, 'AmountIsElevation');

// These are strings that will be used for translations keys and event triggers.
const amountDownID =             "amount-down-arrow";
const amountUpID =               "amount-up-arrow";
const radiusDownID =             "radius-down-arrow";
const radiusUpID =               "radius-up-arrow";
const minDepthDownID =           "min-depth-down-arrow";
const minDepthUpID =             "min-depth-up-arrow";
const amountStepID =             "amount-rate-of-change";
const radiusStepID =             "radius-rate-of-change";
const minDepthStepID =           "min-depth-rate-of-change";
const tooltipDescriptionPrefix = "YY_WATER_FEATURES_DESCRIPTION.";
const sectionTitlePrefix =       "YY_WATER_FEATURES.";
const elevationChangeID =        "ElevationChange";
const placeWaterSourceID =       "PlaceWaterSource";
const moveWaterSourceID =        "MoveWaterSource";
const radiusChangeID =           "RadiusChange";

// Stores the default values for the step arrays. Must be descending order.
const defaultValues : number[] =[1.0, 0.5, 0.25, 0.125];


// This functions trigger an event on C# side and C# designates the method to implement.
function handleClick(eventName: string) {
    trigger(mod.id, eventName);
}

// This function triggers an event to change the water tool mode to specified tool mode.
function changeToolMode(toolMode: WaterToolModes) {
    trigger(mod.id, "ChangeToolMode", toolMode);
}

enum WaterToolModes 
{
    PlaceWaterSource,
    ElevationChange,
    MoveWaterSource,
    RadiusChange,
}

const descriptionToolTipStyle = getModule("game-ui/common/tooltip/description-tooltip/description-tooltip.module.scss", "classes");

// This is working, but it's possible a better solution is possible.
export function descriptionTooltip(tooltipTitle: string | null, tooltipDescription: string | null) : JSX.Element {
    return (
        <>
            <div className={descriptionToolTipStyle.title}>{tooltipTitle}</div>
            <div className={descriptionToolTipStyle.content}>{tooltipDescription}</div>
        </>
    );
}

export const CommonWaterToolSections = () => {
 // These get the value of the bindings.
    const toolActive = useValue(tool.activeTool$).id == "Yenyang's Water Tool";
    const AmountValue = useValue(AmountValue$);
    const RadiusValue = useValue(RadiusValue$);
    const MinDepthValue = useValue(MinDepthValue$);
    const AmountLocaleKey = useValue(AmountLocaleKey$);
    const AmountStep = useValue(AmountStep$);
    const RadiusStep = useValue(RadiusStep$);
    const MinDepthStep = useValue(MinDepthStep$);
    const AmountScale = useValue(AmountScale$);
    const RadiusScale = useValue(RadiusScale$);
    const MinDepthScale = useValue(MinDepthScale$);
    const ShowMinDepth = useValue(ShowMinDepth$);
    const ToolMode = useValue(ToolMode$);
    const AmountIsElevation = useValue(AmountIsElevation$);
    
    // Gets a boolean for whether the amount is a flow.
    const amountIsFlow : boolean = AmountLocaleKey == "YY_WATER_FEATURES.Flow";

    // translation handling. Translates using locale keys that are defined in C# or fallback string here.
    const { translate } = useLocalization();
    const amountDownTooltip =       translate(tooltipDescriptionPrefix + amountDownID,      "Reduces the flow for Streams. Decreases the depth or elevation for rivers, seas, and lakes. Reduces the max depth for retention and detention basins.");
    const amountUpTooltip =         translate(tooltipDescriptionPrefix + amountUpID,        "Increases the flow for Streams. Increases the depth or elevation for rivers, seas, and lakes. Increases the max depth for retention and detention basins.");
    const radiusDownTooltip =       translate(tooltipDescriptionPrefix + radiusDownID,      "Reduces the radius.");
    const radiusUpTooltip =         translate(tooltipDescriptionPrefix + radiusUpID,        "Increases the radius.");
    const minDepthDownTooltip =     translate(tooltipDescriptionPrefix + minDepthDownID,    "Reduces the minimum depth.");
    const minDepthUpTooltip =       translate(tooltipDescriptionPrefix + minDepthUpID,      "Increases the minimum depth.");
    const amountStepTooltip =       translate(tooltipDescriptionPrefix + amountStepID,      "Changes the rate in which the increase and decrease buttons work for Flow, Depth and Elevation.");
    const radiusStepTooltip =       translate(tooltipDescriptionPrefix + radiusStepID,      "Changes the rate in which the increase and decrease buttons work for Radius.");
    const minDepthStepTooltip =     translate(tooltipDescriptionPrefix + minDepthStepID,    "Changes the rate in which the increase and decrease buttons work for minimum depth.");
    const minDepthSection =         translate(sectionTitlePrefix + "MinDepth",              "Min Depth");
    const radiusSection =           translate(sectionTitlePrefix + "Radius",                "Radius");
    const amountSection =           translate(AmountLocaleKey,                              "Depth");
    const elevationChangeTooltip =  translate(tooltipDescriptionPrefix + elevationChangeID, "Water Tool will change target elevations of existing water sources by hovering over existing water source, left clicking, holding, dragging and releasing at new elevation. Usually dragging out raises, and dragging in lowers, but it's really just releasing at the desired elevation. Keep the cursor within playable area for reliability. Right click to cancel.");
    const placeWaterSourceTooltip = translate(tooltipDescriptionPrefix + placeWaterSourceID,"Water Tool will place water sources with left click, and remove water sources with right click.");
    const moveWaterSourceTooltip =  translate(tooltipDescriptionPrefix + moveWaterSourceID, "Water Tool will move existing water sources. Target elevations of existing water sources will not change. Right click to cancel.");
    const radiusChangeTooltip =     translate(tooltipDescriptionPrefix + radiusChangeID,    "Water Tool will change radius of water sources. Right click to cancel.");
    const toolModeTitle =           translate("Toolbar.TOOL_MODE_TITLE", "Tool Mode");
    const placeWaterSourceTitle =   translate(sectionTitlePrefix + placeWaterSourceID,      "Place Water Source");
    const moveWaterSourceTitle =    translate(sectionTitlePrefix + moveWaterSourceID,       "Move Water Source");
    const elevationChangeTitle =    translate(sectionTitlePrefix + elevationChangeID,       "Elevation Change");
    const radiusChangeTitle =       translate(sectionTitlePrefix + radiusChangeID,          "Change Radius");
    
    return (

        <>
            { toolActive && (
                <>
                    { ToolMode == WaterToolModes.PlaceWaterSource && ( 
                        <VanillaComponentResolver.instance.Section title={amountSection}>
                            <>                            
                                {AmountIsElevation && (
                                    <VanillaComponentResolver.instance.ToolButton 
                                        className={VanillaComponentResolver.instance.toolButtonTheme.button} 
                                        tooltip={translate("Water_Features.TOOLTIP_DESCRIPTION[AmountIsElevation]" ,locale["Water_Features.TOOLTIP_DESCRIPTION[AmountIsElevation]"])} 
                                        onSelect={() => handleClick("AmountIsElevationToggled")} 
                                        src={amountIsElevationSrc}
                                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                        selected={AmountIsElevation}
                                    ></VanillaComponentResolver.instance.ToolButton>
                                )}
                                <VanillaComponentResolver.instance.ToolButton 
                                    className={VanillaComponentResolver.instance.mouseToolOptionsTheme.startButton} 
                                    tooltip={amountDownTooltip} 
                                    onSelect={() => handleClick(amountDownID)} 
                                    src={arrowDownSrc}
                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                ></VanillaComponentResolver.instance.ToolButton>
                                <div className={VanillaComponentResolver.instance.mouseToolOptionsTheme.numberField}>{amountIsFlow ? AmountValue.toFixed(AmountScale) : AmountValue.toFixed(AmountScale) + " m"}</div>
                                <VanillaComponentResolver.instance.ToolButton 
                                    className={VanillaComponentResolver.instance.mouseToolOptionsTheme.endButton} 
                                    tooltip={amountUpTooltip} 
                                    onSelect={() => handleClick(amountUpID)} 
                                    src={arrowUpSrc}
                                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                ></VanillaComponentResolver.instance.ToolButton>
                                <VanillaComponentResolver.instance.StepToolButton tooltip={amountStepTooltip} onSelect={() => handleClick(amountStepID)} values={defaultValues} selectedValue={AmountStep}></VanillaComponentResolver.instance.StepToolButton>
                                </>
                        </VanillaComponentResolver.instance.Section>
                    )}
                    { ShowMinDepth && ToolMode == WaterToolModes.PlaceWaterSource && ( 
                        // This section is only shown if binding says so.
                        <VanillaComponentResolver.instance.Section title={minDepthSection}>
                            <VanillaComponentResolver.instance.ToolButton 
                                className={VanillaComponentResolver.instance.mouseToolOptionsTheme.startButton} 
                                tooltip={minDepthDownTooltip} 
                                onSelect={() => handleClick(minDepthDownID)} 
                                src={arrowDownSrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                            ></VanillaComponentResolver.instance.ToolButton>
                            <div className={VanillaComponentResolver.instance.mouseToolOptionsTheme.numberField}>{MinDepthValue.toFixed(MinDepthScale) + " m"}</div>
                            <VanillaComponentResolver.instance.ToolButton 
                                className={VanillaComponentResolver.instance.mouseToolOptionsTheme.endButton} 
                                tooltip={minDepthUpTooltip} 
                                onSelect={() => handleClick(minDepthUpID)} 
                                src={arrowUpSrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                            ></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.StepToolButton tooltip={minDepthStepTooltip} onSelect={() => handleClick(minDepthStepID)} values={defaultValues} selectedValue={MinDepthStep}></VanillaComponentResolver.instance.StepToolButton>
                        </VanillaComponentResolver.instance.Section>
                    )}
                    { ToolMode == WaterToolModes.PlaceWaterSource && (
                        <VanillaComponentResolver.instance.Section title={radiusSection}>
                            <VanillaComponentResolver.instance.ToolButton 
                                className={VanillaComponentResolver.instance.mouseToolOptionsTheme.startButton} 
                                tooltip={radiusDownTooltip} 
                                onSelect={() => handleClick(radiusDownID)} 
                                src={arrowDownSrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                            ></VanillaComponentResolver.instance.ToolButton>
                            <div className={VanillaComponentResolver.instance.mouseToolOptionsTheme.numberField}>{RadiusValue.toFixed(RadiusScale) + " m"}</div>
                            <VanillaComponentResolver.instance.ToolButton 
                                className={VanillaComponentResolver.instance.mouseToolOptionsTheme.endButton} 
                                tooltip={radiusUpTooltip} 
                                onSelect={() => handleClick(radiusUpID)} 
                                src={arrowUpSrc}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                            ></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.StepToolButton tooltip={radiusStepTooltip} onSelect={() => handleClick(radiusStepID)} values={defaultValues} selectedValue={RadiusStep}></VanillaComponentResolver.instance.StepToolButton>
                        </VanillaComponentResolver.instance.Section>
                    )}
                    <VanillaComponentResolver.instance.Section title={toolModeTitle}>
                            <VanillaComponentResolver.instance.ToolButton  selected={ToolMode == WaterToolModes.PlaceWaterSource}    tooltip={descriptionTooltip(placeWaterSourceTitle, placeWaterSourceTooltip)}  onSelect={() => changeToolMode(WaterToolModes.PlaceWaterSource)}   src={placeWaterSourceSrc} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={ToolMode == WaterToolModes.ElevationChange}     tooltip={descriptionTooltip(elevationChangeTitle, elevationChangeTooltip)}     onSelect={() => changeToolMode(WaterToolModes.ElevationChange)}    src={elevationChangeSrc}  focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={ToolMode == WaterToolModes.MoveWaterSource}     tooltip={descriptionTooltip(moveWaterSourceTitle, moveWaterSourceTooltip)}     onSelect={() => changeToolMode(WaterToolModes.MoveWaterSource)}    src={moveWaterSourceSrc}  focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={ToolMode == WaterToolModes.RadiusChange}        tooltip={descriptionTooltip(radiusChangeTitle, radiusChangeTooltip)}           onSelect={() => changeToolMode(WaterToolModes.RadiusChange)}       src={radiusChangeSrc}     focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                    </VanillaComponentResolver.instance.Section>        
                </>
             )}            
        </>
    );
}