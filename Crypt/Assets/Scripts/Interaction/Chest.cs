using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    private string interactText = "Open Chest";
    public string InteractText => interactText;


    public void AlignChest(Vector3Int direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(-direction) * Quaternion.Euler(0f, -90f, 0f);

        gameObject.transform.localRotation = targetRotation;
    }

    public void Interact(PlayerController player)
    {
        
    }
}
