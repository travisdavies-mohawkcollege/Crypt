using TMPro;
using UnityEngine;

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

    public void InitializePanel(RuneSO assignedRune, Pedestal assignedPedestal)
    {
        runeManager = FindAnyObjectByType<RuneManager>();
        runeLibrary = FindAnyObjectByType<RuneLibrary>();
        rune = assignedRune;
        pedestal = assignedPedestal;

        if (rune == null || pedestal == null || runeManager == null || runeLibrary == null)
        {
            Debug.LogError("RunePanel could not initialize because a required reference is missing.");
            return;
        }

        runeId = rune.RuneID;
        runeName.text = rune.RuneName;
        runeDescription.text = rune.RuneDescription;

        isEquipped = pedestal.runeOnPedastal == runeId;
        buttonText.text = isEquipped ? "Unequip" : "Equip";
    }

    public void HandleRuneEquipButton()
    {
        if (rune == null || pedestal == null || runeManager == null || runeLibrary == null) return;
        if (!runeLibrary.IsRuneUnlocked(runeId)) return;

        if (!isEquipped)
        {
            if (!runeManager.EquipRune(runeId, rune.RuneLevel)) return;

            pedestal.runeOnPedastal = runeId;
            isEquipped = true;
            buttonText.text = "Unequip";
            runeLibrary.IntializeRuneSelection(runeLibrary.contentContainer, runeId, pedestal);
            return;
        }

        if (!runeManager.UnequipRune(runeId)) return;

        pedestal.runeOnPedastal = 0;
        isEquipped = false;
        buttonText.text = "Equip";
        runeLibrary.IntializeRuneSelection(runeLibrary.contentContainer, 0, pedestal);
    }
}