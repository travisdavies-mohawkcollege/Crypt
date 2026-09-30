using UnityEngine;
using System.Collections.Generic;
using System;

public class RuneLibrary : MonoBehaviour
{
    //This keeps track of all runes. It knows what runes exist and how many the player owns.
    [NonSerialized]public Dictionary<int, bool> runesOwned = new Dictionary<int, bool>();
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
        foreach(RuneSO rune in runes)
        {
            runesOwned.Add(rune.RuneID, true);
        }
    }

    public void IntializeRuneSelection(Transform contentContainer, int equippedRuneID, Pedestal pedestal)
    {
        if(runeChoices.Count != 0)
        {
            foreach(GameObject panel in runeChoices)
            {
                Destroy(panel);
            }
            runeChoices.Clear();
        }
        this.contentContainer = contentContainer;
        if(equippedRuneID == 0)
        {
            foreach(RuneSO rune in runes)
            {
                GameObject runePanel = Instantiate(runePanelPrefab, contentContainer, false);
                runeChoices.Add(runePanel);
                RunePanel script = runePanel.GetComponent<RunePanel>();
                script.InitializePanel(rune, pedestal);
                if(runeManager.equippedRuneIds.Contains(script.runeId) && script.runeId != pedestal.runeOnPedastal)
                {
                    runeChoices.Remove(runePanel);
                    Destroy(runePanel);
                }
            }
        }
        else
        {
            foreach(RuneSO rune in runes)
            {
                GameObject runePanel = Instantiate(runePanelPrefab, contentContainer, false);
                runeChoices.Add(runePanel);
                RunePanel script = runePanel.GetComponent<RunePanel>();
                script.InitializePanel(rune, pedestal);
                if(script.runeId != equippedRuneID)
                {
                    script.button.SetActive(false);
                }
                if(runeManager.equippedRuneIds.Contains(script.runeId) && script.runeId != pedestal.runeOnPedastal)
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
        if(runesOwned[runeId] == true) return true;
        else return false;
    }


    //All runes and their effects. Will frequently be bools to affect the dungeon generator.
    public void RuneEffect(DungeonManager dm, int runeID)
    {
        switch(runeID)
        {
            //LEAVE THIS BLANK. NO RUNE SHOULD HAVE ID 0.
            case 0:
                Debug.Log("RuneID 0 tried to pass");
                return;
            
            //Halls of Longing
            case 1:   
                if(dm.GridSize.x > 4 || dm.GridSize.z > 4)
                {
                    dm.GridSize = new Vector3Int(2, dm.GridSize.y, 2);
                }
                dm.GridSize += new Vector3Int(UnityEngine.Random.Range(50, 150), 0, UnityEngine.Random.Range(3, 6));
                dm.TargetRooms += UnityEngine.Random.Range(150, 200);
                dm.StraightChance += 0.4f;
                return;
            //Pits of Despair
            case 2:
                dm.TargetRooms += UnityEngine.Random.Range(150, 200);
                dm.GridSize += new Vector3Int(0, UnityEngine.Random.Range(50, 150), 0);
                if(dm.GridSize.x > 4 || dm.GridSize.z > 4) dm.GridSize -= new Vector3Int(3, 0, 3);                
                dm.NextFloorChanceMin = 0.7f;
                return;
            
            case 3:
                
                return;
            
            case 4:
                
                return;
            
            case 5:
                
                return;
        }
    }




}
