using System.Collections.Generic;
using UnityEngine;

// Authored metadata for a reusable room prefab that may occupy multiple dungeon grid cells.
public class RoomModule : MonoBehaviour
{
    [SerializeField] private ModuleType moduleType;

    // Cells are stored relative to the module root so the footprint can be rotated and translated.
    [SerializeField] private List<Vector3Int> occupiedCells;

    // Each entrance identifies a compatible connection point on the module boundary.
    [SerializeField] private List<Entrance> entrances;

    public ModuleType Type => moduleType;
    public IReadOnlyList<Vector3Int> OccupiedCells => occupiedCells;
    public IReadOnlyList<Entrance> Entrances => entrances;
    [SerializeField] private bool ignoreGridBounds;

    public bool IgnoreGridBounds => ignoreGridBounds;
}
