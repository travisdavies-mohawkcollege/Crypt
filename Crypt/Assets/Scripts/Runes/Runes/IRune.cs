using UnityEngine;

public interface IRune 
{
    int RuneID { get; }
    int PowerScoreModifier { get;}
    string RuneName { get; }
    string RuneDescription { get; }

    public void RuneEffect(DungeonManager dungeonManager){}

}
