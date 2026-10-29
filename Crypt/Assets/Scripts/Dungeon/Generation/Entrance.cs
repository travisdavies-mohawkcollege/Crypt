using System.Collections.Generic;
using UnityEngine;

// Describes one authored connection socket on a room module prefab.
public class Entrance : MonoBehaviour
{
    [SerializeField] private EntranceType entranceType;
    [SerializeField] private EntranceDirection direction;
    [SerializeField] private Vector3Int localCell;
    public Vector3Int LocalCell => localCell;

    public EntranceType Type => entranceType;
    public EntranceDirection Direction => direction;

    // Directions are local to the module; module rotation must be applied during placement.
    // Vertical entries are reserved but currently fall back to zero until they are implemented.
    public Vector3Int LocalDirection => direction switch
    {
        EntranceDirection.North => Vector3Int.forward,
        EntranceDirection.East => Vector3Int.right,
        EntranceDirection.South => Vector3Int.back,
        EntranceDirection.West => Vector3Int.left,
        EntranceDirection.Up => Vector3Int.up,
        EntranceDirection.Down => Vector3Int.down,
        _ => Vector3Int.zero
    };

    [Header("Entrance Visuals")]
    [SerializeField] private GameObject blockingWall;
    [SerializeField] private GameObject connectedVisual;

    [Header("Entrance Collision")]
    [SerializeField] private List<Collider> blockingColliders = new();

    public void SetConnected(bool connected)
    {
        if (blockingWall != null)
        {
            blockingWall.SetActive(!connected);
        }

        foreach (Collider blockingCollider in blockingColliders)
        {
            if (blockingCollider != null)
            {
                blockingCollider.enabled = !connected;
            }
        }

        if (connectedVisual != null)
        {
            connectedVisual.SetActive(connected);
        }
    }
}
