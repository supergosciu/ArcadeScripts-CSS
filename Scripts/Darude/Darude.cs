using CounterStrikeSharp.API.Core;

namespace ArcadeScripts.Scripts;

public class Darude : ScriptBase
{
    public Darude(CLogicScript owner) : base(owner)
    {
        Functions.Add("EnterCode", new ScriptFunction<string>(EnterCode));

        EntityNames = ["disco_button7_template"];

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
    }
    
    public const string LETTER_D = "D";
    public const string LETTER_A = "A";
    public const string LETTER_R = "R";
    public const string LETTER_U = "U";
    public const string LETTER_E = "E";

    private string CurrentInput = "";
    private bool Spawned = false;

    private CPointTemplate DiscoButtonTemplate = null!;

    private void OnAllEntitiesSpawned()
    {
        DiscoButtonTemplate = EntityList["disco_button7_template"][0].As<CPointTemplate>();
    }

    public void EnterCode(string letter)
    {
        if (Spawned) return;
        CurrentInput += letter;
        
        if (CurrentInput == "DARUDE")
        {
            DiscoButtonTemplate.AddEntityIOEvent(inputName: "ForceSpawn", delay: 0.0f);
            Spawned = true;
            
            return;
        }

        if (!(CurrentInput == "D" || CurrentInput == "DA" || CurrentInput == "DAR" || CurrentInput == "DARU" || CurrentInput == "DARUD"))
        {
            CurrentInput = letter;
        }
    }

    public override void Remove()
    {
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
    }
}