using Unity.VectorGraphics;
using UnityEngine;

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