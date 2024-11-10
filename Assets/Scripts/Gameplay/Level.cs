using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level : MonoBehaviour
{
    public Transform player1SpawnPoint;
    public Transform player2SpawnPoint;
    public GameObject map;
    public GameObject spawnPlane;

    [HideInInspector]
    public Bounds spawnBounds;

    void Start()
    {
        spawnBounds = spawnPlane.GetComponent<MeshRenderer>().bounds;
    }

    public Transform GetSpawnPoint(string playerId)
    {
        if (playerId == "Player1") return player1SpawnPoint;
        if (playerId == "Player2") return player2SpawnPoint;

        return null;
    }

    public Vector3 GetRandomSpawnPoint()
    {
        float x = Random.Range(spawnBounds.min.x, spawnBounds.max.x);
        float z = Random.Range(spawnBounds.min.z, spawnBounds.max.z);
        return new Vector3(x, player1SpawnPoint.position.y, z);
    }

    
}
