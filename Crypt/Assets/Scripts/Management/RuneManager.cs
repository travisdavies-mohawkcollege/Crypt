using System.Collections.Generic;
using UnityEngine;
using System.Linq;

//This is responsible for keeping tracked of what runes are equipped
public class RuneManager : MonoBehaviour
{
    public List<int> equippedRuneIds = new List<int>();

    public void EquipRune(int runeId)
    {
        equippedRuneIds.Add(runeId);
    }

    public void UnequipRune(int runeId)
    {
        if(equippedRuneIds.Contains(runeId)) equippedRuneIds.Remove(runeId);
    }

    public int HowManyRunesEquipped(int runeId)
    {
        return equippedRuneIds.Count(x => x == runeId);
    }
}
