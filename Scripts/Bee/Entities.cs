using CounterStrikeSharp.API.Core;

namespace ArcadeScripts.Scripts;

public partial class Bee
{
    private CDynamicProp DamageSignModel = null!;
    private CDynamicProp StartButtonModel = null!;
    private CDynamicProp PauseButtonModel = null!;
    private CDynamicProp EasyButtonModel = null!;
    private CDynamicProp MediumButtonModel = null!;
    private CDynamicProp HardButtonModel = null!;
    private CBaseButton[] KeyboardButtons = new CBaseButton[16];
    private CDynamicProp[] KeyboardButtonModels = new CDynamicProp[16];
    private CDynamicProp[] BoothOriginEntities = new CDynamicProp[16];
    private CLogicMeasureMovement[] MeasureEntities = new CLogicMeasureMovement[16];
    private CTriggerHurt[] BeeHurt = new CTriggerHurt[16];
    private CEntityInstance[] CurrentGameButtonModels = new CEntityInstance[2];
    private CPointWorldText BeeTimer = null!;
    private CAmbientGeneric StartSound = null!;
    private Dictionary<int, CPointWorldText> BoothInputEntities = [];
    private Dictionary<int, CPointWorldText> CurrentWordDefinition = [];
    private CPointWorldText CurrentWordEntity = null!;

    private void OnAllEntitiesSpawned()
    {
        foreach (KeyValuePair<string, List<CEntityInstance>> kv in EntityList)
        {
            List<CEntityInstance> entities = kv.Value;
            string targetname = kv.Key;

            if (targetname.StartsWith("bee_keyboard_model"))
            {
                if (int.TryParse(targetname.Replace("bee_keyboard_model", ""), out int index))
                {
                    KeyboardButtonModels[index - 1] = entities[0].As<CDynamicProp>();
                }
            }
            else if (targetname.StartsWith("bee_keyboard"))
            {
                if (int.TryParse(targetname.Replace("bee_keyboard", ""), out int index))
                {
                    KeyboardButtons[index - 1] = entities[0].As<CBaseButton>();
                }
            }
            else if (targetname.StartsWith("bee_origin"))
            {
                if (int.TryParse(targetname.Replace("bee_origin", ""), out int index))
                {
                    BoothOriginEntities[index - 1] = entities[0].As<CDynamicProp>();
                }
            }
            else if (targetname.StartsWith("bee_measure"))
            {
                if (int.TryParse(targetname.Replace("bee_measure", ""), out int index))
                {
                    MeasureEntities[index - 1] = entities[0].As<CLogicMeasureMovement>();
                }
            }
            else if (targetname.StartsWith("bee_input"))
            {
                if (int.TryParse(targetname.Replace("bee_input", ""), out int number))
                {
                    BoothInputEntities[number] = entities[0].As<CPointWorldText>();
                }
            }
            else if (targetname.StartsWith("bee_currentdefinition"))
            {
                if (int.TryParse(targetname.Replace("bee_currentdefinition", ""), out int index))
                {
                    CurrentWordDefinition[index - 1] = entities[0].As<CPointWorldText>();
                }
            }
            else if (targetname.StartsWith("bee_hurt"))
            {
                if (int.TryParse(targetname.Replace("bee_hurt", ""), out int index))
                {
                    BeeHurt[index - 1] = entities[0].As<CTriggerHurt>();
                }
            }
            else if (targetname == "bee_damage_sign_model")
            {
                DamageSignModel = entities[0].As<CDynamicProp>();
            }
            else if (targetname == "bee_starttimer_model")
            {
                StartButtonModel = entities[0].As<CDynamicProp>();
            }
            else if (targetname == "bee_pausetimer_model")
            {
                PauseButtonModel = entities[0].As<CDynamicProp>();
            }
            else if (targetname == "bee_randomword_easy_model")
            {
                EasyButtonModel = entities[0].As<CDynamicProp>();
            }
            else if (targetname == "bee_randomword_medium_model")
            {
                MediumButtonModel = entities[0].As<CDynamicProp>();
            }
            else if (targetname == "bee_randomword_hard_model")
            {
                HardButtonModel = entities[0].As<CDynamicProp>();
            }
            else if (targetname == "bee_spell_model")
            {
                CurrentGameButtonModels[0] = entities[0].As<CDynamicProp>();
            }
            else if (targetname == "bee_math_model")
            {
                CurrentGameButtonModels[1] = entities[0].As<CDynamicProp>();
            }
            else if (targetname == "bee_timer")
            {
                BeeTimer = entities[0].As<CPointWorldText>();
            }
            else if (targetname == "bee_currentword")
            {
                CurrentWordEntity = entities[0].As<CPointWorldText>();
            }
            else if (targetname == "bee_start_sound")
            {
                StartSound = entities[0].As<CAmbientGeneric>();
            }
        }
    }
}