using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using JetBrains.Annotations;

public class DungeonManager : MonoBehaviour
{
    private RuneManager runeManager;
    private DungeonGenerator generator;
    private RuneLibrary library;
    
    [Header("Dungeon Generator Variables")]
    public int TargetRooms;
    public float LoopChance;
    public float StraightChance;
    public float NextFloorChanceMin;
    public Vector3Int GridSize;
    public bool LightsOut;
    
    void Start()
    {
        //Defaults to ensure game doesnt crash while generating a dungeon.
        TargetRooms = 3;
        LoopChance = 0;
        StraightChance = 0.4f;
        NextFloorChanceMin = 0.1f;
        GridSize = new Vector3Int(5, 1, 5);
        LightsOut = false;
    }

    public void IntializeDungeonManager()
    {
        runeManager = FindAnyObjectByType<RuneManager>();
        generator = FindAnyObjectByType<DungeonGenerator>(); 
        library = FindAnyObjectByType<RuneLibrary>();
        foreach(int runeId in runeManager.equippedRuneIds)
        {
            library.RuneEffect(this, runeId);
        }
        generator.InitializeGenerator(TargetRooms, LoopChance, StraightChance, NextFloorChanceMin, GridSize);
        generator.GenerateDungeon();
    }

}