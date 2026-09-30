using TMPro;
using Unity.AppUI.UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class RunePanel : MonoBehaviour
{
    [SerializeField] private RuneSO rune;
    [SerializeField] private TextMeshProUGUI runeName;
    [SerializeField] private TextMeshProUGUI runeDescription;
    [SerializeField] private TextMeshProUGUI buttonText;
    public GameObject button;

    public int runeId;
    private RuneManager runeManager;
    private RuneLibrary runeLibrary;
    public bool isEquipped = false;
    public Pedestal pedestal;

    public void InitializePanel(RuneSO assignedRune, Pedestal pedestal)
    {
        runeManager = FindAnyObjectByType<RuneManager>();
        runeLibrary = FindAnyObjectByType<RuneLibrary>();
        rune = assignedRune;
        this.pedestal = pedestal;

        runeId = rune.RuneID;
        runeName.text = rune.RuneName;
        runeDescription.text = rune.RuneDescription;
        if(pedestal.runeOnPedastal == runeId)
        {
            buttonText.text = "Unequip";
            isEquipped = true;
        } 
        else
        {
            buttonText.text = "Equip";
            isEquipped = false;
        }
    }


    public void HandleRuneEquipButton()
    {
        if(runeLibrary.IsRuneUnlocked(runeId) && !isEquipped)
        {
            runeManager.EquipRune(runeId);
            isEquipped = true;
            buttonText.text = "Equip";
            pedestal.runeOnPedastal = runeId;
            runeLibrary.IntializeRuneSelection(runeLibrary.contentContainer, runeId, pedestal);
        } 
        else if(runeLibrary.IsRuneUnlocked(runeId) && isEquipped)
        {
            runeManager.UnequipRune(runeId);
            isEquipped = false;
            buttonText.text = "Unequip";
            pedestal.runeOnPedastal = 0;
            runeLibrary.IntializeRuneSelection(runeLibrary.contentContainer, 0, pedestal);
        }
        
    }

}
