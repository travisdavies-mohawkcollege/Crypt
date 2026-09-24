using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class RunePanel : MonoBehaviour
{
    [SerializeField] private RuneSO rune;
    [SerializeField] private TextMeshProUGUI runeName;
    [SerializeField] private TextMeshProUGUI runeDescription;
    [SerializeField] private TextMeshProUGUI runesOwned;
    [SerializeField] private TextMeshProUGUI amountApplied;
    private int runeId;
    private int runesApplied = 0;
    private RuneManager runeManager;
    private RuneLibrary runeLibrary;

    public void InitializePanel(RuneSO assignedRune)
    {
        runeManager = FindAnyObjectByType<RuneManager>();
        runeLibrary = FindAnyObjectByType<RuneLibrary>();
        rune = assignedRune;

        runeId = rune.RuneID;
        runeName.text = rune.RuneName;
        runeDescription.text = rune.RuneDescription;
        runesOwned.text = runeLibrary.HowManyOfRuneOwned(runeId).ToString();
        runesApplied = runeManager.HowManyRunesEquipped(runeId);
        amountApplied.text = runesApplied.ToString();
    }


    public void AddRuneButton()
    {
        if(runeLibrary.HowManyOfRuneOwned(runeId) > 0 && runeLibrary.HowManyOfRuneOwned(runeId) > runeManager.HowManyRunesEquipped(runeId))
        {
            runeManager.EquipRune(runeId);
            runesApplied += 1;
            amountApplied.text = runesApplied.ToString();
        } 
        
    }

    public void RemoveRuneButton()
    {
        runeManager.UnequipRune(runeId);
        
        if(runesApplied > 0)
        {
            runesApplied -= 1;
            amountApplied.text = runesApplied.ToString();
        }
        
    }
}
