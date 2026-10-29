using UnityEngine;

//This stores the runtime information for one entrance on a placed module.
public class PlacedEntrance
{
    public PlacedModule Module;
    public int EntranceIndex;

    public Vector3Int Cell;
    public Vector3Int Direction;

    //If this is null, the entrance has not been connected.
    public PlacedEntrance ConnectedEntrance;
    public bool ConnectedToRoom;

    public bool IsConnected => ConnectedEntrance != null || ConnectedToRoom;

    //This is the first grid cell outside of the module.
    public Vector3Int OutsideCell => Cell + Direction;

    //This gets the original entrance information from the prefab.
    public Entrance EntranceData => Module.ModulePrefab.Entrances[EntranceIndex];

    public EntranceType Type => EntranceData.Type;

    public PlacedEntrance(PlacedModule module, int entranceIndex, Vector3Int cell, Vector3Int direction)
    {
        this.Module = module;
        this.EntranceIndex = entranceIndex;
        this.Cell = cell;
        this.Direction = direction;
    }

    public void ConnectEntrance(PlacedEntrance entrance)
    {
        if (entrance == null || entrance == this || IsConnected || entrance.IsConnected)
        {
            return;
        }

        ConnectedEntrance = entrance;
        entrance.ConnectedEntrance = this;
    }

    public void ConnectRoom()
    {
        if (IsConnected)
        {
            return;
        }

        ConnectedToRoom = true;
    }
}
