using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public Player playerPrefab;
    public Level level;

    private Player localPlayer;
    private Dictionary<string, Player> remotePlayers = new Dictionary<string, Player>();

    public UIOverlay uiOverlay;
    public float respawnTime = 3.0f;

    [Header("Multiplayer")]
    public bool isMultiplayer = false;
    [HideInInspector] public string localPlayerId;

    public Client client; // Asignar en el Inspector

    private bool spawn = false;
    private bool movement = false;
    private string id;
    private Vector3 pos;
    private Vector3 rot;

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
        this.AddComponent<GameState>();
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

        // Suscribirse al evento de recepción de datos de otros jugadores
        client.OnPlayerDataReceived += HandlePlayerDataReceived;
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
            // Manejar el respawn de jugadores remotos si es necesario
            yield break;
        }
    }

    public Player GetLocalPlayer()
    {
        return localPlayer;
    }

    //private void HandlePlayerDataReceived(string playerId, Vector3 position)
    //{
    //    Debug.Log($"HandlePlayerDataReceived called for playerId: {playerId}, position: {position}");

    //    if (playerId == localPlayerId)
    //        return; // Do not update the local player

    //    if (remotePlayers.ContainsKey(playerId))
    //    {
    //        // Update existing player's position
    //        //remotePlayers[playerId].transform.position = position;
    //        id = playerId;
    //        pos = position;
    //        movement = true;
    //        Debug.Log($"Updated position of remote player {playerId}");
    //    }
    //    else
    //    {
    //        spawn = true;
    //        // Spawn new remote player
    //        id = playerId;
    //        pos = position;

    //    }
    //}

    private void HandlePlayerDataReceived(string playerId, Vector3 position, Vector3 rotation)
    {
        Debug.Log($"HandlePlayerDataReceived called for playerId: {playerId}, position: {position}, rotation: {rotation}");

        if (playerId == localPlayerId)
            return; // Do not update the local player

        if (remotePlayers.ContainsKey(playerId))
        {
            // Update existing player's position
            //remotePlayers[playerId].transform.position = position;
            id = playerId;
            pos = position;
            rot = rotation;
            movement = true;
            Debug.Log($"Updated position of remote player {playerId}");
        }
        else
        {
            spawn = true;
            // Spawn new remote player
            id = playerId;
            pos = position;

        }
    }


    private void Update()
    {
        if(spawn)
        {
            Player newPlayer = SpawnPlayer(id, pos);
            remotePlayers.Add(id, newPlayer);
            Debug.Log($"Spawned new remote player {id}");
            spawn = false;
        }
        else if(movement)
        {
            remotePlayers[id].transform.SetPositionAndRotation(pos, Quaternion.Euler(rot));
            movement = false;
        }
    }

    private void OnDestroy()
    {
        if (client != null)
        {
            client.OnPlayerDataReceived -= HandlePlayerDataReceived;
        }
    }
}
