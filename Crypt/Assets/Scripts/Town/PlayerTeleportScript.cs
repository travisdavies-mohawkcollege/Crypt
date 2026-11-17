using UnityEngine;

public class PlayerTeleportTrigger : MonoBehaviour
{
    [SerializeField] private Transform destination;
    [SerializeField] private bool copyDestinationRotation = true;

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || destination == null) return;

        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController != null) characterController.enabled = false;

        if (copyDestinationRotation) player.transform.SetPositionAndRotation(destination.position, destination.rotation);
        else player.transform.position = destination.position;

        if (characterController != null) characterController.enabled = true;

        Physics.SyncTransforms();
    }
}