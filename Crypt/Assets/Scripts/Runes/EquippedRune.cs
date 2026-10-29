[System.Serializable]
// Lightweight persistent record linking a rune definition ID to its equipped level.
public class EquippedRune
{
    public int runeID;
    public int level = 1;

    public EquippedRune(int runeID, int level)
    {
        this.runeID = runeID;
        this.level = level;
    }
}
