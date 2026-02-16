using CounterStrikeSharp.API.Core;

namespace ArcadeScripts.Scripts;

public partial class Colors
{
    private List<CDynamicProp> InstructionMiddleEntities = [];
    private List<CDynamicProp> InstructionLeftEntities = [];
    private List<CDynamicProp> InstructionRightEntities = [];

    private CFuncBrush[][] LineEntities = [];
    private CFuncBrush[] LineEntitiesHorizontal = new CFuncBrush[3];
    private CFuncBrush[] LineEntitiesVertical = new CFuncBrush[3];

    private CFuncBrush[] CircleEntities = new CFuncBrush[3];

    private CFuncBrush[] LadderEntities = new CFuncBrush[3];

    private CTriggerMultiple ColorsJumpTrigger = null!;
    private CTimerEntity ColorsTimer = null!;
    private CFuncBrush ColorsLadder = null!;
    private CDynamicProp[] ButtonModelEntities = new CDynamicProp[3];
    private CDynamicProp ColorsEndButtonModel = null!;
    private CDynamicProp ColorsDamageSignModel = null!;

    private void OnAllEntitiesSpawned()
    {
        foreach (KeyValuePair<string, List<CEntityInstance>> kv in EntityList)
        {
            List<CEntityInstance> entities = kv.Value;
            string targetname = kv.Key;

            if (targetname == "colors_line_easy_horizontal")
            {
                LineEntitiesHorizontal[0] = entities[0].As<CFuncBrush>();
            }
            else if (targetname == "colors_line_medium_horizontal")
            {
                LineEntitiesHorizontal[1] = entities[0].As<CFuncBrush>();
            }
            else if (targetname == "colors_line_hard_horizontal")
            {
                LineEntitiesHorizontal[2] = entities[0].As<CFuncBrush>();
            }

            else if (targetname == "colors_line_easy_vertical")
            {
                LineEntitiesVertical[0] = entities[0].As<CFuncBrush>();
            }
            else if (targetname == "colors_line_medium_vertical")
            {
                LineEntitiesVertical[1] = entities[0].As<CFuncBrush>();
            }
            else if (targetname == "colors_line_hard_vertical")
            {
                LineEntitiesVertical[2] = entities[0].As<CFuncBrush>();
            }

            else if (targetname == "colors_circle_easy")
            {
                CircleEntities[0] = entities[0].As<CFuncBrush>();
            }
            else if (targetname == "colors_circle_medium")
            {
                CircleEntities[1] = entities[0].As<CFuncBrush>();
            }
            else if (targetname == "colors_circle_hard")
            {
                CircleEntities[2] = entities[0].As<CFuncBrush>();
            }

            else if (targetname == "colors_commands_middle_model")
            {
                for (int i = 0; i < entities.Count; i++) InstructionMiddleEntities.Add(entities[i].As<CDynamicProp>());
            }
            else if (targetname == "colors_commands_left_model")
            {
                for (int i = 0; i < entities.Count; i++) InstructionLeftEntities.Add(entities[i].As<CDynamicProp>());
            }
            else if (targetname == "colors_commands_right_model")
            {
                for (int i = 0; i < entities.Count; i++) InstructionRightEntities.Add(entities[i].As<CDynamicProp>());
            }

            else if (targetname == "colors_ladder")
            {
                ColorsLadder = entities[0].As<CFuncBrush>();
            }
            else if (targetname == "colors_ladder_brush_easy")
            {
                LadderEntities[0] = entities[0].As<CFuncBrush>();
            }
            else if (targetname == "colors_ladder_brush_medium")
            {
                LadderEntities[1] = entities[0].As<CFuncBrush>();
            }
            else if (targetname == "colors_ladder_brush_hard")
            {
                LadderEntities[2] = entities[0].As<CFuncBrush>();
            }

            else if (targetname == "colors_jump_trigger")
            {
                ColorsJumpTrigger = entities[0].As<CTriggerMultiple>();
            }
            else if (targetname == "colors_timer")
            {
                ColorsTimer = entities[0].As<CTimerEntity>();
            }

            else if (targetname == "colors_damage_sign_model")
            {
                ColorsDamageSignModel = entities[0].As<CDynamicProp>();
            }
            else if (targetname == "colors_easy_button_model")
            {
                ButtonModelEntities[0] = entities[0].As<CDynamicProp>();
            }
            else if (targetname == "colors_medium_button_model")
            {
                ButtonModelEntities[1] = entities[0].As<CDynamicProp>();
            }
            else if (targetname == "colors_hard_button_model")
            {
                ButtonModelEntities[2] = entities[0].As<CDynamicProp>();
            }
            else if (targetname == "colors_stop_button_model")
            {
                ColorsEndButtonModel = entities[0].As<CDynamicProp>();
            }
        }

        LineEntities = [LineEntitiesHorizontal, LineEntitiesVertical];
    }
}