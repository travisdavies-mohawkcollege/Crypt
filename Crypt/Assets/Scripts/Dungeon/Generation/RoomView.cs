using UnityEngine;


// Configures a spawned single-cell room from its generated RoomData.
// Connected neighbours remove the separating wall or vertical surface.
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
    [SerializeField] private GameObject northTorch;
    [SerializeField] private GameObject southTorch;
    [SerializeField] private GameObject eastTorch;
    [SerializeField] private GameObject westTorch;

    public void Configure(RoomData data)
    {
        // A surface remains active only when no connected cell exists on that side.
        northWall.SetActive(!data.ConnectedCells.Contains(data.Cell + Vector3Int.forward));
        northTorch.SetActive(!data.ConnectedCells.Contains(data.Cell + Vector3Int.forward));
        southWall.SetActive(!data.ConnectedCells.Contains(data.Cell + Vector3Int.back));
        southTorch.SetActive(!data.ConnectedCells.Contains(data.Cell + Vector3Int.back));
        eastWall.SetActive(!data.ConnectedCells.Contains(data.Cell + Vector3Int.right));
        eastTorch.SetActive(!data.ConnectedCells.Contains(data.Cell + Vector3Int.right));
        westWall.SetActive(!data.ConnectedCells.Contains(data.Cell + Vector3Int.left));
        westTorch.SetActive(!data.ConnectedCells.Contains(data.Cell + Vector3Int.left));
        floor.SetActive(!data.ConnectedCells.Contains(data.Cell + Vector3Int.down));
        ceiling.SetActive(!data.ConnectedCells.Contains(data.Cell + Vector3Int.up));
    }
}
