using UnityEngine;

public interface IInteractable
{
    string InteractText { get; }
    public void Interact(PlayerController player) {}
}
