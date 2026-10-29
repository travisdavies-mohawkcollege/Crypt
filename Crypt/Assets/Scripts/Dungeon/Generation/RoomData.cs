using System.Collections.Generic;
using UnityEngine;

// Runtime state for one generated hallway cell.
// RoomView reads its connections to decide which walls, floors, and ceilings remain active.
public class RoomData
{
    public Vector3Int Cell;
    public RoomTypes RoomType;
    public HashSet<Vector3Int> ConnectedCells = new();
    public Vector3Int EntryDirection { get; set; }
    public bool IsStairway {  get ; set; }
    public bool IsExit { get ; set; }
    
    public RoomData(Vector3Int cell)
    {
        // New rooms begin as unconnected normal rooms until the generator assigns their role.
        this.Cell = cell;
        EntryDirection = Vector3Int.zero;
        RoomType = RoomTypes.Normal;
    }
}
