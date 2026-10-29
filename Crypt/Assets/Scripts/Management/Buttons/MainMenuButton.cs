using UnityEngine;
using UnityEngine.UI;

// Connects a UI button to the persistent scene-loading service while this object is enabled.
public class MainMenuButton : MonoBehaviour
{
    private SceneController sceneController;
    [SerializeField] private Button button;

    void OnEnable()
    {
        if (sceneController == null) sceneController = FindAnyObjectByType<SceneController>();
        button.onClick.AddListener(sceneController.LoadMainMenu);
    }

    void OnDisable()
    {
        button.onClick.RemoveAllListeners();
    }
}
