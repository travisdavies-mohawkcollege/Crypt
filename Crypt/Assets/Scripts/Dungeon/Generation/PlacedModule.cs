using System.Collections.Generic;
using UnityEngine;

//This stores the runtime information for one placed room module.
//The original RoomModule still stores the prefab's local information.
public class PlacedModule
{
    public RoomModule ModulePrefab;
    public Vector3Int OriginCell;
    public int QuarterTurns;

    //These are the cells this specific placement occupies in the dungeon grid.
    public List<Vector3Int> WorldCells = new();

    //These are the entrances after the module's position and rotation are applied.
    public List<PlacedEntrance> Entrances = new();

    //This is assigned later when the module is actually spawned.
    public RoomModule Instance;

    public PlacedModule(RoomModule modulePrefab, Vector3Int originCell, int quarterTurns)
    {
        this.ModulePrefab = modulePrefab;
        this.OriginCell = originCell;

        //This keeps the rotation between 0 and 3.
        this.QuarterTurns = ((quarterTurns % 4) + 4) % 4;

        CreateWorldCells();
        CreateEntrances();
    }

    void CreateWorldCells()
    {
        WorldCells.Clear();

        foreach (Vector3Int localCell in ModulePrefab.OccupiedCells)
        {
            Vector3Int rotatedCell = DungeonGenerator.RotateCell(localCell, QuarterTurns);
            Vector3Int worldCell = OriginCell + rotatedCell;

            WorldCells.Add(worldCell);
        }
    }

    void CreateEntrances()
    {
        Entrances.Clear();

        for (int i = 0; i < ModulePrefab.Entrances.Count; i++)
        {
            Entrance entrance = ModulePrefab.Entrances[i];

            //Rotate the entrance's local cell and move it to the module's grid position.
            Vector3Int rotatedCell = DungeonGenerator.RotateCell(entrance.LocalCell, QuarterTurns);
            Vector3Int worldCell = OriginCell + rotatedCell;

            //The direction also needs to be rotated with the module.
            Vector3Int worldDirection = DungeonGenerator.RotateCell(entrance.LocalDirection, QuarterTurns);

            PlacedEntrance placedEntrance = new PlacedEntrance(this, i, worldCell, worldDirection);
            Entrances.Add(placedEntrance);
        }
    }

    public void SetInstance(RoomModule instance)
    {
        Instance = instance;
    }

    public void ConfigureEntranceVisuals()
    {
        if (Instance == null)
        {
            return;
        }

        foreach (PlacedEntrance entrance in Entrances)
        {
            Entrance entranceInstance = Instance.Entrances[entrance.EntranceIndex];
            entranceInstance.SetConnected(entrance.IsConnected);
        }
    }
}