using Unity.VectorGraphics;
using UnityEngine;

// Entry point for the persistent bootstrap scene.
public class Bootstrap : MonoBehaviour
{
    private void Start()
    {
        //Find the scene controller.
        SceneController sceneController = FindAnyObjectByType<SceneController>();
        //Other important loading logic goes here. Progress bars, save loading?

        //Then load main menu.
        sceneController.LoadMainMenu();
    }
}
