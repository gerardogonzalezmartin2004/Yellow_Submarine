using UnityEngine;
using System.Collections;

public class FishSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject fishPrefab;

    [Header("Spawneo")]
    [SerializeField] private int minFishPerGroup = 3;
    [SerializeField] private int maxFishPerGroup = 7;
    [SerializeField] private float spawnRadiusAroundDiver = 10f;
    [SerializeField] private float minSpawnDistance = 5f; // no spawna encima del diver
    [SerializeField] private float spawnInterval = 8f;

    [Header("Límite")]
    [SerializeField] private int maxGroupsAlive = 3;

    private Transform diver;
    private int groupsAlive = 0;

    private void Start()
    {
        diver = GameObject.FindGameObjectWithTag("Player")?.transform;
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (groupsAlive < maxGroupsAlive && diver != null)
                SpawnGroup();
        }
    }

    private void SpawnGroup()
    {
        // Posición random alrededor del diver, fuera del radio mínimo
        Vector2 spawnCenter;
        int attempts = 0;
        do
        {
            spawnCenter = (Vector2)diver.position + Random.insideUnitCircle * spawnRadiusAroundDiver;
            attempts++;
        } while (Vector2.Distance(spawnCenter, diver.position) < minSpawnDistance && attempts < 10);

        int count = Random.Range(minFishPerGroup, maxFishPerGroup + 1);

        GameObject groupParent = new GameObject($"FishGroup_{groupsAlive}");
        groupParent.transform.position = spawnCenter;
        groupsAlive++;

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 1.5f;
            GameObject fish = Instantiate(fishPrefab, (Vector2)groupParent.transform.position + offset,
                                          Quaternion.identity, groupParent.transform);

            // Auto-destruir el grupo después de un tiempo
            FishLifetime lifetime = fish.AddComponent<FishLifetime>();
            lifetime.Init(groupParent, () => groupsAlive--);
        }
    }
}

// Helper para destruir el grupo cuando todos los peces se van
public class FishLifetime : MonoBehaviour
{
    [SerializeField] private float lifetime = 30f;
    private GameObject groupParent;
    private System.Action onDestroy;

    public void Init(GameObject parent, System.Action callback)
    {
        groupParent = parent;
        onDestroy = callback;
        Destroy(groupParent, lifetime);
    }

    private void OnDestroy()
    {
        onDestroy?.Invoke();
    }
}