using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Can See Target", story: "[Agent] can see [Target]", category: "Action", id: "dddde1c08e0f82349b822d73ba7f3782")]
public partial class CanSeeTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    [SerializeReference]
    public BlackboardVariable<float> EyeHeight = new(1.5f);

    [SerializeReference]
    public BlackboardVariable<float> TargetHeight = new(1.0f);

    [SerializeReference]
    public BlackboardVariable<float> MaximumDistance = new(20f);


    protected override Status OnStart()
    {
        if (Agent?.Value == null || Target?.Value == null)
            return Status.Failure;

        int blockingMask = LayerMask.GetMask("Wall");

        Vector3 origin =
            Agent.Value.transform.position + Vector3.up * EyeHeight.Value;

        Vector3 destination =
            Target.Value.transform.position + Vector3.up * TargetHeight.Value;

        Vector3 direction = destination - origin;
        float distance = direction.magnitude;

        if (distance > MaximumDistance.Value)
            return Status.Failure;

        bool blocked = Physics.Raycast(
            origin,
            direction.normalized,
            distance,
            blockingMask,
            QueryTriggerInteraction.Ignore
        );

        Debug.DrawLine(
            origin,
            destination,
            blocked ? Color.red : Color.green,
            0.25f
        );

        return blocked ? Status.Failure : Status.Success;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

