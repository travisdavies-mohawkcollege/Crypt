using UnityEngine;

// Contract used by the player's interaction raycast for prompts and actions.
public interface IInteractable
{
    string InteractText { get; }
    public void Interact(PlayerController player) {}
}
