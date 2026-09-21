using UnityEngine;
using UnityEngine.UI;

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