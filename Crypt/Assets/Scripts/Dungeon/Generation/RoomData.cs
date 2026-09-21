using System.Collections.Generic;
using UnityEngine;

//This is essentially each room's memory.
//This can store variables we want to base room
//decorations, and the RoomView reads what cells this
//room is connected to in order too open walls/place doors.

public class RoomData
{
    public Vector3Int Cell;
    public HashSet<Vector3Int> ConnectedCells = new();
    public Vector3Int EntryDirection { get; set; }
    public bool IsStairway {  get ; set; }
    public bool IsExit { get ; set; }
    
    public RoomData(Vector3Int cell)
    {
        this.Cell = cell;
        EntryDirection = Vector3Int.zero;
    }
}
