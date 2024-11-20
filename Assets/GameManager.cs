using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

public enum Events
{
    SHOOT,
    KILL,
    DISCONNECT,
    PAUSE,
    UNPAUSE,
    RESET,
    HEAL,
    NUMEVENTS
}
public struct PlayerState
{
    public string id;
    public Vector3 pos;
    public Vector3 rot;
    public float health;
    public int kills;
    public List<Events> events;
}
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
    [HideInInspector] public List<Events> events; 

    public Client client; 

    private bool spawn = false;
    private bool movement = false;
    private bool hasEvents = false;

    PlayerState otherState; 

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
    }

    private void InitializeMultiplayerGame()
    {
        
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
            //if (localPlayer != null)
            //{
            //    uiOverlay.ShowDeathMessage(respawnTime);
            //    Destroy(localPlayer.gameObject);
            //}

            yield return new WaitForSeconds(respawnTime);

            //Vector3 spawnPosition = level.GetSpawnPoint(playerId).position;
            //localPlayer = SpawnPlayer(playerId, spawnPosition);
            localPlayer.ResetPlayer();
        }
    }

    public Player GetLocalPlayer()
    {
        return localPlayer;
    }

    private void HandlePlayerDataReceived(PlayerState state)
    {
        Debug.Log($"HandlePlayerDataReceived called for playerId: {state.id}, position: {state.pos}, rotation: {state.rot}");

        if (state.id == localPlayerId)
        {
            return; // Do not update the local player
        }
        else
        {

        }

        if (remotePlayers.ContainsKey(state.id))
        {
            // Update existing player's position
            otherState.id = state.id;
            otherState.pos = state.pos;
            otherState.rot = state.rot;
            if(state.events != null)
            {
                otherState.events = state.events;
                hasEvents = true;
            }
           
            movement = true;
            Debug.Log($"Updated position of remote player {state.id}");
        }
        else
        {
            // Spawn new remote player
            spawn = true;
            otherState.id = state.id;
            otherState.pos = state.pos;

        }
    }


    private void Update()
    {
        if(spawn)
        {
            Player newPlayer = SpawnPlayer(otherState.id, otherState.pos);
            remotePlayers.Add(otherState.id, newPlayer);
            newPlayer.PlayerSetup();
            newPlayer.enabled = false;

            Debug.Log($"Spawned new remote player {otherState.id}");
            spawn = false;
        }
        else if (movement)
        {
            Player remotePlayer = remotePlayers[otherState.id];
            Vector3 currentPosition = remotePlayer.transform.position;
            Quaternion currentRotation = remotePlayer.transform.rotation;

            float distance = Vector3.Distance(currentPosition, otherState.pos);
            if (distance > 0.1f)
            {
                remotePlayer.transform.position = Vector3.Lerp(currentPosition, otherState.pos, Time.deltaTime * 50.0f);
            }

            float angleDifference = Quaternion.Angle(currentRotation, Quaternion.Euler(otherState.rot));

            if (angleDifference > 1.0f) 
            {
                remotePlayer.transform.rotation = Quaternion.Lerp(currentRotation, Quaternion.Euler(otherState.rot), Time.deltaTime * 50.0f);
            }

            if (hasEvents)
            {
                UpdateEvents();
                hasEvents = false;
            }
                

            movement = false;
        }
    }
    private void UpdateEvents()
    {
        Player remotePlayer = remotePlayers[otherState.id];
        foreach (Events e in otherState.events)
        {
            switch (e)
            {
                case Events.KILL:
                    remotePlayers[otherState.id].AddKill();
                    break;
                case Events.HEAL:
                    remotePlayers[otherState.id].Heal(20);
                    break;
                case Events.DISCONNECT:
                    //HandleDisconnect;
                    break; 
                case Events.PAUSE:
                    //HandlePause;
                    break;
                case Events.UNPAUSE:
                    //HandleUnpause;
                    break;
                case Events.RESET:
                    //HandleReset;
                    break;
                case Events.SHOOT:
                    if(otherState.id != localPlayer.playerId)
                    {
                        remotePlayer.weaponController.weapon.Shoot();
                    }
                    break;
            }
        }
    }

    private void OnDestroy()
    {
        if (client != null)
        {
            client.OnPlayerDataReceived -= HandlePlayerDataReceived;
        }
    }

    // JSON utility
    public byte[] ToBytes(PlayerState player)
    {
        string json = "PlayerData:" + JsonUtility.ToJson(player);
        Debug.Log($"Sending JSON: {json}");  
        return Encoding.ASCII.GetBytes(json);
    }

    public PlayerState FromBytes(byte[] data, int size)
    {
        string json = Encoding.ASCII.GetString(data, 0, size);
        Debug.Log($"Received JSON: {json}");  
        if (json.StartsWith("PlayerData:"))
        {
            json = json.Substring("PlayerData:".Length);
        }

        try
        {
            return JsonUtility.FromJson<PlayerState>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing JSON: {e.Message}");
            return default;
        }
    }

    // Player to PlayerState
    public PlayerState GetMyState(Player myPlayer)
    {
        PlayerState state = new PlayerState
        {
            pos = myPlayer.gameObject.transform.position,
            rot = myPlayer.gameObject.transform.eulerAngles,
            health = myPlayer.health,
            id = myPlayer.playerId,
            kills = myPlayer.Kills,
            events = events,
        };
        return state;
    }

    public void SendEvent(Events e)
    {
        events.Add(e);
    }
}

