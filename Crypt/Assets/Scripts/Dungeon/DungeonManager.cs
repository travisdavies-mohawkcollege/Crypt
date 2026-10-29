using UnityEngine;

// Builds the effective dungeon settings, applies equipped runes, and starts generation and spawning.
public class DungeonManager : MonoBehaviour
{
    private RuneManager runeManager;
    private DungeonGenerator generator;
    private RuneLibrary library;
    private EnemyDirector director;

    [Header("Dungeon Generator Variables")]
    public int BaseDungeonLevel = 1;
    public int DungeonLevel { get; private set; }
    public int TargetRooms;
    public float LoopChance;
    public float StraightChance;
    public float NextFloorChanceMin;
    public Vector3Int GridSize;
    public bool LightsOut;
    public int NumberOfBranches;
    public int BranchLength;
    public bool GenerateBranches;
    public int EnemyAllowance;

    private void ResetDungeonSettings()
    {
        // Always begin from known baseline values before applying the current rune loadout.
        TargetRooms = 3;
        LoopChance = 0f;
        StraightChance = 0.2f;
        NextFloorChanceMin = 0.1f;
        GridSize = new Vector3Int(5, 1, 5);
        LightsOut = false;
        NumberOfBranches = 2;
        BranchLength = 5;
        GenerateBranches = true;
        EnemyAllowance = 5;
        DungeonLevel = Mathf.Max(1, BaseDungeonLevel);
    }

    public void IntializeDungeonManager()
    {
        // Scene-local systems are resolved after the additive Dungeon scene finishes loading.
        ResetDungeonSettings();
        runeManager = FindAnyObjectByType<RuneManager>();
        generator = FindAnyObjectByType<DungeonGenerator>();
        library = FindAnyObjectByType<RuneLibrary>();
        director = FindAnyObjectByType<EnemyDirector>();

        foreach (EquippedRune rune in runeManager.equippedRunes)
        {
            // Rune levels raise dungeon difficulty before their individual modifiers are applied.
            DungeonLevel += Mathf.Max(1, rune.level);
            library.RuneEffect(this, rune);
        }

        generator.InitializeGenerator(
            TargetRooms,
            LoopChance,
            StraightChance,
            NextFloorChanceMin,
            GridSize,
            NumberOfBranches,
            BranchLength,
            GenerateBranches,
            DungeonLevel
            );

        EnemyAllowance = TargetRooms / 2;

        // Room generation must finish before enemy spawn points are collected by the director.
        generator.GenerateDungeon();
        director.InitializeDirector(EnemyAllowance);
        director.ToggleDirector(true);
        director.SpawnEnemies();
    }

}
