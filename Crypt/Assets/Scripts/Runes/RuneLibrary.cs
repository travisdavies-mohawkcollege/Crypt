using UnityEngine;
using System.Collections.Generic;
using System;

public class RuneLibrary : MonoBehaviour
{
    //This keeps track of all runes. It knows what runes exist and how many the player owns.
    [NonSerialized] public Dictionary<int, bool> runesOwned = new Dictionary<int, bool>();
    [SerializeField] private List<RuneSO> runes;
    [SerializeField] private GameObject runePanelPrefab;
    private List<GameObject> runeChoices = new List<GameObject>();
    public Transform contentContainer;
    private RuneManager runeManager;


    private void Start()
    {
        runeManager = FindAnyObjectByType<RuneManager>();
        //For now I am just giving the player all runes.
        //This will have to check save data later.
        foreach (RuneSO rune in runes)
        {
            runesOwned.Add(rune.RuneID, true);
        }
    }

    public void IntializeRuneSelection(Transform contentContainer, int equippedRuneID, Pedestal pedestal)
    {
        if (runeChoices.Count != 0)
        {
            foreach (GameObject panel in runeChoices)
            {
                Destroy(panel);
            }
            runeChoices.Clear();
        }
        this.contentContainer = contentContainer;
        if (equippedRuneID == 0)
        {
            foreach (RuneSO rune in runes)
            {
                GameObject runePanel = Instantiate(runePanelPrefab, contentContainer, false);
                runeChoices.Add(runePanel);
                RunePanel script = runePanel.GetComponent<RunePanel>();
                script.InitializePanel(rune, pedestal);
                if (runeManager.IsRuneEquipped(script.runeId) && script.runeId != pedestal.runeOnPedastal)
                {
                    runeChoices.Remove(runePanel);
                    Destroy(runePanel);
                }
            }
        }
        else
        {
            foreach (RuneSO rune in runes)
            {
                GameObject runePanel = Instantiate(runePanelPrefab, contentContainer, false);
                runeChoices.Add(runePanel);
                RunePanel script = runePanel.GetComponent<RunePanel>();
                script.InitializePanel(rune, pedestal);
                if (script.runeId != equippedRuneID)
                {
                    script.button.SetActive(false);
                }
                if (runeManager.IsRuneEquipped(script.runeId) && script.runeId != pedestal.runeOnPedastal)
                {
                    runeChoices.Remove(runePanel);
                    Destroy(runePanel);
                }
            }
        }





    }

    public void AddRuneToCollection(int runeId)
    {
        runesOwned[runeId] = true;
    }

    public void RemoveRuneFromCollection(int runeId)
    {
        runesOwned[runeId] = false;
    }

    public bool IsRuneUnlocked(int runeId)
    {
        return runesOwned.TryGetValue(runeId, out bool unlocked) && unlocked;
    }


    public void RuneEffect(DungeonManager dm, EquippedRune equippedRune)
    {
        if (dm == null)
        {
            Debug.LogWarning("Cannot apply a rune without a DungeonManager.");
            return;
        }

        if (equippedRune == null)
        {
            Debug.LogWarning("Cannot apply a null equipped rune.");
            return;
        }

        RuneSO rune = runes.Find(rune => rune != null && rune.RuneID == equippedRune.runeID);

        if (rune == null)
        {
            Debug.LogWarning($"Could not find RuneSO with ID {equippedRune.runeID}.");
            return;
        }

        foreach (RuneModifier modifier in rune.modifiers)
        {
            if (modifier == null) continue;
            ApplyModifier(dm, modifier);
        }
    }


    private void ApplyModifier(DungeonManager dm, RuneModifier modifier)
    {
        switch (modifier.modifierType)
        {
            case ModifierType.GridSize:
                dm.GridSize = ApplyOperation(dm.GridSize, modifier.GetVectorValue(), modifier.operation);
                return;

            case ModifierType.LightsOut:
                dm.LightsOut = modifier.boolValue;
                return;

            case ModifierType.TargetRooms:
                dm.TargetRooms = ApplyOperation(dm.TargetRooms, modifier.GetIntValue(), modifier.operation);
                return;

            case ModifierType.LoopChance:
                dm.LoopChance = ApplyOperation(dm.LoopChance, modifier.GetFloatValue(), modifier.operation);
                return;

            case ModifierType.StraightChance:
                dm.StraightChance = ApplyOperation(dm.StraightChance, modifier.GetFloatValue(), modifier.operation);
                return;

            case ModifierType.NextFloorChanceMin:
                dm.NextFloorChanceMin = ApplyOperation(dm.NextFloorChanceMin, modifier.GetFloatValue(), modifier.operation);
                return;

            case ModifierType.NumberOfBranches:
                dm.NumberOfBranches = ApplyOperation(dm.NumberOfBranches, modifier.GetIntValue(), modifier.operation);
                return;

            case ModifierType.BranchLength:
                dm.BranchLength = ApplyOperation(dm.BranchLength, modifier.GetIntValue(), modifier.operation);
                return;

            case ModifierType.GenerateBranches:
                dm.GenerateBranches = modifier.boolValue;
                return;

            default:
                Debug.LogWarning($"Unhandled modifier type {modifier.modifierType}.");
                return;
        }
    }

    private int ApplyOperation(int currentValue, int modifierValue, ModifierOperation operation)
    {
        return operation == ModifierOperation.Set ? modifierValue : currentValue + modifierValue;
    }

    private float ApplyOperation(float currentValue, float modifierValue, ModifierOperation operation)
    {
        return operation == ModifierOperation.Set ? modifierValue : currentValue + modifierValue;
    }

    private Vector3Int ApplyOperation(Vector3Int currentValue, Vector3Int modifierValue, ModifierOperation operation)
    {
        return operation == ModifierOperation.Set ? modifierValue : currentValue + modifierValue;
    }



}
