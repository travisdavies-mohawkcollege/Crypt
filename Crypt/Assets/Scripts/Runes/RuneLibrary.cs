using UnityEngine;
using System.Collections.Generic;
using System;

public class RuneLibrary : MonoBehaviour
{
    //This keeps track of all runes. It knows what runes exist and how many the player owns.
    [NonSerialized]public Dictionary<int, int> runesOwned = new Dictionary<int, int>();
    [SerializeField] private List<RuneSO> runes;

    [SerializeField] private GameObject runePanelPrefab;

    private void Start()
    {
        //For now I am just giving the player 5 of each rune.
        //This will have to check save data later.
        foreach(RuneSO rune in runes)
        {
            runesOwned.Add(rune.RuneID, 99);
        }
    }

    public void IntializeRuneSelection(Transform contentContainer)
    {
        foreach(RuneSO rune in runes)
        {
            GameObject runePanel = Instantiate(runePanelPrefab, contentContainer, false);
            RunePanel script = runePanel.GetComponent<RunePanel>();
            script.InitializePanel(rune);
        }
    }

    public void AddRuneToCollection(int runeId)
    {
        runesOwned[runeId] += 1;
    }

    public void RemoveRuneFromCollection(int runeId)
    {
        runesOwned[runeId] -= 1;
    }

    public bool IsRuneUnlocked(int runeId)
    {
        if(runesOwned[runeId] > 0) return true;
        else return false;
    }

    public int HowManyOfRuneOwned(int runeId)
    {
        if(runesOwned.TryGetValue(runeId, out int quantity)) return quantity;
        else return 0; 
    }

    //All runes and their effects. Will frequently be bools to affect the dungeon generator.
    public void RuneEffect(DungeonManager dm, int runeID)
    {
        switch(runeID)
        {
            //Horizonal Bounds Rune
            case 0:
                dm.GridSize += new Vector3Int(2, 0, 2);
                return;
            //Vertical Bounds Rune
            case 1:   
                dm.GridSize += new Vector3Int(0, 1, 0);
                return;
            //More Rooms Rune
            case 2:
                dm.TargetRooms += 5;
                return;
            //Vertical Chance Increase
            case 3:
                dm.NextFloorChanceMin += 0.1f;
                return;
            //Maze Rune
            case 4:
                dm.StraightChance += 0.1f;
                return;
            //Open Space Rune
            case 5:
                dm.LoopChance += 0.1f;
                return;
        }
    }




}
