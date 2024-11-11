using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

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

    public Client client; 

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
        this.AddComponent<GameState>();
    }

    private void InitializeMultiplayerGame()
    {
        // Solo se genera el jugador local en modo multijugador
        Vector3 spawnPosition = level.GetSpawnPoint(localPlayerId).position;
        localPlayer = SpawnPlayer(localPlayerId, spawnPosition);
        localPlayer.SetPlayerAsLocal();

        uiOverlay.StartUI(localPlayer);

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
    }

    public Player GetLocalPlayer()
    {
        return localPlayer;
    }

    private void HandlePlayerDataReceived(string playerId, Vector3 position, Vector3 rotation)
    {
        Debug.Log($"HandlePlayerDataReceived called for playerId: {playerId}, position: {position}, rotation: {rotation}");

        if (playerId == localPlayerId)
        {
            return; // Do not update the local player
        }
        else
        {

        }

        if (remotePlayers.ContainsKey(playerId))
        {
            // Update existing player's position
            id = playerId;
            pos = position;
            rot = rotation;
            movement = true;
            Debug.Log($"Updated position of remote player {playerId}");
        }
        else
        {
            // Spawn new remote player
            spawn = true;
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
            newPlayer.PlayerSetup();
            newPlayer.enabled = false;

            Debug.Log($"Spawned new remote player {id}");
            spawn = false;
        }
        else if (movement)
        {
            Player remotePlayer = remotePlayers[id];
            Vector3 currentPosition = remotePlayer.transform.position;
            Quaternion currentRotation = remotePlayer.transform.rotation;

            float distance = Vector3.Distance(currentPosition, pos);
            if (distance > 0.1f)
            {
                remotePlayer.transform.position = Vector3.Lerp(currentPosition, pos, Time.deltaTime * 50.0f);
            }

            float angleDifference = Quaternion.Angle(currentRotation, Quaternion.Euler(rot));

            if (angleDifference > 1.0f) 
            {
                remotePlayer.transform.rotation = Quaternion.Lerp(currentRotation, Quaternion.Euler(rot), Time.deltaTime * 50.0f);
            }

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
