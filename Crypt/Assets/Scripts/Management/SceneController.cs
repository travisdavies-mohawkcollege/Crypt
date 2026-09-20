using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Events;

public class SceneController : MonoBehaviour
{
    //Control scenes and loading to keep the scene loading seemless.
    Scene currentScene;
    private bool isLoading = false;
    public UnityEvent sceneLoadEvent;

    public void Awake()
    {
        currentScene = SceneManager.GetActiveScene();
        Debug.Log($"Current scene is {currentScene.name}");
    }

    public void LoadMainMenu()
    {
        StartCoroutine(LoadSceneAsyncCoroutine("MainMenu"));
        sceneLoadEvent.Invoke();
    }

    public void LoadDungeon()
    {
        StartCoroutine(LoadSceneAsyncCoroutine("Dungeon"));
        Debug.Log("loading dungeon");
        sceneLoadEvent.Invoke();
    }

    public void LoadTown()
    {
        StartCoroutine(LoadSceneAsyncCoroutine("Town"));
        sceneLoadEvent.Invoke();
    }

    private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        if (isLoading) yield break;
        if (currentScene.name == sceneName) yield break;
        isLoading = true;
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!asyncLoad.isDone)
        {
            //ProgressBar

            yield return null;
        }
        Scene newScene = SceneManager.GetSceneByName(sceneName);
        
        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(currentScene);
        while (!asyncUnload.isDone) yield return null;
        
        currentScene = newScene;
        SceneManager.SetActiveScene(currentScene);
        if(currentScene.name == "Town")
        {
            TownManager townManager = FindAnyObjectByType<TownManager>();
            townManager.InitializeTown();
        }
        else if(currentScene.name == "Dungeon")
        {
            DungeonManager dungeon = FindAnyObjectByType<DungeonManager>();
            dungeon.IntializeDungeonManager();
        }
        isLoading = false;
    }
}
