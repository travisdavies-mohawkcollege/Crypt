using UnityEngine;

[CreateAssetMenu(fileName = "Enemy Data", menuName = "Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string EnemyName;
    public EnemyRarity EnemyTier;
    public GameObject EnemyPrefab;

}
