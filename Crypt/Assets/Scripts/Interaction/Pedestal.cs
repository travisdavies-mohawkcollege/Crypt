using Unity.VisualScripting;
using UnityEngine;

// Opens rune selection for one pedestal and displays whether that pedestal holds a rune.
public class Pedestal : MonoBehaviour, IInteractable
{
    private string interactText = "Select Rune";
    public string InteractText => interactText;
    [SerializeField] private int pedestalNumber;
    [SerializeField] private GameObject rune;
    public int runeOnPedastal = 0;


    
    public void Interact(PlayerController player)
    {
        // The panel receives this pedestal so its selection is written back to the correct slot.
        player.runeSelectionCanvas.SetActive(true);
        player.SetCursorLocked(false);
        player.ActivateRunePanel(this, runeOnPedastal);
    }

    public void Update()
    {
        // Keep the pedestal's physical rune indicator synchronized with its stored rune ID.
        if(runeOnPedastal != 0)
        {
            rune.SetActive(true);
        }
        else
        {
            rune.SetActive(false);
        }
    }
}
