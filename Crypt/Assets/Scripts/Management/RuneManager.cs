using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// Owns the persistent list of equipped rune IDs and levels.
public class RuneManager : MonoBehaviour
{
    public List<EquippedRune> equippedRunes = new List<EquippedRune>();

    public bool EquipRune(int runeID, int level)
    {
        // A rune ID may only occupy one pedestal/loadout entry at a time.
        if (IsRuneEquipped(runeID)) return false;
        equippedRunes.Add(new EquippedRune(runeID, Mathf.Max(1, level)));
        return true;
    }

    public bool UnequipRune(int runeID)
    {
        // Remove the matching runtime entry rather than changing the RuneSO definition.
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
