using System;
using System.Collections.Generic;
using UnityEngine;

// Art-directed background generation for a vast, unreachable liminal dungeon.
// It creates sparse districts, long corridors, voids, bridges, and vertical towers.
public class LiminalBackgroundDungeonGenerator : MonoBehaviour
{
    private enum CellStyle
    {
        Room,
        Vertical,
        Landmark
    }

    [Header("References")]
    [SerializeField] private Transform existingDungeonParent;
    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private GameObject verticalPrefab;
    [SerializeField] private GameObject landmarkPrefab;
    [SerializeField] private Transform backgroundParent;

    [Header("Grid")]
    [SerializeField] private Vector3 roomSpacing = new Vector3(12f, 4f, 12f);
    [Min(1)] [SerializeField] private int unreachableGap = 3;
    [Min(1)] [SerializeField] private int districtCount = 4;
    [Min(3)] [SerializeField] private int distancePerDistrict = 8;
    [Min(1)] [SerializeField] private int pathsPerSide = 3;
    [Min(4)] [SerializeField] private int basePathLength = 18;
    [Min(1)] [SerializeField] private int aerialLayers = 5;

    [Header("Atmosphere")]
    [Range(0f, 1f)] [SerializeField] private float straightHallChance = 0.72f;
    [Range(0f, 1f)] [SerializeField] private float verticalStepChance = 0.12f;
    [Range(0f, 1f)] [SerializeField] private float deliberateVoidChance = 0.08f;
    [Range(0f, 1f)] [SerializeField] private float towerChance = 0.035f;
    [Range(0f, 1f)] [SerializeField] private float widenedRoomChance = 0.08f;

    [Header("Repeatability")]
    [Tooltip("The same seed produces the same distant dungeon.")]
    [SerializeField] private int seed = 1408;

    private readonly List<GameObject> spawnedObjects = new();
    private readonly Dictionary<Vector3Int, CellStyle> cells = new();
    private System.Random random;

    // Call this after the playable DungeonGenerator.GenerateDungeon().
    public void GenerateDungeon()
    {
        ClearPreviousDungeon();

        if (existingDungeonParent == null || existingDungeonParent.childCount == 0)
        {
            Debug.LogError("Liminal background generation needs the existing dungeon parent.");
            return;
        }

        if (roomPrefab == null)
        {
            Debug.LogError("Liminal background generation needs a Room Prefab.");
            return;
        }

        random = new System.Random(seed);

        FindExistingBounds(
            out int minimumX,
            out int maximumX,
            out int minimumZ,
            out int maximumZ,
            out int lowestLayer,
            out int highestLayer);

        for (int district = 0; district < districtCount; district++)
        {
            int innerDistance = unreachableGap + district * distancePerDistrict;
            int outerDistance = innerDistance + distancePerDistrict + 4;
            int districtPaths = pathsPerSide + district;
            int districtPathLength = basePathLength + district * 6;
            int districtTopLayer = highestLayer + aerialLayers + district;

            for (int side = 0; side < 4; side++)
            {
                for (int path = 0; path < districtPaths; path++)
                {
                    GeneratePath(
                        side,
                        innerDistance,
                        outerDistance,
                        districtPathLength,
                        minimumX,
                        maximumX,
                        minimumZ,
                        maximumZ,
                        lowestLayer,
                        highestLayer,
                        districtTopLayer);
                }
            }
        }

        AddAerialBridges(highestLayer, highestLayer + aerialLayers + districtCount);
        SpawnCells();
    }

    private void GeneratePath(
        int side,
        int innerDistance,
        int outerDistance,
        int length,
        int minimumX,
        int maximumX,
        int minimumZ,
        int maximumZ,
        int lowestLayer,
        int highestLayer,
        int topLayer)
    {
        int layer = Next(lowestLayer, topLayer + 1);
        Vector3Int cell;
        Vector3Int direction;

        switch (side)
        {
            case 0:
                cell = new Vector3Int(
                    minimumX - innerDistance,
                    layer,
                    Next(minimumZ - innerDistance, maximumZ + innerDistance + 1));
                direction = Vector3Int.left;
                break;

            case 1:
                cell = new Vector3Int(
                    maximumX + innerDistance,
                    layer,
                    Next(minimumZ - innerDistance, maximumZ + innerDistance + 1));
                direction = Vector3Int.right;
                break;

            case 2:
                cell = new Vector3Int(
                    Next(minimumX - innerDistance, maximumX + innerDistance + 1),
                    layer,
                    minimumZ - innerDistance);
                direction = Vector3Int.back;
                break;

            default:
                cell = new Vector3Int(
                    Next(minimumX - innerDistance, maximumX + innerDistance + 1),
                    layer,
                    maximumZ + innerDistance);
                direction = Vector3Int.forward;
                break;
        }

        // A shaft ties each floating path into another layer without making a solid wall.
        int shaftBottom = Next(lowestLayer, highestLayer + 1);
        AddVerticalColumn(cell.x, cell.z, shaftBottom, cell.y);

        for (int step = 0; step < length; step++)
        {
            bool deliberateVoid =
                step > 2 &&
                step < length - 2 &&
                Roll(deliberateVoidChance);

            if (!deliberateVoid)
            {
                AddCell(cell, CellStyle.Room);

                if (Roll(widenedRoomChance))
                {
                    Vector3Int sideRoom = cell + GetPerpendicular(direction);
                    if (IsInsideDistrict(sideRoom, outerDistance, minimumX, maximumX, minimumZ, maximumZ))
                    {
                        AddCell(sideRoom, CellStyle.Room);
                    }
                }

                if (Roll(towerChance))
                {
                    int towerHeight = Next(2, 6);
                    AddVerticalColumn(cell.x, cell.z, cell.y, Mathf.Min(topLayer, cell.y + towerHeight));
                    AddCell(new Vector3Int(cell.x, Mathf.Min(topLayer, cell.y + towerHeight), cell.z), CellStyle.Landmark);
                }
            }

            if (Roll(verticalStepChance))
            {
                int nextLayer = Mathf.Clamp(cell.y + (Roll(0.55f) ? 1 : -1), lowestLayer, topLayer);
                if (nextLayer != cell.y)
                {
                    AddVerticalColumn(cell.x, cell.z, cell.y, nextLayer);
                    cell.y = nextLayer;
                }
            }

            if (!Roll(straightHallChance))
            {
                direction = Roll(0.72f)
                    ? GetPerpendicular(direction)
                    : GetRandomHorizontalDirection();
            }

            Vector3Int candidate = cell + direction;
            if (!IsInsideDistrict(candidate, outerDistance, minimumX, maximumX, minimumZ, maximumZ))
            {
                direction = GetRandomHorizontalDirection();
                candidate = cell + direction;
            }

            if (IsInsideDistrict(candidate, outerDistance, minimumX, maximumX, minimumZ, maximumZ))
            {
                cell = candidate;
            }
        }

        AddCell(cell, CellStyle.Landmark);
    }

    private bool IsInsideDistrict(
        Vector3Int cell,
        int distance,
        int minimumX,
        int maximumX,
        int minimumZ,
        int maximumZ)
    {
        bool insideOuterBounds =
            cell.x >= minimumX - distance &&
            cell.x <= maximumX + distance &&
            cell.z >= minimumZ - distance &&
            cell.z <= maximumZ + distance;

        bool insideProtectedDungeon =
            cell.x > minimumX - unreachableGap &&
            cell.x < maximumX + unreachableGap &&
            cell.z > minimumZ - unreachableGap &&
            cell.z < maximumZ + unreachableGap;

        return insideOuterBounds && !insideProtectedDungeon;
    }

    private void AddAerialBridges(int highestDungeonLayer, int maximumLayer)
    {
        List<Vector3Int> snapshot = new(cells.Keys);
        if (snapshot.Count == 0)
        {
            return;
        }

        int bridgeCount = Mathf.Max(4, districtCount * 3);

        for (int bridge = 0; bridge < bridgeCount; bridge++)
        {
            Vector3Int start = snapshot[Next(0, snapshot.Count)];
            int bridgeLayer = Next(highestDungeonLayer + 1, maximumLayer + 1);
            AddVerticalColumn(start.x, start.z, start.y, bridgeLayer);

            Vector3Int direction = GetRandomHorizontalDirection();
            int bridgeLength = Next(4, 11);
            Vector3Int cell = new Vector3Int(start.x, bridgeLayer, start.z);

            for (int step = 0; step < bridgeLength; step++)
            {
                AddCell(cell, CellStyle.Room);
                cell += direction;

                if (step > 1 && Roll(0.18f))
                {
                    direction = GetPerpendicular(direction);
                }
            }

            // Keep bridge supports above the playable dungeon even if a bridge
            // happens to cross over its horizontal footprint.
            AddVerticalColumn(cell.x, cell.z, highestDungeonLayer + 1, bridgeLayer);
            AddCell(cell, CellStyle.Landmark);
        }
    }

    private void AddVerticalColumn(int x, int z, int firstLayer, int secondLayer)
    {
        int bottom = Mathf.Min(firstLayer, secondLayer);
        int top = Mathf.Max(firstLayer, secondLayer);

        for (int y = bottom; y <= top; y++)
        {
            AddCell(new Vector3Int(x, y, z), CellStyle.Vertical);
        }
    }

    private void AddCell(Vector3Int cell, CellStyle style)
    {
        if (!cells.TryGetValue(cell, out CellStyle existingStyle) || style > existingStyle)
        {
            cells[cell] = style;
        }
    }

    private void SpawnCells()
    {
        foreach (KeyValuePair<Vector3Int, CellStyle> entry in cells)
        {
            GameObject prefab = SelectPrefab(entry.Value);
            Vector3Int cell = entry.Key;
            Vector3 position = new Vector3(
                cell.x * roomSpacing.x,
                cell.y * roomSpacing.y,
                cell.z * roomSpacing.z);

            Quaternion rotation = Quaternion.Euler(0f, Next(0, 4) * 90f, 0f);
            spawnedObjects.Add(Instantiate(prefab, position, rotation, backgroundParent));
        }
    }

    private GameObject SelectPrefab(CellStyle style)
    {
        if (style == CellStyle.Landmark && landmarkPrefab != null)
        {
            return landmarkPrefab;
        }

        if (style == CellStyle.Vertical && verticalPrefab != null)
        {
            return verticalPrefab;
        }

        return roomPrefab;
    }

    private Vector3Int GetRandomHorizontalDirection()
    {
        switch (Next(0, 4))
        {
            case 0: return Vector3Int.right;
            case 1: return Vector3Int.left;
            case 2: return Vector3Int.forward;
            default: return Vector3Int.back;
        }
    }

    private Vector3Int GetPerpendicular(Vector3Int direction)
    {
        Vector3Int perpendicular = direction.x != 0 ? Vector3Int.forward : Vector3Int.right;
        return Roll(0.5f) ? perpendicular : -perpendicular;
    }

    private bool Roll(float chance)
    {
        return random.NextDouble() < chance;
    }

    private int Next(int minimumInclusive, int maximumExclusive)
    {
        return random.Next(minimumInclusive, maximumExclusive);
    }

    private void FindExistingBounds(
        out int minimumX,
        out int maximumX,
        out int minimumZ,
        out int maximumZ,
        out int lowestLayer,
        out int highestLayer)
    {
        Transform first = existingDungeonParent.GetChild(0);
        minimumX = maximumX = Mathf.RoundToInt(first.position.x / roomSpacing.x);
        minimumZ = maximumZ = Mathf.RoundToInt(first.position.z / roomSpacing.z);
        lowestLayer = highestLayer = Mathf.RoundToInt(first.position.y / roomSpacing.y);

        for (int i = 1; i < existingDungeonParent.childCount; i++)
        {
            Vector3 position = existingDungeonParent.GetChild(i).position;
            int x = Mathf.RoundToInt(position.x / roomSpacing.x);
            int y = Mathf.RoundToInt(position.y / roomSpacing.y);
            int z = Mathf.RoundToInt(position.z / roomSpacing.z);

            minimumX = Mathf.Min(minimumX, x);
            maximumX = Mathf.Max(maximumX, x);
            minimumZ = Mathf.Min(minimumZ, z);
            maximumZ = Mathf.Max(maximumZ, z);
            lowestLayer = Mathf.Min(lowestLayer, y);
            highestLayer = Mathf.Max(highestLayer, y);
        }
    }

    private void ClearPreviousDungeon()
    {
        cells.Clear();

        foreach (GameObject instance in spawnedObjects)
        {
            if (instance != null)
            {
                Destroy(instance);
            }
        }

        spawnedObjects.Clear();
    }
}
