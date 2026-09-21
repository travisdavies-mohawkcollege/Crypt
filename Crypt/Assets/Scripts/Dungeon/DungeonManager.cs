using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    private DungeonGenerator generator;
    void Start()
    {
        
    }

    public void IntializeDungeonManager()
    {
        generator = FindAnyObjectByType<DungeonGenerator>();
        generator.InitializeGenerator(15, 0.2f, 0.7f, 0.01f, new Vector3Int (10, 3, 10));
        generator.GenerateDungeon();
    }

}