using UnityEngine;
using UnityEngine.SceneManagement;

// Starts a dungeon transition when the Town portal is interacted with.
public class Portal : MonoBehaviour, IInteractable
{
    private string interactText = "Enter Portal";
    public string InteractText => interactText;

    private SceneController sceneController;
    private PlayerManager playerManager;

    
    public void Interact(PlayerController player)
    {
        playerManager = FindAnyObjectByType<PlayerManager>();
        playerManager.BackupPlayerState();

        sceneController = FindAnyObjectByType<SceneController>();
        sceneController.LoadDungeon();
        Debug.Log($"Scene controller : {sceneController}");

    }
}
