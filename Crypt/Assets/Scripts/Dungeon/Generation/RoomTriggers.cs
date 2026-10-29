using UnityEngine;

// Activates a room's local lighting the first time a player enters its trigger volume.
public class RoomTriggers : MonoBehaviour
{
    [SerializeField] private Torch[] torches;
    private void OnTriggerEnter(Collider other)
    {
        // Ignore enemies and physics props that share the generated room.
        if(other.CompareTag("Player"))
        {
            foreach(Torch torch in torches)
            {
                torch.TurnOnTorch();
            }
        }
    }
}
