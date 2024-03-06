import { useModding } from "modding/modding-context";
import { ModuleRegistry } from "modding/types";
import { MouseEvent, useCallback } from "react";

export const WaterToolComponent = (moduleRegistry: ModuleRegistry) => (Component: any) => {
    // The module registrys are found by logging console.log('mr', moduleRegistry); in the index file and finding appropriate one.
    const toolMouseModule = moduleRegistry.registry.get("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx");
    const toolButtonModule = moduleRegistry.registry.get("game-ui/game/components/tool-options/tool-button/tool-button.tsx")!!;
    const mouseToolTheme = moduleRegistry.registry.get("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.module.scss")?.classes;
    // These are found in the minified JS file after searching for module.
    const Section: any = toolMouseModule?.Section;
    const StepToolButton: any = toolButtonModule?.StepToolButton;
    const ToolButton: any = toolButtonModule?.ToolButton;

    return (props: any) => {
        const { children, ...otherProps} = props || {};
        const { api: { api: { useValue, bindValue, trigger } } } = useModding();
        const { engine } = useModding();

        // These establish the binding with C# side. Without C# side game ui will crash.

        // This binding is for whether the Water tool is active.
        const toolActive$ = bindValue<boolean>('WaterTool', 'ToolActive');
        const toolActive = useValue(toolActive$);

        // This binding is for what value to show in Amount field.
        const AmountValue$ = bindValue<number> ('WaterTool', 'AmountValue');
        const AmountValue = useValue(AmountValue$);

        // This binding is for what value to show in Radius field.
        const RadiusValue$ = bindValue<number> ('WaterTool', 'RadiusValue');
        const RadiusValue = useValue(RadiusValue$);

        // This binding is for what value to show in Min Depth field.
        const MinDepthValue$ = bindValue<number> ('WaterTool', 'MinDepthValue');
        const MinDepthValue = useValue(MinDepthValue$);

        // This binding is for what value to show in Min Depth field.
        const AmountLocaleKey$ = bindValue<string> ('WaterTool', 'AmountLocaleKey');
        const AmountLocaleKey = useValue(AmountLocaleKey$);

        // This binding is for what selected value of the Amount step.
        const AmountStep$ = bindValue<number> ('WaterTool', 'AmountStep');
        const AmountStep = useValue(AmountStep$);

        // This binding is for what selected value of the Radius step.
        const RadiusStep$ = bindValue<number> ('WaterTool', 'RadiusStep');
        const RadiusStep = useValue(RadiusStep$);

         // This binding is for what selected value of the Min Depth step.
        const MinDepthStep$ = bindValue<number> ('WaterTool', 'MinDepthStep');
        const MinDepthStep = useValue(MinDepthStep$);

        // This binding is for what scale to round Amount.
        const AmountScale$ = bindValue<number> ('WaterTool', 'AmountScale');
        const AmountScale = useValue(AmountScale$);

        // This binding is for what scale to round Radius.
        const RadiusScale$ = bindValue<number> ('WaterTool', 'RadiusScale');
        const RadiusScale = useValue(RadiusScale$);

        // This binding is for what scale to round Min Depth.
        const MinDepthScale$ = bindValue<number> ('WaterTool', 'MinDepthScale');
        const MinDepthScale = useValue(MinDepthScale$);

        // This binding is for whether to show Min Depth.
        const ShowMinDepth$ = bindValue<number> ('WaterTool', 'ShowMinDepth');
        const ShowMinDepth = useValue(ShowMinDepth$);

        const increaseAmount = useCallback ((ev: MouseEvent<HTMLButtonElement>) => {
            // This triggers an event on C# side and C# designates the method to implement.
            trigger("WaterTool", "IncreaseAmount");
        }, []);

        const decreaseAmount = useCallback ((ev: MouseEvent<HTMLButtonElement>) => {
            // This triggers an event on C# side and C# designates the method to implement.
            trigger("WaterTool", "DecreaseAmount");
        }, []);

        const amountStepPressed = useCallback ((ev: MouseEvent<HTMLButtonElement>) => {
            // This triggers an event on C# side and C# designates the method to implement.
            trigger("WaterTool", "AmountStepPressed");
        }, []);

        const increaseMinDepth = useCallback ((ev: MouseEvent<HTMLButtonElement>) => {
            // This triggers an event on C# side and C# designates the method to implement.
            trigger("WaterTool", "IncreaseMinDepth");
        }, []);

        const decreaseMinDepth = useCallback ((ev: MouseEvent<HTMLButtonElement>) => {
            // This triggers an event on C# side and C# designates the method to implement.
            trigger("WaterTool", "DecreaseMinDepth");
        }, []);

        const minDepthStepPressed = useCallback ((ev: MouseEvent<HTMLButtonElement>) => {
            // This triggers an event on C# side and C# designates the method to implement.
            trigger("WaterTool", "MinDepthStepPressed");
        }, []);

        const increaseRadius = useCallback ((ev: MouseEvent<HTMLButtonElement>) => {
            // This triggers an event on C# side and C# designates the method to implement.
            trigger("WaterTool", "IncreaseRadius");
        }, []);

        const decreaseRadius = useCallback ((ev: MouseEvent<HTMLButtonElement>) => {
            // This triggers an event on C# side and C# designates the method to implement.
            trigger("WaterTool", "DecreaseRadius");
        }, []);

        const radiusStepPressed = useCallback ((ev: MouseEvent<HTMLButtonElement>) => {
            // This triggers an event on C# side and C# designates the method to implement.
            trigger("WaterTool", "RadiusStepPressed");
        }, []);

        const amountIsFlow : boolean = AmountLocaleKey == "YY_WATER_FEATURES.Flow";

        var result = Component();
        if (toolActive) 
        {
            result.props.children?.unshift
            (
                /* 
                Add a new section before other tool options sections with translated title based of localization key from binding. Localization key defined in C#.
                Adds up and down buttons and field with step button. All buttons have translated tooltips. OnSelect triggers C# events. Src paths are from UIL.
                values must be decending. SelectedValue is from binding. 
                */
                <>
                    <Section title={engine.translate(AmountLocaleKey)}>
                        <ToolButton className={mouseToolTheme.startButton} tooltip={engine.translate("YY_WATER_FEATURES_DESCRIPTION.amount-down-arrow")} onSelect={decreaseAmount} src="coui://uil/Standard/ArrowDownThickStroke.svg"></ToolButton>
                        <div className={mouseToolTheme.numberField}>{amountIsFlow ? AmountValue.toFixed(AmountScale) : AmountValue.toFixed(AmountScale) + " m"}</div>
                        <ToolButton className={mouseToolTheme.endButton} tooltip={engine.translate("YY_WATER_FEATURES_DESCRIPTION.amount-up-arrow")} onSelect={increaseAmount} src="coui://uil/Standard/ArrowUpThickStroke.svg"></ToolButton>
                        <StepToolButton class name={mouseToolTheme.indicator} tooltip={engine.translate("YY_WATER_FEATURES_DESCRIPTION.amount-rate-of-change")} onSelect={amountStepPressed} values={[1.0, 0.5, 0.25, 0.125]} selectedValue={AmountStep}></StepToolButton>
                    </Section>
                    { ShowMinDepth ? 
                    // This section is only shown if binding says so.
                    <Section title={engine.translate("YY_WATER_FEATURES.MinDepth")}>
                        <ToolButton className={mouseToolTheme.startButton} tooltip={engine.translate("YY_WATER_FEATURES_DESCRIPTION.min-depth-down-arrow")} onSelect={decreaseMinDepth} src="coui://uil/Standard/ArrowDownThickStroke.svg"></ToolButton>
                        <div className={mouseToolTheme.numberField}>{MinDepthValue.toFixed(MinDepthScale) + " m"}</div>
                        <ToolButton className={mouseToolTheme.endButton} tooltip={engine.translate("YY_WATER_FEATURES_DESCRIPTION.min-depth-up-arrow")} onSelect={increaseMinDepth} src="coui://uil/Standard/ArrowUpThickStroke.svg"></ToolButton>
                        <StepToolButton class name={mouseToolTheme.indicator} tooltip={engine.translate("YY_WATER_FEATURES_DESCRIPTION.min-depth-rate-of-change")} onSelect={minDepthStepPressed} values={[1.0, 0.5, 0.25, 0.125]} selectedValue={MinDepthStep}></StepToolButton>
                    </Section> 
                    
                    : <></>
                    }
                    
                    <Section title={engine.translate("YY_WATER_FEATURES.Radius")}>
                        <ToolButton className={mouseToolTheme.startButton} tooltip={engine.translate("YY_WATER_FEATURES_DESCRIPTION.radius-down-arrow")} onSelect={decreaseRadius} src="coui://uil/Standard/ArrowDownThickStroke.svg"></ToolButton>
                        <div className={mouseToolTheme.numberField}>{RadiusValue.toFixed(RadiusScale) + " m"}</div>
                        <ToolButton className={mouseToolTheme.endButton} tooltip={engine.translate("YY_WATER_FEATURES_DESCRIPTION.radius-up-arrow")} onSelect={increaseRadius} src="coui://uil/Standard/ArrowUpThickStroke.svg"></ToolButton>
                        <StepToolButton class name={mouseToolTheme.indicator} tooltip={engine.translate("YY_WATER_FEATURES_DESCRIPTION.radius-rate-of-change")} onSelect={radiusStepPressed} values={[1.0, 0.5, 0.25, 0.125]} selectedValue={RadiusStep}></StepToolButton>
                    </Section>
                </>
            );
        }
        return result;
    };
}
