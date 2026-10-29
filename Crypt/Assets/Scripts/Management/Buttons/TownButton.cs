using UnityEngine;
using UnityEngine.UI;

// Connects a UI button to the Town transition while this object is enabled.
public class TownButton : MonoBehaviour
{
    private SceneController sceneController;
    [SerializeField] private Button button;

    void OnEnable()
    {
        if (sceneController == null) sceneController = FindAnyObjectByType<SceneController>();
        button.onClick.AddListener(sceneController.LoadTown);
    }

    void OnDisable()
    {
        button.onClick.RemoveAllListeners();
    }
}
