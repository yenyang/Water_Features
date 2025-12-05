import { bindValue, trigger, useValue } from "cs2/api";
import {getModule, ModuleRegistryExtend} from "cs2/modding";
import { CommonWaterToolSections, descriptionTooltip } from "mods/CommonWaterToolSections/commonWaterToolSections";
import mod from "../../../mod.json";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import styles from "../waterToolSections/waterToolSections.module.scss";
import { useState } from "react";
import { tool } from "cs2/bindings";
import locale from "../../lang/en-US.json";
import { useLocalization } from "cs2/l10n";

const SeaLevel$ =           bindValue<number>(mod.id, 'SeaLevel');
const SeaLevelLocked$ =     bindValue<boolean>(mod.id, 'SeaLevelLocked');
const LegacyWaterSources$ = bindValue<boolean>(mod.id, 'LegacyWaterSources');
const SeaLevelSliderRange$ = bindValue<number>(mod.id, 'SeaLevelSliderRange');

const SliderField : any = getModule("game-ui/editor/widgets/fields/number-slider-field.tsx", "FloatSliderField");

const uilStandard =                         "coui://uil/Standard/";
const lockedSrc =     uilStandard + "LockClosed.svg";
const unlockedSrc =     uilStandard + "LockOpen.svg";
const arrowDownSrc =         uilStandard +  "ArrowDownThickStroke.svg";
const arrowUpSrc =           uilStandard +  "ArrowUpThickStroke.svg";


export const WaterToolComponent: ModuleRegistryExtend = (Component : any) => {
    // I believe you should not put anything here.
    return (props) => {
        const {children, ...otherProps} = props || {};

        const toolActive = useValue(tool.activeTool$).id == "Yenyang's Water Tool";

        const LegacyWaterSources = useValue(LegacyWaterSources$);
        const SeaLevel = useValue(SeaLevel$);
        const SeaLevelLocked = useValue(SeaLevelLocked$);
        const SeaLevelSliderRange = useValue(SeaLevelSliderRange$);

        const { translate } = useLocalization();
        
        let [StartingSeaLevel, setStartingSeaLevel] = useState(SeaLevel);
        let [PreviousSeaLevelSliderRange, setPreviousSeaLevelSliderRange] = useState(SeaLevelSliderRange);
        
        if (PreviousSeaLevelSliderRange !== SeaLevelSliderRange) 
        {
            setPreviousSeaLevelSliderRange(SeaLevelSliderRange);
            let nextStartingSeaLevel = SeaLevel;
            if (nextStartingSeaLevel - SeaLevelSliderRange <= 0) 
            {
                nextStartingSeaLevel = SeaLevelSliderRange/2;
            }

            if (nextStartingSeaLevel + SeaLevelSliderRange > 2000) 
            {
                nextStartingSeaLevel = 2000 - SeaLevelSliderRange / 2;
            }

            setStartingSeaLevel(nextStartingSeaLevel);
        }

        var result = Component();
        result.props.children?.unshift
        (
            <>
                <CommonWaterToolSections></CommonWaterToolSections>                     
                    {LegacyWaterSources == false && toolActive && (
                        <>
                            <VanillaComponentResolver.instance.Section title={translate("Water_Features.SECTION_TITLE[SeaLevel]", locale["Water_Features.SECTION_TITLE[SeaLevel]"])}>   
                                <div className={styles.rowGroup}>
                                    {!SeaLevelLocked ?
                                        <>
                                            <div className={styles.SliderFieldWidth}>
                                                <SliderField 
                                                    value={SeaLevel} 
                                                    min={Math.max(0, StartingSeaLevel-SeaLevelSliderRange/2)} 
                                                    max={Math.min(2000, StartingSeaLevel+SeaLevelSliderRange/2)} 
                                                    fractionDigits={1} 
                                                    onChange={(e: number) => { trigger(mod.id, "SetSeaLevel", e);}}
                                                ></SliderField>
                                            </div>
                                            <span className={styles.smallSpacer}></span>
                                        </>
                                        :
                                        <div className={VanillaComponentResolver.instance.mouseToolOptionsTheme.numberField}>{Math.round(SeaLevel*10)/10}</div>
                                    }
                                    <VanillaComponentResolver.instance.ToolButton
                                        className={SeaLevelLocked ? VanillaComponentResolver.instance.mouseToolOptionsTheme.endButton : VanillaComponentResolver.instance.toolButtonTheme.button} 
                                        tooltip={descriptionTooltip(translate("Water_Features.TOOLTIP_TITLE[LockSeaLevel]", locale["Water_Features.TOOLTIP_TITLE[LockSeaLevel]"]), translate( "Water_Features.TOOLTIP_DESCRIPTION[LockSeaLevel]", locale["Water_Features.TOOLTIP_DESCRIPTION[LockSeaLevel]"]))}
                                        onSelect={() => trigger(mod.id, "ToggleSeaLevelLock")} 
                                        src={ SeaLevelLocked ?  lockedSrc : unlockedSrc }
                                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}>                          
                                    </VanillaComponentResolver.instance.ToolButton>
                                </div>
                            </VanillaComponentResolver.instance.Section>
                            { !SeaLevelLocked && (
                                <VanillaComponentResolver.instance.Section title={locale["Water_Features.SECTION_TITLE[SliderRange]"]}>
                                        <div className={styles.rowGroup}>              
                                            <VanillaComponentResolver.instance.ToolButton
                                                className={VanillaComponentResolver.instance.mouseToolOptionsTheme.startButton} 
                                                disabled={SeaLevelSliderRange <= 10}
                                                tooltip={translate("Water_Features.TOOLTIP_TITLE[DecreaseSeaLevelSliderRange]", locale["Water_Features.TOOLTIP_TITLE[DecreaseSeaLevelSliderRange]"])} 
                                                onSelect={() => trigger(mod.id, "DecreaseSeaLevelSliderRange")} 
                                                src={arrowDownSrc}
                                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}>                          
                                            </VanillaComponentResolver.instance.ToolButton>
                                            <div className={VanillaComponentResolver.instance.mouseToolOptionsTheme.numberField}>{SeaLevelSliderRange}</div>
                                            <VanillaComponentResolver.instance.ToolButton
                                                className={VanillaComponentResolver.instance.mouseToolOptionsTheme.endButton} 
                                                disabled={SeaLevelSliderRange >= 2000}
                                                tooltip={translate("Water_Features.TOOLTIP_TITLE[IncreaseSeaLevelSliderRange]", locale["Water_Features.TOOLTIP_TITLE[IncreaseSeaLevelSliderRange]"])} 
                                                onSelect={() => trigger(mod.id, "IncreaseSeaLevelSliderRange")} 
                                                src={arrowUpSrc}
                                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}>                          
                                            </VanillaComponentResolver.instance.ToolButton>                                              
                                        </div>
                                </VanillaComponentResolver.instance.Section>      
                            )}
                        </>
                    )}            
            </>
        );
        return result;
    };
}