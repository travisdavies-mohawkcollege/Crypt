using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewRuneData", menuName = "Rune Data")]
// Immutable rune definition containing display data and generation modifiers.
public class RuneSO : ScriptableObject 
{
    public int RuneID;
    public int RuneLevel;
    public string RuneName;
    public string RuneDescription;
    public List<RuneModifier> modifiers = new();

}
