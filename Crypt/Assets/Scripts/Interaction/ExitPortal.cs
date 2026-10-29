using UnityEngine;
using UnityEngine.SceneManagement;

// Sends the player back to Town when the dungeon exit is interacted with.
public class ExitPortal : MonoBehaviour, IInteractable
{
    private string interactText = "Escape Dungeon";
    public string InteractText => interactText;

    private SceneController sceneController;

    
    public void Interact(PlayerController player)
    {
        sceneController = FindAnyObjectByType<SceneController>();
        sceneController.LoadTown();
        Debug.Log($"Scene controller : {sceneController}");

    }
}
