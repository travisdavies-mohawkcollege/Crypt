using System.Collections.Generic;
using UnityEngine;
using System.Linq;

//This is responsible for keeping tracked of what runes are equipped
public class RuneManager : MonoBehaviour
{
    public List<EquippedRune> equippedRunes = new List<EquippedRune>();

    public bool EquipRune(int runeID, int level)
    {
        if (IsRuneEquipped(runeID)) return false;
        equippedRunes.Add(new EquippedRune(runeID, Mathf.Max(1, level)));
        return true;
    }

    public bool UnequipRune(int runeID)
    {
        EquippedRune rune = equippedRunes.Find(rune => rune.runeID == runeID);
        if (rune == null) return false;
        equippedRunes.Remove(rune);
        return true;
    }

    public bool IsRuneEquipped(int runeID)
    {
        return equippedRunes.Any(rune => rune.runeID == runeID);
    }

    public int HowManyRunesEquipped(int runeID)
    {
        return equippedRunes.Count(rune => rune.runeID == runeID);
    }
}
