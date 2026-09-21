using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public GameObject playerPrefab;
    private GameObject player;

    private SceneController sceneController;

    private void Awake()
    {
        sceneController = FindAnyObjectByType<SceneController>();
    }
    private void OnEnable()
    {
        sceneController.sceneLoadEvent.AddListener(DespawnPlayer);
    }

    private void OnDisable()
    {
        sceneController.sceneLoadEvent.RemoveListener(DespawnPlayer);
    }

    public void SpawnPlayerToSpawnPoint()
    {
        SpawnPoint spawnScript = FindAnyObjectByType<SpawnPoint>();
        GameObject spawnPoint = spawnScript.gameObject;
        player = Instantiate(playerPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
    }

    public void DespawnPlayer()
    {
        //Save player info then destroy
        if(player!=null) Destroy(player);
    }
}