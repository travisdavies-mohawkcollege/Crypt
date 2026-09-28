using UnityEngine;

public class RoomTriggers : MonoBehaviour
{
    [SerializeField] private Torch[] torches;
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            foreach(Torch torch in torches)
            {
                torch.TurnOnTorch();
            }
        }
    }
}
