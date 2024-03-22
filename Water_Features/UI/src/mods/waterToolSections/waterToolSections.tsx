import {ModuleRegistryExtend} from "cs2/modding";
import { bindValue, trigger, useValue } from "cs2/api";
import { tool } from "cs2/bindings";
import mod from "../../../mod.json";
import { VanillaComponentResolver } from "../VanillaComponentResolver/VanillaComponentResolver";
import { useLocalization } from "cs2/l10n";

// These contain the coui paths to Unified Icon Library svg assets
export const couiStandard =                         "coui://uil/Standard/";
export const arrowDownSrc =         couiStandard +  "ArrowDownThickStroke.svg";
export const arrowUpSrc =           couiStandard +  "ArrowUpThickStroke.svg";
export const elevationChangeSrc =   couiStandard +  "ArrowUp.svg";
export const placeWaterSourceSrc =  couiStandard +  "Dot.svg";
export const moveWaterSourceSrc =   couiStandard +  "BoxArrowBoxAdjustEndProp.svg";
export const radiusChangeSrc    =   couiStandard +  "Circle.svg"

// These establishes the binding with C# side. Without C# side game ui will crash.
export const AmountValue$ =        bindValue<number> (mod.id, 'AmountValue');
export const RadiusValue$ =        bindValue<number> (mod.id, 'RadiusValue');
export const MinDepthValue$ =      bindValue<number> (mod.id, 'MinDepthValue');
export const AmountLocaleKey$ =    bindValue<string> (mod.id, 'AmountLocaleKey');
export const AmountStep$ =         bindValue<number> (mod.id, 'AmountStep');
export const RadiusStep$ =         bindValue<number> (mod.id, 'RadiusStep');
export const MinDepthStep$ =       bindValue<number> (mod.id, 'MinDepthStep');
export const AmountScale$ =        bindValue<number> (mod.id, 'AmountScale');
export const RadiusScale$ =        bindValue<number> (mod.id, 'RadiusScale');
export const MinDepthScale$ =      bindValue<number> (mod.id, 'MinDepthScale');
export const ShowMinDepth$ =       bindValue<number> (mod.id, 'ShowMinDepth');
export const ToolMode$ =           bindValue<number> (mod.id, "ToolMode");

// These are strings that will be used for translations keys and event triggers.
export const amountDownID =             "amount-down-arrow";
export const amountUpID =               "amount-up-arrow";
export const radiusDownID =             "radius-down-arrow";
export const radiusUpID =               "radius-up-arrow";
export const minDepthDownID =           "min-depth-down-arrow";
export const minDepthUpID =             "min-depth-up-arrow";
export const amountStepID =             "amount-rate-of-change";
export const radiusStepID =             "radius-rate-of-change";
export const minDepthStepID =           "min-depth-rate-of-change";
export const tooltipDescriptionPrefix = "YY_WATER_FEATURES_DESCRIPTION.";
export const sectionTitlePrefix =       "YY_WATER_FEATURES.";
export const elevationChangeID =        "ElevationChange";
export const placeWaterSourceID =       "PlaceWaterSource";
export const moveWaterSourceID =        "MoveWaterSource";
export const radiusChangeID =           "RadiusChange";

// Stores the default values for the step arrays. Must be descending order.
export const defaultValues : number[] =[1.0, 0.5, 0.25, 0.125];

// This functions trigger an event on C# side and C# designates the method to implement.
export function handleClick(eventName: string) {
    trigger(mod.id, eventName);
}

// This function triggers an event to change the water tool mode to specified tool mode.
export function changeToolMode(toolMode: WaterToolModes) {
    trigger(mod.id, "ChangeToolMode", toolMode);
}

export enum WaterToolModes 
{
    PlaceWaterSource,
    ElevationChange,
    MoveWaterSource,
    RadiusChange,
}

export const WaterToolComponent: ModuleRegistryExtend = (Component : any) => {
    // I believe you should not put anything here.
    return (props) => {
        const {children, ...otherProps} = props || {};

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

        var result = Component();
        if (toolActive) 
        {
            result.props.children?.unshift
            (
                /* 
                Add a new section before other tool options sections with translated title based of localization key from binding. Localization key defined in C#.
                Adds up and down buttons and field with step button. All buttons have translated tooltips. OnSelect triggers C# events. Src paths are local imports.
                values must be decending. SelectedValue is from binding. 
                */
                <>
                    { ToolMode == WaterToolModes.PlaceWaterSource && ( 
                        <VanillaComponentResolver.instance.Section title={amountSection}>
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
                            <VanillaComponentResolver.instance.ToolButton  selected={ToolMode == WaterToolModes.PlaceWaterSource}    tooltip={placeWaterSourceTooltip}  onSelect={() => changeToolMode(WaterToolModes.PlaceWaterSource)}   src={placeWaterSourceSrc} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={ToolMode == WaterToolModes.ElevationChange}     tooltip={elevationChangeTooltip}   onSelect={() => changeToolMode(WaterToolModes.ElevationChange)}    src={elevationChangeSrc}  focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={ToolMode == WaterToolModes.MoveWaterSource}     tooltip={moveWaterSourceTooltip}   onSelect={() => changeToolMode(WaterToolModes.MoveWaterSource)}    src={moveWaterSourceSrc}  focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={ToolMode == WaterToolModes.RadiusChange}        tooltip={radiusChangeTooltip}      onSelect={() => changeToolMode(WaterToolModes.RadiusChange)}       src={radiusChangeSrc}     focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                    </VanillaComponentResolver.instance.Section>                    
                </>
            );
        }
        return result;
    };
}