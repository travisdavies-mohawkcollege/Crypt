using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class DungeonManager : MonoBehaviour
{
    private RuneManager runeManager;
    private DungeonGenerator generator;
    
    private List<IRune> activeRunes = new List<IRune>();

    [Header("Dungeon Generator Variables")]
    public int TargetRooms;
    public float LoopChance;
    public float StraightChance;
    public float NextFloorChance;
    public Vector3Int GridSize;
    public bool LightsOut;
    
    void Start()
    {
        
    }

    public void IntializeDungeonManager()
    {
        runeManager = FindAnyObjectByType<RuneManager>();
        generator = FindAnyObjectByType<DungeonGenerator>();
        activeRunes = runeManager.FetchEquippedRunes();
        if(activeRunes.Count > 0)
        {
            foreach(IRune rune in activeRunes)
            {
                rune.RuneEffect(this);
            }
        }

        generator.InitializeGenerator(TargetRooms, LoopChance, StraightChance, NextFloorChance, GridSize);
        generator.GenerateDungeon();
    }

}