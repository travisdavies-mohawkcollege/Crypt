using Unity.Behavior;
using UnityEngine;

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
