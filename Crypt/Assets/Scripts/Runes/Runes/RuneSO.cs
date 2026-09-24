using UnityEngine;


[CreateAssetMenu(fileName = "NewRuneData", menuName = "Rune Data")]
public class RuneSO : ScriptableObject 
{
    public int RuneID;
    public int PowerScoreModifier;
    public string RuneName;
    public string RuneDescription;

}
