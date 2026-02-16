using CounterStrikeSharp.API.Core;

namespace ArcadeScripts.Scripts;

public partial class Tapper
{
    private List<CBaseDoor> TapperTap = [];
    private List<CBaseButton> TapperTapUpgrades = [];
    private List<CBaseButton> TapperTapButtonUpgrades = [];
    private CTimerEntity TapperAutotapper = null!;
    private CAmbientGeneric TapperStartSound = null!;
    private CAmbientGeneric TapperWarmupSound = null!;
    private CPointWorldText[] TapperRankText = new CPointWorldText[NumBooths];
    private CFuncMoveLinear[] TapperPlatforms = new CFuncMoveLinear[4];
    private CPointWorldText[] TapperTimerText = new CPointWorldText[5];
    private CPointWorldText[] TapperScoreText = new CPointWorldText[5];
    private CPointWorldText[] TapperScoreTextOuter = new CPointWorldText[5];
    private CDynamicProp[] TapperTapUpgradeModels0 = new CDynamicProp[5];
    private CDynamicProp[] TapperTapUpgradeModels1 = new CDynamicProp[5];
    private CDynamicProp[] TapperTapUpgradeModels2 = new CDynamicProp[5];
    private CDynamicProp[] TapperTapUpgradeModels3 = new CDynamicProp[5];
    private CDynamicProp[][] TapperTapUpgradeModels = new CDynamicProp[4][];
    private CDynamicProp[] TapperTapButtonUpgradeModels0 = new CDynamicProp[3];
    private CDynamicProp[] TapperTapButtonUpgradeModels1 = new CDynamicProp[3];
    private CDynamicProp[] TapperTapButtonUpgradeModels2 = new CDynamicProp[3];
    private CDynamicProp[] TapperTapButtonUpgradeModels3 = new CDynamicProp[3];
    private CDynamicProp[][] TapperTapButtonUpgradeModels = new CDynamicProp[4][];

    public void OnAllEntitiesSpawned()
    {
        foreach (KeyValuePair<string, List<CEntityInstance>> kv in EntityList)
        {
            List<CEntityInstance> entities = kv.Value;
            string targetname = kv.Key;
            string classname = entities[0].DesignerName;

            if (classname == "func_door" && kv.Key.StartsWith("tapper_tap"))
            {
                for (int i = 0; i < entities.Count; i++) TapperTap.Add(entities[i].As<CBaseDoor>());
            }
            else if (classname == "func_button" && targetname.StartsWith("tapper_tap_upgrade"))
            {
                for (int i = 0; i < entities.Count; i++) TapperTapUpgrades.Add(entities[i].As<CBaseButton>());
            }
            else if (classname == "func_button" && targetname.StartsWith("tapper_tap_buttonupgrade"))
            {
                for (int i = 0; i < entities.Count; i++) TapperTapButtonUpgrades.Add(entities[i].As<CBaseButton>());
            }
            else if (targetname.StartsWith("tapper_rank_text"))
            {
                if (int.TryParse(targetname.Replace("tapper_rank_text", ""), out int index))
                {
                    TapperRankText[index] = entities[0].As<CPointWorldText>();
                }
            }
            else if (targetname.StartsWith("tapper_platform"))
            {
                if (int.TryParse(targetname.Replace("tapper_platform", ""), out int index))
                {
                    TapperPlatforms[index] = entities[0].As<CFuncMoveLinear>();
                }
            }
            else if (targetname.StartsWith("tapper_timer_text"))
            {
                if (int.TryParse(targetname.Replace("tapper_timer_text", ""), out int index))
                {
                    TapperTimerText[index] = entities[0].As<CPointWorldText>();
                }
            }
            else if (targetname.StartsWith("tapper_score_outer"))
            {
                if (int.TryParse(targetname.Replace("tapper_score_outer", ""), out int index))
                {
                    TapperScoreTextOuter[index] = entities[0].As<CPointWorldText>();
                }
            }
            else if (targetname.StartsWith("tapper_score"))
            {
                if (int.TryParse(targetname.Replace("tapper_score", ""), out int index))
                {
                    TapperScoreText[index] = entities[0].As<CPointWorldText>();
                }
            }
            else if (targetname == "tapper_autotapper")
            {
                TapperAutotapper = entities[0].As<CTimerEntity>();
            }
            else if (targetname == "tapper_start_sound")
            {
                TapperStartSound = entities[0].As<CAmbientGeneric>();
            }
            else if (targetname == "tapper_warmup_sound")
            {
                TapperWarmupSound = entities[0].As<CAmbientGeneric>();
            }
            else if (targetname.StartsWith("tapper_tap_upgrade"))
            {
                if (targetname.EndsWith("0_model"))
                {
                    if (int.TryParse(targetname.Replace("tapper_tap_upgrade", "").Replace("_0_model", ""), out int upgradeNumber))
                    {
                        TapperTapUpgradeModels0[upgradeNumber] = entities[0].As<CDynamicProp>();
                    }
                }
                else if (targetname.EndsWith("1_model"))
                {
                    if (int.TryParse(targetname.Replace("tapper_tap_upgrade", "").Replace("_1_model", ""), out int upgradeNumber))
                    {
                        TapperTapUpgradeModels1[upgradeNumber] = entities[0].As<CDynamicProp>();
                    }
                }
                else if (targetname.EndsWith("2_model"))
                {
                    if (int.TryParse(targetname.Replace("tapper_tap_upgrade", "").Replace("_2_model", ""), out int upgradeNumber))
                    {
                        TapperTapUpgradeModels2[upgradeNumber] = entities[0].As<CDynamicProp>();
                    }
                }
                else if (targetname.EndsWith("3_model"))
                {
                    if (int.TryParse(targetname.Replace("tapper_tap_upgrade", "").Replace("_3_model", ""), out int upgradeNumber))
                    {
                        TapperTapUpgradeModels3[upgradeNumber] = entities[0].As<CDynamicProp>();
                    }
                }
            }
            else if (targetname.StartsWith("tapper_tap_buttonupgrade"))
            {
                if (targetname.EndsWith("0_model"))
                {
                    if (int.TryParse(targetname.Replace("tapper_tap_buttonupgrade", "").Replace("_0_model", ""), out int upgradeNumber))
                    {
                        TapperTapButtonUpgradeModels0[upgradeNumber] = entities[0].As<CDynamicProp>();
                    }
                }
                else if (targetname.EndsWith("1_model"))
                {
                    if (int.TryParse(targetname.Replace("tapper_tap_buttonupgrade", "").Replace("_1_model", ""), out int upgradeNumber))
                    {
                        TapperTapButtonUpgradeModels1[upgradeNumber] = entities[0].As<CDynamicProp>();
                    }
                }
                else if (targetname.EndsWith("2_model"))
                {
                    if (int.TryParse(targetname.Replace("tapper_tap_buttonupgrade", "").Replace("_2_model", ""), out int upgradeNumber))
                    {
                        TapperTapButtonUpgradeModels2[upgradeNumber] = entities[0].As<CDynamicProp>();
                    }
                }
                else if (targetname.EndsWith("3_model"))
                {
                    if (int.TryParse(targetname.Replace("tapper_tap_buttonupgrade", "").Replace("_3_model", ""), out int upgradeNumber))
                    {
                        TapperTapButtonUpgradeModels3[upgradeNumber] = entities[0].As<CDynamicProp>();
                    }
                }
            }
        }

        TapperTapUpgradeModels = [TapperTapUpgradeModels0, TapperTapUpgradeModels1, TapperTapUpgradeModels2, TapperTapUpgradeModels3];
        TapperTapButtonUpgradeModels = [TapperTapButtonUpgradeModels0, TapperTapButtonUpgradeModels1, TapperTapButtonUpgradeModels2, TapperTapButtonUpgradeModels3];
    }
}