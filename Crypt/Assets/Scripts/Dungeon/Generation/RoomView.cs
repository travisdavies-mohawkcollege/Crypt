using UnityEngine;


// This handles customizing the rooms when they spawn. 
// It allows us to connect rooms, enable or disable decorations.
// I will later expand this in order to have preset room decorations that 
// we can set in the RoomData.
public class RoomView : MonoBehaviour
{
    [Header("Room Pieces")]
    [Tooltip("All pieces of a room that can be turned on or off must be placed here. This includes decorations.")]
    [SerializeField] private GameObject northWall;
    [SerializeField] private GameObject southWall;
    [SerializeField] private GameObject eastWall;
    [SerializeField] private GameObject westWall;
    [SerializeField] private GameObject floor;
    [SerializeField] private GameObject ceiling;

    public void Configure(RoomData data)
    {
        northWall.SetActive(!data.ConnectedCells.Contains(data.Cell + Vector3Int.forward));
        southWall.SetActive(!data.ConnectedCells.Contains(data.Cell + Vector3Int.back));
        eastWall.SetActive(!data.ConnectedCells.Contains(data.Cell + Vector3Int.right));
        westWall.SetActive(!data.ConnectedCells.Contains(data.Cell + Vector3Int.left));
        floor.SetActive(!data.ConnectedCells.Contains(data.Cell + Vector3Int.down));
        ceiling.SetActive(!data.ConnectedCells.Contains(data.Cell + Vector3Int.up));
    }
}
