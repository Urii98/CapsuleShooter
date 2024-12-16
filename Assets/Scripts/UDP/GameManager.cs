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
    SPAWNHEAL,
    REMOVEHEAL,
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

    [HideInInspector]
    public ReplicationManager replicationManager;

    private bool spawn = false;
    private bool movement = false;
    private bool hasEvents = false;

    PlayerState otherState;

    private List<HealData> spawnHeals = new List<HealData>();
    private List<int> removeHeals = new List<int>();

    private Dictionary<int, GameObject> healsDict = new Dictionary<int, GameObject>();

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
        replicationManager = gameObject.AddComponent<ReplicationManager>();

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
            yield return new WaitForSeconds(respawnTime);
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
            return; // No actualiza el local player
        }

        if (remotePlayers.ContainsKey(state.id))
        {
            otherState.id = state.id;
            otherState.pos = state.pos;
            otherState.rot = state.rot;
            if (state.events != null)
            {
                otherState.events = state.events;
                hasEvents = true;
            }

            movement = true;
        }
        else
        {
            spawn = true;
            otherState.id = state.id;
            otherState.pos = state.pos;
        }
    }

    private void Update()
    {
        if (spawn)
        {
            Player newPlayer = SpawnPlayer(otherState.id, otherState.pos);
            remotePlayers.Add(otherState.id, newPlayer);
            newPlayer.PlayerSetup();
            newPlayer.enabled = false;
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
        Player remotePlayer = null;
        if (remotePlayers.ContainsKey(otherState.id))
        {
            remotePlayer = remotePlayers[otherState.id];
        }

        // Procesamos los eventos
        foreach (Events e in otherState.events)
        {
            switch (e)
            {
                case Events.KILL:
                    if (remotePlayer != null)
                        remotePlayer.AddKill();
                    break;
                case Events.HEAL:
                    if (remotePlayer != null)
                        remotePlayer.Heal(20);
                    break;
                case Events.DISCONNECT:
                    break;
                case Events.PAUSE:
                    break;
                case Events.UNPAUSE:
                    break;
                case Events.RESET:
                    break;
                case Events.SHOOT:
                    if (remotePlayer != null && otherState.id != localPlayer.playerId)
                    {
                        remotePlayer.weaponController.weapon.Shoot();
                    }
                    break;
                case Events.SPAWNHEAL:
                    if (spawnHeals.Count > 0)
                    {
                        HealData hd = spawnHeals[0];
                        spawnHeals.RemoveAt(0);
                        GameObject healObj = Instantiate(level.healPrefab, hd.position, Quaternion.identity);
                        HealItem hi = healObj.GetComponent<HealItem>();
                        if (hi != null)
                        {
                            hi.healId = hd.id;
                            hi.gameManager = this;
                        }
                        healsDict[hd.id] = healObj;
                    }
                    break;
                case Events.REMOVEHEAL:
                    if (removeHeals.Count > 0)
                    {
                        int healId = removeHeals[0];
                        removeHeals.RemoveAt(0);
                        if (healsDict.TryGetValue(healId, out GameObject healObj))
                        {
                            Destroy(healObj);
                            healsDict.Remove(healId);
                        }
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

    public void AddSpawnHealEvent(HealData hd)
    {
        spawnHeals.Add(hd);
        events.Add(Events.SPAWNHEAL);
    }

    public void AddRemoveHealEvent(int healId)
    {
        removeHeals.Add(healId);
        events.Add(Events.REMOVEHEAL);
    }

    public void AddSpawnHealEventServer(HealData hd)
    {
        GameObject healObj = Instantiate(level.healPrefab, hd.position, Quaternion.identity);
        HealItem hi = healObj.GetComponent<HealItem>();
        if (hi != null)
        {
            hi.healId = hd.id;
            hi.gameManager = this;
        }
        healsDict[hd.id] = healObj;
    }

    public void AddRemoveHealEventServer(int healId)
    {
        if (healsDict.TryGetValue(healId, out GameObject healObj))
        {
            Destroy(healObj);
            healsDict.Remove(healId);
        }
    }

}

