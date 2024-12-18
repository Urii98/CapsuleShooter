using System.Collections;
using UnityEngine;

public class HealSpawner : MonoBehaviour
{
    public GameObject healPrefab;
    public float spawnInterval = 10f;
    public Level level;

    
    public Server server;

    private void Start()
    {
        if (server == null)
        {
            Debug.LogError("No hay Server asignado al HealSpawner.");
            enabled = false;
            return;
        }

        StartCoroutine(SpawnHealsRoutine());
    }

    private IEnumerator SpawnHealsRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnHeal();
        }
    }

    private void SpawnHeal()
    {
        Vector3 spawnPosition = level.GetRandomSpawnPoint();
        spawnPosition.y = 0.1f;

        server.SpawnHealOnServer(spawnPosition);
    }
}
