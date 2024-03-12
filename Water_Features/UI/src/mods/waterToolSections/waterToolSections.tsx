import {ModuleRegistryExtend} from "cs2/modding";
import { bindValue, trigger, useValue } from "cs2/api";
import { tool } from "cs2/bindings";
import mod from "../../../mod.json";
import { VanillaComponentResolver } from "../VanillaComponentResolver/VanillaComponentResolver";
import arrowDownSrc from "./ArrowDownThickStrokeWT.svg";
import arrowUpSrc from "./ArrowUpThickStrokeWT.svg";
import { useLocalization } from "cs2/l10n";

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

// These are strings that will be used for translations keys and event triggers.
export const amountDownID =     "amount-down-arrow";
export const amountUpID =       "amount-up-arrow";
export const radiusDownID =     "radius-down-arrow";
export const radiusUpID =       "radius-up-arrow";
export const minDepthDownID =   "min-depth-down-arrow";
export const minDepthUpID =     "min-depth-up-arrow";
export const amountStepID =     "amount-rate-of-change";
export const radiusStepID =     "radius-rate-of-change";
export const minDepthStepID =   "min-depth-rate-of-change";
export const tooltipDescriptionPrefix ="YY_WATER_FEATURES_DESCRIPTION.";
export const sectionTitlePrefix =      "YY_WATER_FEATURES."; 

// Stores the default values for the step arrays. Must be descending order.
export const defaultValues : number[] =[1.0, 0.5, 0.25, 0.125];

// This functions trigger an event on C# side and C# designates the method to implement.
export function handleClick(eventName: string) {
    trigger(mod.id, eventName);
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

        // Gets a boolean for whether the amount is a flow.
        const amountIsFlow : boolean = AmountLocaleKey == "YY_WATER_FEATURES.Flow";

        // translation handling. Translates using locale keys that are defined in C# or fallback string here.
        const { translate } = useLocalization();
        const amountDownTooltip =   translate(tooltipDescriptionPrefix + amountDownID,      "Reduces the flow for Streams. Decreases the depth or elevation for rivers, seas, and lakes. Reduces the max depth for retention and detention basins.");
        const amountUpTooltip =     translate(tooltipDescriptionPrefix + amountUpID,        "Increases the flow for Streams. Increases the depth or elevation for rivers, seas, and lakes. Increases the max depth for retention and detention basins.");
        const radiusDownTooltip =   translate(tooltipDescriptionPrefix + radiusDownID,      "Reduces the radius.");
        const radiusUpTooltip =     translate(tooltipDescriptionPrefix + radiusUpID,        "Increases the radius.");
        const minDepthDownTooltip = translate(tooltipDescriptionPrefix + minDepthDownID,    "Reduces the minimum depth.");
        const minDepthUpTooltip =   translate(tooltipDescriptionPrefix + minDepthUpID,      "Increases the minimum depth.");
        const amountStepTooltip =   translate(tooltipDescriptionPrefix + amountStepID,      "Changes the rate in which the increase and decrease buttons work for Flow, Depth and Elevation.");
        const radiusStepTooltip =   translate(tooltipDescriptionPrefix + radiusStepID,      "Changes the rate in which the increase and decrease buttons work for Radius.");
        const minDepthStepTooltip = translate(tooltipDescriptionPrefix + minDepthStepID,    "Changes the rate in which the increase and decrease buttons work for minimum depth.");
        const minDepthSection =     translate(sectionTitlePrefix + "MinDepth",              "Min Depth");
        const radiusSection =       translate(sectionTitlePrefix + "Radius",                "Radius");
        const amountSection =       translate(AmountLocaleKey,                              "Depth");
        
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
                    { ShowMinDepth ? 
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
                    : <></>
                    }
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
                </>
            );
        }
        return result;
    };
}