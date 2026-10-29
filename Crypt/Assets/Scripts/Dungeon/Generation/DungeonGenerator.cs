using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation;

// Generates the dungeon's runtime cell graph, instantiates room views, and rebuilds navigation.
public class DungeonGenerator : MonoBehaviour
{
    [Header("Generation Variables")]
    [Tooltip("These can be altered by runes in game to change how the dungeon generates.")]
    [SerializeField] Vector3Int gridSize = new Vector3Int(10, 3, 10);
    [SerializeField] private int targetRooms;
    [SerializeField] private float loopChance;
    [SerializeField] private float straightChance;
    [SerializeField] private float nextFloorChanceMin;
    [SerializeField] private float lootRoomChance;
    [SerializeField] private int numberOfBranches;
    [SerializeField] private int branchLength;
    [SerializeField] private bool generateBranches;

    private int dungeonLevel;
    private float nextFloorChance;



    //This decides the physical size of each cell.
    //Room prefabs need to be scaled accordingly.
    [SerializeField] private Vector3 roomSpacing = new Vector3(12f, 4f, 12f);

    //Authored modules are selected during layout generation and spawned after the layout is complete.
    [Header("Room Prefabs")]
    [SerializeField] private ModuleCatalog moduleCatalog;
    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private GameObject chestRoomPrefab;

    [Header("Module Generation")]
    [Range(0f, 1f)]
    [SerializeField] private float moduleChance = 0.2f;
    [Range(0f, 1f)]
    [SerializeField] private float mediumModuleChance = 0.25f;
    [Range(0f, 1f)]
    [SerializeField] private float largeModuleChance = 0.1f;
    [SerializeField] private int modulePlacementAttempts = 6;

    [Header("Misc.")]
    //This is just to keep the scene organized and spawn all rooms under one parent object.
    [SerializeField] private Transform roomParent;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private NavMeshSurface navMeshSurface;
    private Vector3Int lastCell;
    private Vector3Int lastBranchCell;
    private int branchCount = 0;

    //This keeps track of the rooms position and its data.
    private Dictionary<Vector3Int, RoomData> rooms = new();
    private List<PlacedModule> modules = new();
    private List<Vector3Int> expandableRooms = new();
    private HashSet<Vector3Int> occupiedCells = new();
    private List<PlacedEntrance> availableEntrances = new();


    //This is essentially a list of directions the dungeon can spread too.
    //Vertical directions have been intentionally removed from here to be handled carefully.
    private static readonly Vector3Int[] Directions =
    {
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.forward,
        Vector3Int.back,
    };

    public void InitializeGenerator(int TargetRooms, float LoopChance, float StraightChance, float NextFloorChanceMin, Vector3Int GridSize, int NumberOfBranches, int BranchLength, bool GenerateBranches, int DungeonLevel)
    {
        // Copy the final DungeonManager values so one generation run uses a stable configuration.
        targetRooms = TargetRooms;
        loopChance = LoopChance;
        straightChance = StraightChance;
        nextFloorChanceMin = NextFloorChanceMin;
        nextFloorChance = nextFloorChanceMin;
        gridSize = GridSize;
        numberOfBranches = NumberOfBranches;
        branchLength = BranchLength;
        generateBranches = GenerateBranches;
        dungeonLevel = Mathf.Clamp(DungeonLevel, 1, 60);
    }

    public void GenerateDungeon()
    {
        // Layout data is completed before any scene objects or NavMesh data are created.
        branchCount = 0;
        if (!GenerateLayout(targetRooms))
        {
            return;
        }

        if (generateBranches) GenerateBranches(numberOfBranches, branchLength);
        TryPlaceExitModule();
        AddLoops(loopChance);
        SpawnModules();
        SpawnRooms();
        GenerateNavMesh();
    }

    bool GenerateLayout(int targetRoomCount)
    {
        //Ensure theres no rooms already.
        modules.Clear();
        rooms.Clear();
        expandableRooms.Clear();
        occupiedCells.Clear();
        availableEntrances.Clear();

        if (moduleCatalog == null || moduleCatalog.EntranceModule == null || moduleCatalog.ExitModule == null)
        {
            Debug.LogError("Dungeon Generator needs an entrance and exit module in its Module Catalog.");
            return false;
        }

        //Target room count now represents the total number of occupied grid cells.
        int maxCells = gridSize.x * gridSize.y * gridSize.z;
        int entranceCellCount = moduleCatalog.EntranceModule.OccupiedCells.Count;
        int exitCellCount = moduleCatalog.ExitModule.OccupiedCells.Count;
        int minimumCellCount = entranceCellCount + exitCellCount + 1;

        if (minimumCellCount > maxCells)
        {
            Debug.LogError("The dungeon grid is too small for the entrance and exit modules.");
            return false;
        }

        targetRoomCount = Mathf.Clamp(targetRoomCount, minimumCellCount, maxCells);

        //Leave enough room in the cell budget for the exit module at the end.
        int layoutTargetCellCount = targetRoomCount - exitCellCount;
        Vector3Int startCell = new Vector3Int(gridSize.x / 2, 0, gridSize.z / 2);

        if (!TryPlaceStartingModule(startCell, layoutTargetCellCount, out PlacedModule startingModule))
        {
            Debug.LogError("The entrance module could not fit at the dungeon start position.");
            return false;
        }

        Stack<Vector3Int> path = new();

        if (!TryCreateRoomFromModule(startingModule, out RoomData startingRoom))
        {
            Debug.LogError("The entrance module does not have an entrance that can open into a hallway.");
            return false;
        }

        path.Push(startingRoom.Cell);
        lastCell = startingRoom.Cell;

        //Continue from hallway cells and unused module entrances until the cell budget is reached.
        while (occupiedCells.Count < layoutTargetCellCount && (path.Count > 0 || availableEntrances.Count > 0))
        {
            //If the current hallway path ends, begin another path from an unused module entrance.
            if (path.Count == 0)
            {
                if (!TryCreateRoomFromRandomEntrance(out RoomData entranceRoom))
                {
                    break;
                }

                path.Push(entranceRoom.Cell);
                lastCell = entranceRoom.Cell;
                continue;
            }

            //This looks at the most recent cell on our path.
            Vector3Int currentCell = path.Peek();

            //Get a list of free directions.
            List<Vector3Int> availableDirections = GetAvailableDirections(currentCell);

            //This will attempt to generate a new floor if it cannot expand horizontally.
            if (availableDirections.Count == 0)
            {
                if (occupiedCells.Count < layoutTargetCellCount && currentCell.y != gridSize.y - 1 && IsCellFree(currentCell + Vector3Int.up) && GenerateNextFloor())
                {
                    rooms[currentCell].IsStairway = true;
                    //Make new cell in position above.
                    Vector3Int _newCell = currentCell + Vector3Int.up;
                    RoomData _newRoom = new RoomData(_newCell);
                    //Tell the cell where we entered from.
                    _newRoom.EntryDirection = Vector3Int.up;
                    //Add the cell to our dicontary.
                    CommitRoom(_newRoom);
                    //This informs the cell that it connects to a cell above it.
                    //It also informs the new cell that there is a cell below it.
                    ConnectRooms(rooms[currentCell], _newRoom);
                    //Add a new cell to expand from.
                    expandableRooms.Add(_newCell);
                    //Put this on top of our path stack.
                    path.Push(_newCell);
                    lastCell = _newCell;
                    continue;
                }

                //If we couldn't expand horizontally or vertically, remove this from
                //our path.
                if (availableDirections.Count == 0)
                {
                    path.Pop();
                    expandableRooms.Remove(currentCell);
                }
                continue;
            }

            //Check where we came from.
            Vector3Int previousDirection = rooms[currentCell].EntryDirection;
            //Try to pick the same direction to make more maze like dungeons.
            Vector3Int direction = ChooseDirection(availableDirections, previousDirection, straightChance);

            //Sometimes replace the next hallway cell with an authored room module.
            if (Random.value < moduleChance && TryPlaceRandomModuleFromRoom(rooms[currentCell], direction, layoutTargetCellCount, out PlacedModule placedModule))
            {
                //Continue the current path through one of the new module's unused entrances.
                if (occupiedCells.Count < layoutTargetCellCount && TryCreateRoomFromModule(placedModule, out RoomData continuationRoom))
                {
                    path.Push(continuationRoom.Cell);
                    lastCell = continuationRoom.Cell;
                }

                continue;
            }

            //Pick the new position and make a room there.
            Vector3Int newCell = currentCell + direction;
            RoomData newRoom = new RoomData(newCell);
            //Store where we came from.
            newRoom.EntryDirection = direction;
            //Add the room to our dictonary.
            CommitRoom(newRoom);
            //Connect the new room to the previous.
            ConnectRooms(rooms[currentCell], newRoom);
            //Add a new cell to expand from.
            expandableRooms.Add(newCell);
            //Mark this as the last room.
            lastCell = newCell;
            //Add the new cell to the top of our path stack.
            path.Push(newCell);
        }

        return true;
    }

    void GenerateBranches(int branches, int branchLength)
    {
        // Each successful branch grows from a randomly selected room that can still expand.
        while (branches > branchCount && expandableRooms.Count != 0)
        {
            int length = 0;
            bool branchRoomMade = false;
            Stack<Vector3Int> path = new();
            Vector3Int chosenCell = expandableRooms[Random.Range(0, expandableRooms.Count)];
            path.Push(chosenCell);
            while (length < branchLength && path.Count > 0)
            {
                Vector3Int currentCell = path.Peek();
                //get avail directions
                List<Vector3Int> availableDirections = GetAvailableDirections(currentCell);
                if (availableDirections.Count == 0)
                {
                    path.Pop();
                    expandableRooms.Remove(currentCell);
                    continue;
                }
                //mark where we came in
                Vector3Int previousDirection = rooms[currentCell].EntryDirection;
                //choose a direction
                Vector3Int direction = ChooseDirection(availableDirections, previousDirection, straightChance);

                if (Random.value < moduleChance && TryPlaceRandomModuleFromRoom(rooms[currentCell], direction, gridSize.x * gridSize.y * gridSize.z, out PlacedModule placedModule))
                {
                    branchRoomMade = true;
                    length++;

                    if (TryCreateRoomFromModule(placedModule, out RoomData continuationRoom))
                    {
                        path.Push(continuationRoom.Cell);
                        lastBranchCell = continuationRoom.Cell;
                    }
                    else
                    {
                        lastBranchCell = currentCell;
                    }

                    continue;
                }

                //make our cell there
                Vector3Int newCell = currentCell + direction;
                RoomData newRoom = new RoomData(newCell);
                newRoom.EntryDirection = direction;
                ConnectRooms(rooms[currentCell], newRoom);
                CommitRoom(newRoom);
                path.Push(newCell);
                branchRoomMade = true;
                lastBranchCell = newCell;
                length++;
            }
            if (branchRoomMade && GenerateLootRoom())
            {
                rooms[lastBranchCell].RoomType = RoomTypes.Loot;
            }
            if (branchRoomMade) branchCount++;
        }
    }


    bool GenerateLootRoom()
    {
        // Loot designation is decided after a branch has produced at least one room.
        if (Random.value < lootRoomChance)
        {
            return true;
        }
        return false;
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

    //This checks the shared room and module occupancy to see if a cell is free.
    bool IsCellFree(Vector3Int cell)
    {
        return !occupiedCells.Contains(cell);
    }

    //This checks that a given coordinate is inside the requested grid size.
    bool IsInsideGrid(Vector3Int cell)
    {
        return cell.x >= 0 && cell.x < gridSize.x && cell.y >= 0 && cell.y < gridSize.y && cell.z >= 0 && cell.z < gridSize.z;
    }

    //This checks a cell's neighbours to decide if it is within the grid or occupied.
    //It returns a list of all free directions.
    List<Vector3Int> GetAvailableDirections(Vector3Int cell, bool ignoreGridBounds = false)
    {
        List<Vector3Int> available = new();

        foreach (Vector3Int direction in Directions)
        {
            Vector3Int neighbour = cell + direction;

            bool insideGrid = ignoreGridBounds || IsInsideGrid(neighbour);

            if (insideGrid && IsCellFree(neighbour))
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
        if (availableDirections.Contains(previousDirection) && Random.value < straightChance)
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

    //Rooms and modules both reserve cells through occupiedCells.
    void CommitRoom(RoomData room)
    {
        rooms.Add(room.Cell, room);
        occupiedCells.Add(room.Cell);
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

            if (data.RoomType == RoomTypes.Loot)
            {
                instance = Instantiate(chestRoomPrefab, position, Quaternion.identity, roomParent);
            }
            else
            {
                instance = Instantiate(roomPrefab, position, Quaternion.identity, roomParent);
            }

            RoomView view = instance.GetComponent<RoomView>();
            view.Configure(data);
            if (data.RoomType == RoomTypes.Loot)
            {
                // Loot-room gameplay is initialized only after its configured room prefab exists.
                Chest chest = instance.GetComponentInChildren<Chest>();

                if (chest == null)
                {
                    Debug.LogError("Loot room does not contain a Chest.");
                    continue;
                }

                chest.AlignChest(data.EntryDirection);
                chest.InitializeChest(dungeonLevel);
            }
        }
    }

    void SpawnModules()
    {
        foreach (PlacedModule module in modules)
        {
            Quaternion rotation = Quaternion.Euler(0, module.QuarterTurns * 90, 0);
            RoomModule instance = Instantiate(module.ModulePrefab, GridToWorld(module.OriginCell), rotation, roomParent);
            module.SetInstance(instance);
            module.ConfigureEntranceVisuals();
        }
    }


    //Modules
    public static Vector3Int RotateCell(Vector3Int cell, int quarterTurns)
    {
        quarterTurns = ((quarterTurns % 4) + 4) % 4;

        return quarterTurns switch
        {
            0 => new Vector3Int(cell.x, cell.y, cell.z), // 0°
            1 => new Vector3Int(cell.z, cell.y, -cell.x), // 90°
            2 => new Vector3Int(-cell.x, cell.y, -cell.z), // 180°
            3 => new Vector3Int(-cell.z, cell.y, cell.x), // 270°
            _ => cell
        };
    }

    bool CanPlaceModule(PlacedModule module, int maxCellCount = int.MaxValue)
    {
        if (occupiedCells.Count + module.WorldCells.Count > maxCellCount)
        {
            return false;
        }

        foreach (Vector3Int cell in module.WorldCells)
        {
            //Normal modules must stay inside the grid.
            if (!module.ModulePrefab.IgnoreGridBounds && !IsInsideGrid(cell))
            {
                return false;
            }

            //Outside cells still need to be free so modules cannot overlap.
            if (!IsCellFree(cell))
            {
                return false;
            }
        }

        return true;
    }

    void ReserveModuleCells(PlacedModule module)
    {
        foreach (Vector3Int cell in module.WorldCells)
        {
            occupiedCells.Add(cell);
        }
    }

    void CommitModule(PlacedModule module)
    {
        modules.Add(module);
        ReserveModuleCells(module);

        foreach (PlacedEntrance entrance in module.Entrances)
        {
            availableEntrances.Add(entrance);
        }
    }

    bool TryPlaceModule(RoomModule modulePrefab, Vector3Int originCell, int quarterTurns, out PlacedModule placedModule, int maxCellCount = int.MaxValue)
    {
        placedModule = new PlacedModule(modulePrefab, originCell, quarterTurns);

        if (!CanPlaceModule(placedModule, maxCellCount))
        {
            placedModule = null;
            return false;
        }

        CommitModule(placedModule);
        return true;
    }

    bool TryPlaceStartingModule(Vector3Int originCell, int maxCellCount, out PlacedModule placedModule)
    {
        int firstRotation = Random.Range(0, 4);

        for (int i = 0; i < 4; i++)
        {
            int quarterTurns = (firstRotation + i) % 4;

            if (TryPlaceModule(moduleCatalog.EntranceModule, originCell, quarterTurns, out placedModule, maxCellCount))
            {
                return true;
            }
        }

        placedModule = null;
        return false;
    }

    bool TryCreateRoomFromModule(PlacedModule module, out RoomData room)
    {
        int firstEntrance = Random.Range(0, module.Entrances.Count);

        for (int i = 0; i < module.Entrances.Count; i++)
        {
            PlacedEntrance entrance = module.Entrances[(firstEntrance + i) % module.Entrances.Count];

            if (TryCreateRoomFromEntrance(entrance, out room))
            {
                return true;
            }
        }

        room = null;
        return false;
    }

    bool TryCreateRoomFromRandomEntrance(out RoomData room)
    {
        while (availableEntrances.Count > 0)
        {
            PlacedEntrance entrance = availableEntrances[Random.Range(0, availableEntrances.Count)];

            if (TryCreateRoomFromEntrance(entrance, out room))
            {
                return true;
            }

            //Occupied cells only increase, so an entrance that cannot open now will never become valid later.
            availableEntrances.Remove(entrance);
        }

        room = null;
        return false;
    }

    bool TryCreateRoomFromEntrance(PlacedEntrance entrance, out RoomData room)
    {
        room = null;

        if (entrance == null || entrance.IsConnected)
        {
            return false;
        }

        Vector3Int roomCell = entrance.OutsideCell;

        if (!IsInsideGrid(roomCell) || !IsCellFree(roomCell))
        {
            return false;
        }

        room = new RoomData(roomCell);
        room.EntryDirection = entrance.Direction;
        CommitRoom(room);
        ConnectRoomToEntrance(room, entrance);
        expandableRooms.Add(roomCell);
        return true;
    }

    void ConnectRoomToEntrance(RoomData room, PlacedEntrance entrance)
    {
        room.ConnectedCells.Add(entrance.Cell);
        entrance.ConnectRoom();
        availableEntrances.Remove(entrance);
    }

    RoomModule SelectRandomModule()
    {
        float sizeRoll = Random.value;
        RoomModule selectedModule = null;

        if (sizeRoll < largeModuleChance)
        {
            selectedModule = SelectModuleFromList(moduleCatalog.LargeModules);
        }
        else if (sizeRoll < largeModuleChance + mediumModuleChance)
        {
            selectedModule = SelectModuleFromList(moduleCatalog.MediumModules);
        }

        if (selectedModule == null)
        {
            selectedModule = SelectModuleFromList(moduleCatalog.SmallModules);
        }

        if (selectedModule == null)
        {
            selectedModule = SelectModuleFromList(moduleCatalog.MediumModules);
        }

        if (selectedModule == null)
        {
            selectedModule = SelectModuleFromList(moduleCatalog.LargeModules);
        }

        return selectedModule;
    }

    RoomModule SelectModuleFromList(IReadOnlyList<RoomModule> moduleList)
    {
        if (moduleList == null || moduleList.Count == 0)
        {
            return null;
        }

        int firstModule = Random.Range(0, moduleList.Count);

        for (int i = 0; i < moduleList.Count; i++)
        {
            RoomModule module = moduleList[(firstModule + i) % moduleList.Count];

            if (module != null && module.Type != ModuleType.Entrance && module.Type != ModuleType.Exit)
            {
                return module;
            }
        }

        return null;
    }

    bool TryPlaceRandomModuleFromRoom(RoomData sourceRoom, Vector3Int sourceDirection, int maxCellCount, out PlacedModule placedModule)
    {
        for (int attempt = 0; attempt < modulePlacementAttempts; attempt++)
        {
            RoomModule modulePrefab = SelectRandomModule();

            if (modulePrefab == null)
            {
                break;
            }

            if (TryAttachModuleToRoom(sourceRoom, sourceDirection, modulePrefab, out placedModule, maxCellCount))
            {
                return true;
            }
        }

        placedModule = null;
        return false;
    }

    bool TryAttachModuleToRoom(RoomData sourceRoom, Vector3Int sourceDirection, RoomModule modulePrefab, out PlacedModule placedModule, int maxCellCount = int.MaxValue)
    {
        placedModule = null;
        Vector3Int targetCell = sourceRoom.Cell + sourceDirection;

        for (int entranceIndex = 0; entranceIndex < modulePrefab.Entrances.Count; entranceIndex++)
        {
            Entrance candidateEntrance = modulePrefab.Entrances[entranceIndex];

            //The current hallway prefab uses archway openings.
            if (candidateEntrance.Type != EntranceType.Archway)
            {
                continue;
            }

            for (int quarterTurns = 0; quarterTurns < 4; quarterTurns++)
            {
                Vector3Int candidateDirection = RotateCell(candidateEntrance.LocalDirection, quarterTurns);

                if (candidateDirection != -sourceDirection)
                {
                    continue;
                }

                Vector3Int rotatedEntranceCell = RotateCell(candidateEntrance.LocalCell, quarterTurns);
                Vector3Int originCell = targetCell - rotatedEntranceCell;

                if (!TryPlaceModule(modulePrefab, originCell, quarterTurns, out placedModule, maxCellCount))
                {
                    continue;
                }

                PlacedEntrance connectedEntrance = placedModule.Entrances[entranceIndex];
                sourceRoom.ConnectedCells.Add(connectedEntrance.Cell);
                connectedEntrance.ConnectRoom();
                availableEntrances.Remove(connectedEntrance);
                return true;
            }
        }

        return false;
    }

    bool AreEntrancesCompatible(PlacedEntrance source, Entrance candidate)
    {
        return source.Type == candidate.Type;
    }

    Vector3Int GetModuleOrigin(PlacedEntrance sourceEntrance, Entrance candidateEntrance, int quarterTurns)
    {
        Vector3Int rotatedEntranceCell = RotateCell(candidateEntrance.LocalCell, quarterTurns);
        return sourceEntrance.OutsideCell - rotatedEntranceCell;
    }

    bool IsEntranceFacingSource(PlacedEntrance sourceEntrance, Entrance candidateEntrance, int quarterTurns)
    {
        Vector3Int candidateDirection = RotateCell(candidateEntrance.LocalDirection, quarterTurns);
        return candidateDirection == -sourceEntrance.Direction;
    }

    bool TryAttachModule(PlacedEntrance sourceEntrance, RoomModule modulePrefab, out PlacedModule placedModule, int maxCellCount = int.MaxValue)
    {
        placedModule = null;

        if (sourceEntrance == null || sourceEntrance.IsConnected || modulePrefab == null)
        {
            return false;
        }

        for (int entranceIndex = 0; entranceIndex < modulePrefab.Entrances.Count; entranceIndex++)
        {
            Entrance candidateEntrance = modulePrefab.Entrances[entranceIndex];

            if (!AreEntrancesCompatible(sourceEntrance, candidateEntrance))
            {
                continue;
            }

            for (int quarterTurns = 0; quarterTurns < 4; quarterTurns++)
            {
                if (!IsEntranceFacingSource(sourceEntrance, candidateEntrance, quarterTurns))
                {
                    continue;
                }

                Vector3Int originCell = GetModuleOrigin(sourceEntrance, candidateEntrance, quarterTurns);

                if (!TryPlaceModule(modulePrefab, originCell, quarterTurns, out placedModule, maxCellCount))
                {
                    continue;
                }

                PlacedEntrance connectedEntrance = placedModule.Entrances[entranceIndex];
                sourceEntrance.ConnectEntrance(connectedEntrance);
                availableEntrances.Remove(sourceEntrance);
                availableEntrances.Remove(connectedEntrance);
                return true;
            }
        }

        return false;
    }

    bool TryPlaceExitModule()
    {
        RoomModule exitModule = moduleCatalog.ExitModule;
        int maxCellCount = int.MaxValue;
        Vector3Int preferredCell = branchCount > 0 ? lastBranchCell : lastCell;

        if (rooms.TryGetValue(preferredCell, out RoomData preferredRoom) && TryAttachModuleToAnySide(preferredRoom, exitModule, out PlacedModule placedExit, maxCellCount))
        {
            return true;
        }

        List<Vector3Int> roomCells = new(rooms.Keys);
        int firstRoom = roomCells.Count > 0 ? Random.Range(0, roomCells.Count) : 0;

        for (int i = 0; i < roomCells.Count; i++)
        {
            RoomData room = rooms[roomCells[(firstRoom + i) % roomCells.Count]];

            if (room == preferredRoom)
            {
                continue;
            }

            if (TryAttachModuleToAnySide(room, exitModule, out placedExit, maxCellCount))
            {
                return true;
            }
        }

        List<PlacedEntrance> entranceOptions = new(availableEntrances);
        int firstEntrance = entranceOptions.Count > 0 ? Random.Range(0, entranceOptions.Count) : 0;

        for (int i = 0; i < entranceOptions.Count; i++)
        {
            PlacedEntrance entrance = entranceOptions[(firstEntrance + i) % entranceOptions.Count];

            if (TryAttachModule(entrance, exitModule, out placedExit, maxCellCount))
            {
                return true;
            }
        }

        Debug.LogError("The exit module could not be connected to the generated dungeon.");
        return false;
    }

    bool TryAttachModuleToAnySide(RoomData sourceRoom, RoomModule modulePrefab, out PlacedModule placedModule, int maxCellCount)
    {
        List<Vector3Int> directions = GetAvailableDirections(sourceRoom.Cell, modulePrefab.IgnoreGridBounds);

        if (directions.Count == 0)
        {
            placedModule = null;
            return false;
        }

        int firstDirection = Random.Range(0, directions.Count);

        for (int i = 0; i < directions.Count; i++)
        {
            Vector3Int direction = directions[(firstDirection + i) % directions.Count];

            if (TryAttachModuleToRoom(sourceRoom, direction, modulePrefab, out placedModule, maxCellCount))
            {
                return true;
            }
        }

        placedModule = null;
        return false;
    }


    void GenerateNavMesh()
    {
        // Build once after all generated geometry and spawn-point objects are in the scene.
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }
    }
}
