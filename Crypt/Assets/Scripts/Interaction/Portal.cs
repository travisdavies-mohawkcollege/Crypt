using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour, IInteractable
{
    private string interactText = "Enter Portal";
    public string InteractText => interactText;

    private SceneController sceneController;

    
    public void Interact(PlayerController player)
    {
        sceneController = FindAnyObjectByType<SceneController>();
        sceneController.LoadDungeon();
        Debug.Log($"Scene controller : {sceneController}");

    }
}
