using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "New Action", story: "[Agent] [runs]", category: "Action/Animation", id: "5ead6a81adc48178287d5d3ff5a47632")]
public partial class RunAction : Action
{
    [SerializeReference] public BlackboardVariable<Animator> Agent;
    [SerializeReference] public BlackboardVariable<bool> Runs;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

