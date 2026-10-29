using UnityEngine;

[CreateAssetMenu(fileName = "Enemy Data", menuName = "Enemy Data")]
// Immutable enemy definition used by the director when selecting spawnable prefabs.
public class EnemyData : ScriptableObject
{
    public string EnemyName;
    public EnemyRarity EnemyTier;
    public GameObject EnemyPrefab;

}
