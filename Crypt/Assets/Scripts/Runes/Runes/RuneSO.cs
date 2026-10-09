using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewRuneData", menuName = "Rune Data")]
public class RuneSO : ScriptableObject 
{
    public int RuneID;
    public int RuneLevel;
    public string RuneName;
    public string RuneDescription;
    public List<RuneModifier> modifiers = new();

}
