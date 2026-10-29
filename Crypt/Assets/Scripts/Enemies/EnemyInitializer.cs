using Unity.Behavior;
using UnityEngine;

// Injects the spawned player into an enemy's behavior graph before enabling its AI.
public class EnemyIntializer : MonoBehaviour
{

    [SerializeField] private BehaviorGraphAgent behaviorAgent;
    public void InitializeEnemy(GameObject player)
    {
        if(behaviorAgent.SetVariableValue("Player", player))
        {
            behaviorAgent.enabled = true;
        }
    }
}
