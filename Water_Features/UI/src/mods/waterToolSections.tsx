import { useModding } from "modding/modding-context";
import { ModuleRegistry } from "modding/types";
import { MouseEvent, useCallback } from "react";

export const unselectedImageSource : string = "coui://uil/Standard/Anarchy.svg";
export const selectedImageSource : string = "coui://uil/Colored/Anarchy.svg";

export const WaterToolComponent = (moduleRegistry: ModuleRegistry) => (Component: any) => {
    // The module registrys are found by logging console.log('mr', moduleRegistry); in the index file and finding appropriate one.
    const toolMouseModule = moduleRegistry.registry.get("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx");
    const toolButtonModule = moduleRegistry.registry.get("game-ui/game/components/tool-options/tool-button/tool-button.tsx")!!;
    const theme = moduleRegistry.registry.get("game-ui/game/components/tool-options/tool-button/tool-button.module.scss")?.classes;
    const mouseToolTheme = moduleRegistry.registry.get("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.module.scss")?.classes;
    // These are found in the minified JS file after searching for module.
    const Section: any = toolMouseModule?.Section;
    const StepToolButton: any = toolButtonModule?.StepToolButton;
    const ValueToolButton: any = toolButtonModule?.ValueToolButton;

    return (props: any) => {
        const { children, ...otherProps} = props || {};
        const { api: { api: { useValue, bindValue, trigger } } } = useModding();
        const { engine } = useModding();

        console.log("Hello");
        // These establish the binding with C# side. Without C# side game ui will crash.

        // This binding is for whether the tool is active.
        // const toolActive$ = bindValue<boolean>('WaterTool', 'ToolActive');
        // const toolActive = useValue(toolActive$);

        // This binding is for what amount to show in Amount field.
        // const AmountValue$ = bindValue<Number> ('WaterTool', 'AmountValue');
        // const AmountValue = useValue(AmountValue$);


        const handleClick = useCallback ((ev: MouseEvent<HTMLButtonElement>) => {
            // This triggers an event on C# side and C# designates the method to implement.
            trigger("Anarchy", "AnarchyToggled");
        }, [])
        
        // This will return original component and children if there is nothing to insert.
         /*if (!toolActive) {
            return (
                <Component {...otherProps}>
                    {children}
                </Component>
            );
            }*/
        
        var result = Component();
        result.props.children?.unshift(
            /* 
            Add a new section before other tool options sections with translated title based of this localization key. Localization key defined in C#.
            Add a new Tool button into that section. Selected is based on Anarchy Enabled binding. 
            Tooltip is translated based on localization key. OnSelect run callback fucntion here to trigger event. 
            Anarchy specific image source changes bases on Anarchy Enabled binding. 
            */
            <Section title="Flow">
                <ValueToolButton className = {mouseToolTheme.numberField} value = "1"></ValueToolButton>
            </Section>)
        return <>{result}</>;
    };
}
