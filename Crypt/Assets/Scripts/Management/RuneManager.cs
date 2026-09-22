using System.Collections.Generic;
using UnityEngine;

//This is responsible for keeping tracked of what runes are equipped
public class RuneManager : MonoBehaviour
{
    public List<IRune> equippedRunes = new List<IRune>();
    public List<IRune> FetchEquippedRunes()
    {
        return equippedRunes;
    } 
}
