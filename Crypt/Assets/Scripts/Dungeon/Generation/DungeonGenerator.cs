using UnityEngine;
using System.Collections.Generic;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Generation Variables")]
    [Tooltip("These can be altered by runes in game to change how the dungeon generates.")]
    [SerializeField] Vector3Int gridSize = new Vector3Int(10, 3, 10);
    [SerializeField] private int targetRooms;
    [SerializeField] private float loopChance;
    [SerializeField] private float straightChance;
    [SerializeField] private float nextFloorChanceMin;
    private float nextFloorChance;

    

    //This decides the physical size of each cell.
    //Room prefabs need to be scaled accordingly.
    [SerializeField] private Vector3 roomSpacing = new Vector3(12f, 4f, 12f);

    //We can use prefabs to spawn multiple cell premade rooms.
    //This code is NOT implemeted yet!
    [Header("Room Prefabs")]
    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private GameObject spawnRoomPrefab;
    [SerializeField] private GameObject escapeRoomPrefab;

    [Header("Misc.")]
    //This is just to keep the scene organized and spawn all rooms under one parent object.
    [SerializeField] private Transform roomParent;
    [SerializeField] private PlayerManager playerManager;
    private Vector3 lastCell;

    private bool firstRoom = true;

    //This keeps track of the rooms position and its data.
    private Dictionary<Vector3Int, RoomData> rooms = new();

    
    //This is essentially a list of directions the dungeon can spread too.
    //Vertical directions have been intentionally removed from here to be handled carefully.
    private static readonly Vector3Int[] Directions =
    {
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.forward,
        Vector3Int.back,
    };

    public void InitializeGenerator(int TargetRooms, float LoopChance, float StraightChance, float NextFloorChanceMin, Vector3Int GridSize)
    {
        targetRooms = TargetRooms;
        loopChance = LoopChance;
        straightChance = StraightChance;
        nextFloorChanceMin = NextFloorChanceMin;
        nextFloorChance = nextFloorChanceMin;
        gridSize = GridSize;
    }

    private void Start()
    {

    }
    
    public void GenerateDungeon()
    {
        firstRoom = true;
        GenerateLayout(targetRooms);
        AddLoops(loopChance);
        SpawnRooms();
        playerManager.SpawnPlayerToSpawnPoint();
    }

    void GenerateLayout(int targetRoomCount)
    {
        //Ensure theres no rooms already.
        rooms.Clear();

        //Max rooms cannot excede the size of the grid.
        int maxRooms = gridSize.x * gridSize.y * gridSize.z;
        //If desired rooms is greater than the greatest amount possible, cap it.
        targetRoomCount = Mathf.Clamp(targetRoomCount, 3, maxRooms);

        //This decides the where the dungeon starts generating.
        //We can customize this to start higher or lower, or in the center.
        Vector3Int startCell = new Vector3Int(gridSize.x / 2, 0, gridSize.z / 2);
        //"Create" the first room by adding it to our dictionary.
        rooms.Add(startCell, new RoomData(startCell));
        //This list keeps track of rooms that can be expanded in case we hit a dead end.
        List<Vector3Int> expandableRooms = new() { startCell };
        //This keeps the dungeon attempting to generate along it's path rather then going
        //back randomly to continue generation.
        Stack<Vector3Int> path = new();
        path.Push(startCell);


        //This loop runs if we still want rooms and there is still somewhere to expand from.
        while (rooms.Count < targetRoomCount && expandableRooms.Count > 0)
        {
            //This looks at the most recent cell on our path.
            Vector3Int currentCell = path.Peek();

            //Get a list of free directions.
            List<Vector3Int> availableDirections = GetAvailableDirections(currentCell);

            //This will attempt to generate a new floor if it cannot expand horizontally OR by chance.
            if(availableDirections.Count == 0 || GenerateNextFloor())
            {
                if (GenerateNextFloor() && currentCell.y != gridSize.y - 1 && IsCellFree(currentCell + Vector3Int.up))
                {
                    rooms[currentCell].IsStairway = true;
                    //Make new cell in position above.
                    Vector3Int _newCell = currentCell + Vector3Int.up;
                    RoomData _newRoom = new RoomData(_newCell);
                    //Tell the cell where we entered from.
                    _newRoom.EntryDirection = Vector3Int.up;
                    //Add the cell to our dicontary.
                    rooms.Add(_newCell, _newRoom);
                    //This informs the cell that it connects to a cell above it.
                    //It also informs the new cell that there is a cell below it.
                    ConnectRooms(rooms[currentCell], _newRoom);
                    //Add a new cell to expand from.
                    expandableRooms.Add(_newCell);
                    //Put this on top of our path stack.
                    path.Push(_newCell);
                    continue;
                }
                //If we couldn't expand horizontally or vertically, this remove this from
                //our path.
                if(availableDirections.Count == 0)path.Pop();
                continue;
            }

            //Check where we came from.
            Vector3Int previousDirection = rooms[currentCell].EntryDirection;
            //Try to pick the same direction to make more maze like dungeons.
            Vector3Int direction = ChooseDirection(availableDirections, previousDirection, straightChance);
            //Pick the new position and make a room there.
            Vector3Int newCell = currentCell + direction;
            RoomData newRoom = new RoomData(newCell);
            //Store where we came from.
            newRoom.EntryDirection = direction;
            //Add the room to our dictonary.
            rooms.Add(newCell, newRoom);
            //Connect the new room to the previous.
            ConnectRooms(rooms[currentCell], newRoom);
            //Add a new cell to expand from.
            expandableRooms.Add(newCell);
            //Mark this as the last room.
            lastCell = newCell;
            //Add the new cell to the top of our path stack.
            path.Push(newCell);
        }

    }

    bool GenerateNextFloor()
    {
        //Slowly increases the chance that a room is able to spawn the next floor.
        if (Random.value < nextFloorChance)
        {
            nextFloorChance = nextFloorChanceMin;
            return true;
        }
        nextFloorChance += 0.1f;
        return false;
    }

    //This returns the position of a cell in world space.
    Vector3 GridToWorld(Vector3Int cell)
    {
        return new Vector3(cell.x * roomSpacing.x, cell.y * roomSpacing.y, cell.z * roomSpacing.z);
    }

    //This checks our dictonary to see if a cell is free.
    bool IsCellFree(Vector3Int cell)
    {
        return !rooms.ContainsKey(cell);
    }

    //This checks that a given coordinate is inside the requested grid size.
    bool IsInsideGrid(Vector3Int cell)
    {
        return cell.x >= 0 && cell.x < gridSize.x && cell.y >= 0 && cell.y < gridSize.y && cell.z >= 0 && cell.z < gridSize.z;
    }

    //This checks a cell's neighbours to decide if it is within the grid or occupied.
    //It returns a list of all free directions.
    List<Vector3Int> GetAvailableDirections(Vector3Int cell)
    {
        List<Vector3Int> available = new();

        foreach (Vector3Int direction in Directions)
        {
            Vector3Int neighbour = cell + direction;

            if (IsInsideGrid(neighbour) && IsCellFree(neighbour))
            {
                available.Add(direction);
            }

        }
        return available;
    }

    //This picks a direction that is weighted towards going straight.
    //If it doesn't or can't go straight it returns a random directions from free ones.
    Vector3Int ChooseDirection(List<Vector3Int> availableDirections, Vector3Int previousDirection, float straightChance)
    {
        if(availableDirections.Contains(previousDirection) && Random.value < straightChance)
        {
            return previousDirection;
        }

        return availableDirections[Random.Range(0, availableDirections.Count)];
    }

    //This just tells two rooms that they are connected.
    void ConnectRooms(RoomData a, RoomData b)
    {
        b.ConnectedCells.Add(a.Cell);
        a.ConnectedCells.Add(b.Cell);
    }

    //This is a second pass over the dungeon to attempt to connect
    //generic rooms to each other if they are neighbours.
    void AddLoops(float loopChance)
    {
        foreach (RoomData room in rooms.Values)
        {
            foreach (Vector3Int direction in Directions)
            {
                Vector3Int neighbourCell = room.Cell + direction;
                if (!rooms.TryGetValue(neighbourCell, out RoomData neighbour))
                {
                    continue;
                }
                if (room.ConnectedCells.Contains(neighbourCell))
                {
                    continue;
                }
                //Even if it meets all requirements, it is still chance based.
                if (Random.value < loopChance)
                {
                    ConnectRooms(room, neighbour);
                }
            }
        }
    }

    void SpawnRooms()
    {
        //This reads our room's data, spawns a prefab at it's position,
        //then asks RoomView to customize it according to it's data.
        foreach (RoomData data in rooms.Values)
        {
            Vector3 position = GridToWorld(data.Cell);
            GameObject instance;
            if(firstRoom) 
            {
                instance = Instantiate(spawnRoomPrefab, position, Quaternion.identity, roomParent);
                firstRoom = false;
            }
            else if(data.Cell == lastCell) { instance = Instantiate(escapeRoomPrefab, position, Quaternion.identity, roomParent); }
            else { instance = Instantiate(roomPrefab, position, Quaternion.identity, roomParent); }

            RoomView view = instance.GetComponent<RoomView>();
            view.Configure(data);
        }
    }
}