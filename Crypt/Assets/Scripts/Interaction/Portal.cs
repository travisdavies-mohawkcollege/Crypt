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
        if (sceneController != null)
        {
            sceneController.LoadDungeon();
        }
        else
        {
            Debug.LogError("SceneController not found in scene!");
        }
    }
}
