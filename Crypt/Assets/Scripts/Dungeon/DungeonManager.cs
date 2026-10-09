using UnityEngine;

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
        ResetDungeonSettings();
        runeManager = FindAnyObjectByType<RuneManager>();
        generator = FindAnyObjectByType<DungeonGenerator>();
        library = FindAnyObjectByType<RuneLibrary>();
        director = FindAnyObjectByType<EnemyDirector>();

        foreach (EquippedRune rune in runeManager.equippedRunes)
        {
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

        generator.GenerateDungeon();
        director.InitializeDirector(EnemyAllowance);
        director.ToggleDirector(true);
        director.SpawnEnemies();
    }

}