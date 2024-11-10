using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public Player playerPrefab;
    public Level level;

    private Player player1;
    private Player player2;

    public UIOverlay uiOverlay;
    public float respawnTime = 3.0f;

    [Header("Multiplayer")]
    public bool isMultiplayer = false;
    public string localPlayerId = "Player1";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (isMultiplayer)
        {
            Debug.Log("Esperando conexión con el servidor...");
        }
        else
        {
            InitializeLocalGame();
        }
    }

    private void InitializeLocalGame()
    {
        player1 = SpawnPlayer("Player1", level.GetSpawnPoint("Player1").position);
        player2 = SpawnPlayer("Player2", level.GetSpawnPoint("Player2").position);

        if (localPlayerId == "Player1")
        {
            uiOverlay.StartUI(player1);
        }
        else
        {
            uiOverlay.StartUI(player2);
        }
    }


    public Player SpawnPlayer(string playerId, Vector3 spawnPosition)
    {
        Player player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        player.SetPlayerId(playerId);
        player.OnObjectDied += () => StartCoroutine(RespawnPlayer(playerId));
        return player;
    }

    private IEnumerator RespawnPlayer(string playerId)
    {
        Player player = (playerId == "Player1") ? player1 : player2;

        if (player != null)
        {
            if (playerId == localPlayerId)
            {
                uiOverlay.ShowDeathMessage(respawnTime);
            }

            Destroy(player.gameObject);
        }

        yield return new WaitForSeconds(respawnTime);

        if (playerId == "Player1")
        {
            player1 = SpawnPlayer("Player1", level.GetSpawnPoint("Player1").position);
            player1.ResetPlayer();
        }
        else if (playerId == "Player2")
        {
            player2 = SpawnPlayer("Player2", level.GetSpawnPoint("Player2").position);
            player2.ResetPlayer();
        }
    }


}
