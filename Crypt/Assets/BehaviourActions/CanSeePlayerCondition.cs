using System;
using Unity.Behavior;
using UnityEngine;

[Serializable]
[Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "Can see Player",
    story: "[Agent] can see [Target]",
    category: "Conditions",
    id: "f39e7ef40a54191c0f96fc8af0a597f5")]
public partial class CanSeePlayerCondition : Condition
{
    [SerializeReference]
    public BlackboardVariable<GameObject> Agent;

    [SerializeReference]
    public BlackboardVariable<GameObject> Target;

    [SerializeReference]
    public BlackboardVariable<float> EyeHeight = new(1.5f);

    [SerializeReference]
    public BlackboardVariable<float> TargetHeight = new(1.0f);

    [SerializeReference]
    public BlackboardVariable<float> MaximumDistance = new(20f);

    public override bool IsTrue()
    {
        GameObject agent = Agent?.Value;
        GameObject target = Target?.Value;

        if (agent == null || target == null)
            return false;

        Vector3 origin =
            agent.transform.position + Vector3.up * EyeHeight.Value;

        Vector3 destination =
            target.transform.position + Vector3.up * TargetHeight.Value;

        Vector3 direction = destination - origin;
        float distance = direction.magnitude;

        if (distance > MaximumDistance.Value)
            return false;

        // The target is effectively at the ray's origin.
        if (distance <= Mathf.Epsilon)
            return true;

        int blockingMask = LayerMask.GetMask("Wall");

        bool isBlocked = Physics.Raycast(
            origin,
            direction / distance,
            distance,
            blockingMask,
            QueryTriggerInteraction.Ignore
        );

        bool canSeePlayer = !isBlocked;

        Debug.DrawLine(
            origin,
            destination,
            canSeePlayer ? Color.green : Color.red,
            0.25f
        );

        return canSeePlayer;
    }
}