using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public Player playerPrefab;
    public Level level;

    private Player localPlayer;

    public UIOverlay uiOverlay;
    public float respawnTime = 3.0f;

    [Header("Multiplayer")]
    public bool isMultiplayer = false;
    public string localPlayerId = "Player1"; // "Player1" o "Player2"

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
            InitializeMultiplayerGame();
        }
        else
        {
            InitializeLocalGame();
        }
    }

    private void InitializeLocalGame()
    {
        Player player1 = SpawnPlayer("Player1", level.GetSpawnPoint("Player1").position);
        Player player2 = SpawnPlayer("Player2", level.GetSpawnPoint("Player2").position);

        // Iniciar la UI para el jugador local
        if (localPlayerId == "Player1")
        {
            uiOverlay.StartUI(player1);
        }
        else
        {
            uiOverlay.StartUI(player2);
        }
    }

    private void InitializeMultiplayerGame()
    {
        // Solo se genera el jugador local en modo multijugador
        Vector3 spawnPosition = level.GetSpawnPoint(localPlayerId).position;
        localPlayer = SpawnPlayer(localPlayerId, spawnPosition);

        // Iniciar la UI para el jugador local
        uiOverlay.StartUI(localPlayer);
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
        if (playerId == localPlayerId)
        {
            if (localPlayer != null)
            {
                uiOverlay.ShowDeathMessage(respawnTime);
                Destroy(localPlayer.gameObject);
            }

            yield return new WaitForSeconds(respawnTime);

            Vector3 spawnPosition = level.GetSpawnPoint(playerId).position;
            localPlayer = SpawnPlayer(playerId, spawnPosition);
            localPlayer.ResetPlayer();
        }
        else
        {
            // En este punto no hacemos nada con otros jugadores
            // Más adelante puedes agregar lógica para manejar otros jugadores
            yield break;
        }
    }
}
