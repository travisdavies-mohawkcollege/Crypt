using System.Collections.Generic;
using UnityEngine;

// Creates a sporadic, unreachable 3D dungeon around an existing dungeon.
public class BackgroundDungeonGenerator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The parent containing rooms spawned by the regular DungeonGenerator.")]
    [SerializeField] private Transform existingDungeonParent;
    [SerializeField] private GameObject backgroundRoomPrefab;

    [Tooltip("Optional stair, shaft, or vertical room. The normal prefab is used if empty.")]
    [SerializeField] private GameObject verticalRoomPrefab;
    [SerializeField] private Transform backgroundRoomParent;

    [Header("Grid")]
    [SerializeField] private Vector3 roomSpacing = new Vector3(10f, 12f, 10f);

    [Tooltip("Empty cells between the playable dungeon and the background.")]
    [Min(1)]
    [SerializeField] private int unreachableGap = 2;

    [Tooltip("How far paths may spread horizontally.")]
    [Min(5)]
    [SerializeField] private int outsideDistance = 12;

    [Tooltip("How many layers may be generated above the dungeon's highest floor.")]
    [Min(1)]
    [SerializeField] private int layersAboveDungeon = 4;

    [Header("Path Shape")]
    [Min(1)]
    [SerializeField] private int pathsPerSide = 4;

    [Min(1)]
    [SerializeField] private int pathLength = 24;

    [Range(0f, 1f)]
    [SerializeField] private float continueStraightChance = 0.65f;

    [Tooltip("Chance that a path moves up or down instead of horizontally.")]
    [Range(0f, 1f)]
    [SerializeField] private float verticalMoveChance = 0.15f;

    [Tooltip("Chance that a generated room receives an extra vertical column.")]
    [Range(0f, 1f)]
    [SerializeField] private float extraVerticalConnectionChance = 0.04f;

    private readonly List<GameObject> spawnedRooms = new();

    // Call immediately after the regular DungeonGenerator.GenerateDungeon().
    public void GenerateDungeon()
    {
        ClearPreviousDungeon();

        if (existingDungeonParent == null || existingDungeonParent.childCount == 0)
        {
            Debug.LogError("No generated dungeon rooms were found.");
            return;
        }

        if (backgroundRoomPrefab == null)
        {
            Debug.LogError("A Background Room Prefab must be assigned.");
            return;
        }

        FindExistingBounds(
            out int minimumX,
            out int maximumX,
            out int minimumZ,
            out int maximumZ,
            out int lowestLayer,
            out int highestLayer);

        int minimumBackgroundLayer = lowestLayer;
        int maximumBackgroundLayer = highestLayer + Mathf.Max(1, layersAboveDungeon);

        HashSet<Vector3Int> cells = new();
        HashSet<Vector3Int> verticalCells = new();

        for (int side = 0; side < 4; side++)
        {
            for (int path = 0; path < pathsPerSide; path++)
            {
                CreateRandomPath(
                    side,
                    minimumX,
                    maximumX,
                    minimumZ,
                    maximumZ,
                    minimumBackgroundLayer,
                    maximumBackgroundLayer,
                    highestLayer,
                    path == 0,
                    cells,
                    verticalCells);
            }
        }

        AddExtraVerticalConnections(
            minimumBackgroundLayer,
            maximumBackgroundLayer,
            cells,
            verticalCells);

        SpawnBackgroundRooms(cells, verticalCells);
    }

    private void CreateRandomPath(
        int side,
        int minimumX,
        int maximumX,
        int minimumZ,
        int maximumZ,
        int minimumLayer,
        int maximumLayer,
        int highestDungeonLayer,
        bool forceAboveDungeon,
        HashSet<Vector3Int> cells,
        HashSet<Vector3Int> verticalCells)
    {
        // The first path on every side is guaranteed to be above the dungeon.
        // The remaining paths may appear beside it or on any aerial layer.
        int layer = forceAboveDungeon
            ? Random.Range(highestDungeonLayer + 1, maximumLayer + 1)
            : Random.Range(minimumLayer, maximumLayer + 1);
        Vector3Int cell;
        Vector3Int direction;

        switch (side)
        {
            case 0:
                cell = new Vector3Int(
                    minimumX - unreachableGap,
                    layer,
                    Random.Range(minimumZ, maximumZ + 1));
                direction = Vector3Int.left;
                break;

            case 1:
                cell = new Vector3Int(
                    maximumX + unreachableGap,
                    layer,
                    Random.Range(minimumZ, maximumZ + 1));
                direction = Vector3Int.right;
                break;

            case 2:
                cell = new Vector3Int(
                    Random.Range(minimumX, maximumX + 1),
                    layer,
                    minimumZ - unreachableGap);
                direction = Vector3Int.back;
                break;

            default:
                cell = new Vector3Int(
                    Random.Range(minimumX, maximumX + 1),
                    layer,
                    maximumZ + unreachableGap);
                direction = Vector3Int.forward;
                break;
        }

        // Every aerial path begins with a vertical connection to the lowest
        // background layer, so the separate layers look like one 3D structure.
        AddVerticalColumn(
            cell.x,
            cell.z,
            minimumLayer,
            cell.y,
            cells,
            verticalCells);

        for (int step = 0; step < pathLength; step++)
        {
            cells.Add(cell);

            if (Random.value < verticalMoveChance)
            {
                int verticalDirection = Random.value < 0.5f ? -1 : 1;
                int newLayer = Mathf.Clamp(cell.y + verticalDirection, minimumLayer, maximumLayer);

                if (newLayer != cell.y)
                {
                    verticalCells.Add(cell);
                    cell = new Vector3Int(cell.x, newLayer, cell.z);
                    cells.Add(cell);
                    verticalCells.Add(cell);
                }
            }

            if (Random.value > continueStraightChance)
            {
                direction = GetRandomHorizontalDirection();
            }

            Vector3Int nextCell = cell + direction;

            if (!IsAllowedBackgroundCell(
                    nextCell,
                    minimumX,
                    maximumX,
                    minimumZ,
                    maximumZ,
                    minimumLayer,
                    maximumLayer))
            {
                direction = GetRandomHorizontalDirection();
                nextCell = cell + direction;

                if (!IsAllowedBackgroundCell(
                        nextCell,
                        minimumX,
                        maximumX,
                        minimumZ,
                        maximumZ,
                        minimumLayer,
                        maximumLayer))
                {
                    continue;
                }
            }

            cell = nextCell;
        }
    }

    private bool IsAllowedBackgroundCell(
        Vector3Int cell,
        int minimumX,
        int maximumX,
        int minimumZ,
        int maximumZ,
        int minimumLayer,
        int maximumLayer)
    {
        bool insideOuterLimit =
            cell.x >= minimumX - outsideDistance &&
            cell.x <= maximumX + outsideDistance &&
            cell.z >= minimumZ - outsideDistance &&
            cell.z <= maximumZ + outsideDistance &&
            cell.y >= minimumLayer &&
            cell.y <= maximumLayer;

        bool insideProtectedDungeon =
            cell.x > minimumX - unreachableGap &&
            cell.x < maximumX + unreachableGap &&
            cell.z > minimumZ - unreachableGap &&
            cell.z < maximumZ + unreachableGap;

        return insideOuterLimit && !insideProtectedDungeon;
    }

    private static Vector3Int GetRandomHorizontalDirection()
    {
        switch (Random.Range(0, 4))
        {
            case 0: return Vector3Int.right;
            case 1: return Vector3Int.left;
            case 2: return Vector3Int.forward;
            default: return Vector3Int.back;
        }
    }

    private void AddExtraVerticalConnections(
        int minimumLayer,
        int maximumLayer,
        HashSet<Vector3Int> cells,
        HashSet<Vector3Int> verticalCells)
    {
        List<Vector3Int> snapshot = new(cells);

        foreach (Vector3Int cell in snapshot)
        {
            if (Random.value >= extraVerticalConnectionChance)
            {
                continue;
            }

            int targetLayer = Random.Range(minimumLayer, maximumLayer + 1);
            AddVerticalColumn(
                cell.x,
                cell.z,
                cell.y,
                targetLayer,
                cells,
                verticalCells);
        }
    }

    private static void AddVerticalColumn(
        int x,
        int z,
        int firstLayer,
        int secondLayer,
        HashSet<Vector3Int> cells,
        HashSet<Vector3Int> verticalCells)
    {
        int bottom = Mathf.Min(firstLayer, secondLayer);
        int top = Mathf.Max(firstLayer, secondLayer);

        for (int y = bottom; y <= top; y++)
        {
            Vector3Int cell = new Vector3Int(x, y, z);
            cells.Add(cell);
            verticalCells.Add(cell);
        }
    }

    private void SpawnBackgroundRooms(
        HashSet<Vector3Int> cells,
        HashSet<Vector3Int> verticalCells)
    {
        foreach (Vector3Int cell in cells)
        {
            Vector3 position = new Vector3(
                cell.x * roomSpacing.x,
                cell.y * roomSpacing.y,
                cell.z * roomSpacing.z);

            GameObject prefab =
                verticalCells.Contains(cell) && verticalRoomPrefab != null
                    ? verticalRoomPrefab
                    : backgroundRoomPrefab;

            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);
            GameObject room = Instantiate(
                prefab,
                position,
                rotation,
                backgroundRoomParent);

            spawnedRooms.Add(room);
        }
    }

    private void FindExistingBounds(
        out int minimumX,
        out int maximumX,
        out int minimumZ,
        out int maximumZ,
        out int lowestLayer,
        out int highestLayer)
    {
        Transform firstRoom = existingDungeonParent.GetChild(0);

        minimumX = maximumX = Mathf.RoundToInt(firstRoom.position.x / roomSpacing.x);
        minimumZ = maximumZ = Mathf.RoundToInt(firstRoom.position.z / roomSpacing.z);
        lowestLayer = highestLayer = Mathf.RoundToInt(firstRoom.position.y / roomSpacing.y);

        for (int i = 1; i < existingDungeonParent.childCount; i++)
        {
            Vector3 position = existingDungeonParent.GetChild(i).position;
            int cellX = Mathf.RoundToInt(position.x / roomSpacing.x);
            int cellZ = Mathf.RoundToInt(position.z / roomSpacing.z);
            int cellY = Mathf.RoundToInt(position.y / roomSpacing.y);

            minimumX = Mathf.Min(minimumX, cellX);
            maximumX = Mathf.Max(maximumX, cellX);
            minimumZ = Mathf.Min(minimumZ, cellZ);
            maximumZ = Mathf.Max(maximumZ, cellZ);
            lowestLayer = Mathf.Min(lowestLayer, cellY);
            highestLayer = Mathf.Max(highestLayer, cellY);
        }
    }

    private void ClearPreviousDungeon()
    {
        foreach (GameObject room in spawnedRooms)
        {
            if (room != null)
            {
                Destroy(room);
            }
        }

        spawnedRooms.Clear();
    }
}
