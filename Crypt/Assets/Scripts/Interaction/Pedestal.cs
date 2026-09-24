using Unity.VisualScripting;
using UnityEngine;

public class Pedestal : MonoBehaviour, IInteractable
{
    private string interactText = "Select Runes";
    public string InteractText => interactText;


    
    public void Interact(PlayerController player)
    {
        player.runeSelectionCanvas.SetActive(true);
        player.SetCursorLocked(false);
    }
}
