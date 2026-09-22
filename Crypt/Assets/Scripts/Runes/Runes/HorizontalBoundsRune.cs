using UnityEngine;

public class HorizontalBoundsRune : IRune
{
    public int RuneID => 000;
    public int PowerScoreModifier => 1;
    public string RuneName => "Increase Horizontal Bounds";
    public string RuneDescription => "Increases the horizontal space that the dungeon has to generate in.";

    public void RuneEffect(DungeonManager dungeonManager)
    {
        dungeonManager.GridSize += new Vector3Int (2, 0, 2);
    }

}
