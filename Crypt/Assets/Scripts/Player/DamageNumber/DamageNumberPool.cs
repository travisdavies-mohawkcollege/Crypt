using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class DamageNumberPool : MonoBehaviour
{
    public static DamageNumberPool Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject normalTextPrefab;
    [SerializeField] private GameObject criticalTextPrefab;

    [Header("Pool Containers")]
    [SerializeField] private Transform normalContainer;
    [SerializeField] private Transform criticalContainer;

    [Header("Initial Pool Sizes")]
    [SerializeField, Min(1)] private int normalPoolSize = 30;
    [SerializeField, Min(1)] private int criticalPoolSize = 20;

    [Header("Display Settings")]
    [SerializeField] private int criticalDamageThreshold = 70;
    [SerializeField] private float positionRandomness = 0.3f;

    private readonly List<GameObject> normalPool = new();
    private readonly List<GameObject> criticalPool = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        CreatePool(
            normalTextPrefab,
            normalPoolSize,
            normalPool,
            normalContainer
        );

        CreatePool(
            criticalTextPrefab,
            criticalPoolSize,
            criticalPool,
            criticalContainer
        );
    }

    public void Show(Vector3 worldPosition, int damage)
    {
        bool isCritical = damage >= criticalDamageThreshold;

        GameObject popup = isCritical
            ? GetAvailableObject(
                criticalTextPrefab,
                criticalPool,
                criticalContainer)
            : GetAvailableObject(
                normalTextPrefab,
                normalPool,
                normalContainer);

        Vector3 offset = new Vector3(
            Random.Range(-positionRandomness, positionRandomness),
            Random.Range(0f, positionRandomness),
            Random.Range(-positionRandomness, positionRandomness)
        );

        popup.transform.position = worldPosition + offset;

        TMP_Text textComponent =
            popup.GetComponentInChildren<TMP_Text>(true);

        if (textComponent != null)
            textComponent.text = damage.ToString();

        popup.SetActive(true);
    }

    private void CreatePool(
        GameObject prefab,
        int poolSize,
        List<GameObject> pool,
        Transform container)
    {
        for (int i = 0; i < poolSize; i++)
            CreateObject(prefab, pool, container);
    }

    private GameObject GetAvailableObject(
        GameObject prefab,
        List<GameObject> pool,
        Transform container)
    {
        foreach (GameObject pooledObject in pool)
        {
            if (!pooledObject.activeInHierarchy)
                return pooledObject;
        }

        // Expand the pool if every existing object is busy.
        return CreateObject(prefab, pool, container);
    }

    private GameObject CreateObject(
        GameObject prefab,
        List<GameObject> pool,
        Transform container)
    {
        GameObject newObject = Instantiate(
            prefab,
            Vector3.zero,
            Quaternion.identity,
            container
        );

        newObject.SetActive(false);
        pool.Add(newObject);

        return newObject;
    }
}