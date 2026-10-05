using Unity.VisualScripting;
using UnityEngine;

public class Pedestal : MonoBehaviour, IInteractable
{
    private string interactText = "Select Rune";
    public string InteractText => interactText;
    [SerializeField] private int pedestalNumber;
    [SerializeField] private GameObject rune;
    public int runeOnPedastal = 0;


    
    public void Interact(PlayerController player)
    {
        player.runeSelectionCanvas.SetActive(true);
        player.SetCursorLocked(false);
        player.ActivateRunePanel(this, runeOnPedastal);
    }

    public void Update()
    {
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
