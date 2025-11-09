import {ModuleRegistryExtend} from "cs2/modding";
import { CommonWaterToolSections } from "mods/CommonWaterToolSections/commonWaterToolSections";


export const WaterToolComponent: ModuleRegistryExtend = (Component : any) => {
    // I believe you should not put anything here.
    return (props) => {
        const {children, ...otherProps} = props || {};
        var result = Component();
        result.props.children?.unshift
        (
            <>
                <CommonWaterToolSections></CommonWaterToolSections>          
            </>
        );
        return result;
    };
}