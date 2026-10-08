using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    private bool isLoading = false;
    public UnityEvent sceneLoadEvent;

    public void LoadMainMenu()
    {
        StartCoroutine(LoadSceneAsyncCoroutine("MainMenu"));
    }

    public void LoadDungeon()
    {
        StartCoroutine(LoadSceneAsyncCoroutine("Dungeon"));
    }

    public void LoadTown()
    {
        if(isLoading) return;
        isLoading = true;
        sceneLoadEvent?.Invoke();
        SceneManager.LoadScene("Town");
        isLoading = false;
        RuneManager runeManager = FindAnyObjectByType<RuneManager>();
        runeManager.UnequipAllRunes();
    }

    private IEnumerator LoadSceneAsyncCoroutine(string targetSceneName)
    {
        if (isLoading) yield break;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name == targetSceneName) yield break;

        isLoading = true;
        sceneLoadEvent?.Invoke();

        // Load target scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Scene newScene = SceneManager.GetSceneByName(targetSceneName);
        if (newScene.IsValid())
        {
            SceneManager.SetActiveScene(newScene);
        }

        // Unload previous scene
        if (activeScene.IsValid() && activeScene.isLoaded && activeScene != newScene)
        {
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(activeScene);
            while (!asyncUnload.isDone)
            {
                yield return null;
            }
        }

        yield return null;
        Physics.SyncTransforms();
        InitializeSceneDependencies(targetSceneName);

        isLoading = false;
    }

    private void InitializeSceneDependencies(string sceneName)
    {
        if (sceneName == "Dungeon")
        {
            DungeonManager dungeon = FindAnyObjectByType<DungeonManager>();
            if (dungeon != null)
            {
                dungeon.IntializeDungeonManager();
            }

            PlayerManager playerManager = FindAnyObjectByType<PlayerManager>();
            if (playerManager != null)
            {
                playerManager.SpawnPlayerToSpawnPoint();
            }

            EnemyIntializer[] enemyIntializers = FindObjectsByType<EnemyIntializer>();
            if(enemyIntializers.Length > 0)
            {
                foreach(EnemyIntializer intializer in enemyIntializers)
                {
                    intializer.InitializeEnemy(playerManager.player);
                }
            }
        }
        else if (sceneName == "Town")
        {
            //Commented out to prevent double town intialization.
            /*
            TownManager townManager = FindAnyObjectByType<TownManager>();
            if (townManager != null)
            {
                townManager.InitializeTown();
            }
            */
        }
    }
}