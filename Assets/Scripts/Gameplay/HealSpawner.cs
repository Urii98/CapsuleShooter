using System.Collections;
using UnityEngine;

public class HealSpawner : MonoBehaviour
{
    public GameObject healPrefab;
    public float spawnInterval = 10f;
    public int maxHeals = 5; 

    private int currentHeals = 0;

    public Level level;

    private void Start()
    {
        StartCoroutine(SpawnHealsRoutine());
    }

    private IEnumerator SpawnHealsRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (currentHeals < maxHeals)
            {
                SpawnHeal();
            }
        }
    }

    private void SpawnHeal()
    {
        Vector3 spawnPosition = level.GetRandomSpawnPoint();
        spawnPosition.y = 0.5f;
        Instantiate(healPrefab, spawnPosition, Quaternion.identity);
        currentHeals++;
    }

    public void HealPickedUp()
    {
        currentHeals--;
    }
}
