using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
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

    public float animSpeed;  
    public bool isJumping;
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
    private bool hasEvents = false;
    private bool spawnHealBool = false;
    private bool spawnRemoveBool = false;

    PlayerState otherState;

    private List<HealData> spawnHeals = new List<HealData>();
    private List<int> removeHeals = new List<int>();

    private Dictionary<int, GameObject> healsDict = new Dictionary<int, GameObject>();

    private Dictionary<string, List<PlayerSnapshot>> playerSnapshots = new Dictionary<string, List<PlayerSnapshot>>();

    private ConcurrentQueue<PlayerState> receivedStates = new ConcurrentQueue<PlayerState>();

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
        receivedStates.Enqueue(state);
    }

    private void ProcessReceivedState(PlayerState state)
    {
        Debug.Log($"ProcessReceivedState called for playerId: {state.id}, position: {state.pos}, rotation: {state.rot}");

        if (state.id == localPlayerId)
        {
            return; 
        }

        if (remotePlayers.ContainsKey(state.id))
        {
            Player remotePlayer = remotePlayers[state.id];
            remotePlayer.GetComponent<Animator>().SetFloat("Speed", state.animSpeed);
            remotePlayer.GetComponent<Animator>().SetBool("IsJumping", state.isJumping);

            otherState.id = state.id;
            otherState.pos = state.pos;
            otherState.rot = state.rot;
            if (state.events != null)
            {
                otherState.events = state.events;
                hasEvents = true;
            }
        }
        else
        {
            spawn = true;
            otherState.id = state.id;
            otherState.pos = state.pos;
        }

        if (!playerSnapshots.ContainsKey(state.id))
        {
            playerSnapshots[state.id] = new List<PlayerSnapshot>();
        }

        float now = Time.time; 

        Quaternion rot = Quaternion.Euler(state.rot);

        Debug.Log($"Creating snapshot for {state.id}: time={now}, pos={state.pos}, rot={rot}");

        playerSnapshots[state.id].Add(new PlayerSnapshot(now, state.pos, rot));

        if (playerSnapshots[state.id].Count > 20)
        {
            playerSnapshots[state.id].RemoveRange(0, playerSnapshots[state.id].Count - 20);
        }
    }

    private void Update()
    {
        while (receivedStates.TryDequeue(out PlayerState state))
        {
            ProcessReceivedState(state);
        }

        InterpolatingRemotePlayers();

        if (spawn)
        {
            Player newPlayer = SpawnPlayer(otherState.id, otherState.pos);
            remotePlayers.Add(otherState.id, newPlayer);
            newPlayer.PlayerSetup();
            newPlayer.enabled = false;
            spawn = false;
        }
        else if (spawnHealBool)
        {
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

            spawnHealBool = false;
        }
        else if (spawnRemoveBool)
        {
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
            spawnRemoveBool = false;
        }
        else if(hasEvents)
        {
            UpdateEvents();
            hasEvents = false;
        }
    }

    private void InterpolatingRemotePlayers()
    {
        foreach (var kvp in remotePlayers)
        {
            string remotePlayerId = kvp.Key;
            Player remotePlayer = kvp.Value;

            if (!playerSnapshots.ContainsKey(remotePlayerId))
                continue;

            var snapshots = playerSnapshots[remotePlayerId];
            if (snapshots.Count < 2)
                continue; 

            float interpolationBackTime = 0.1f;
            float renderTime = Time.time - interpolationBackTime;

            PlayerSnapshot older = snapshots[0];
            PlayerSnapshot newer = snapshots[snapshots.Count - 1];

            if (newer.time <= renderTime)
            {
                remotePlayer.transform.position = newer.position;
                remotePlayer.transform.rotation = newer.rotation;
                continue;
            }

            if (older.time > renderTime)
            {
                remotePlayer.transform.position = older.position;
                remotePlayer.transform.rotation = older.rotation;
                continue;
            }

            for (int i = 0; i < snapshots.Count - 1; i++)
            {
                if (snapshots[i].time <= renderTime && snapshots[i + 1].time >= renderTime)
                {
                    older = snapshots[i];
                    newer = snapshots[i + 1];
                    break;
                }
            }

            float t = 0f;
            float duration = (newer.time - older.time);
            if (duration > 0.0001f)
            {
                t = (renderTime - older.time) / duration;
            }

            Vector3 interpolatedPosition = Vector3.Lerp(older.position, newer.position, t);
            Quaternion interpolatedRotation = Quaternion.Slerp(older.rotation, newer.rotation, t);

            remotePlayer.transform.position = interpolatedPosition;
            remotePlayer.transform.rotation = interpolatedRotation;
        }
    }

    private void UpdateEvents()
    {
        Player remotePlayer = null;
        if (remotePlayers.ContainsKey(otherState.id))
        {
            remotePlayer = remotePlayers[otherState.id];
        }

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

            animSpeed = myPlayer.GetCurrentAnimSpeed(),
            isJumping = myPlayer.GetIsJumping()
        };
        return state;
    }

    public void SendEvent(Events e)
    {
        events.Add(e);
    }

    public void AddSpawnHealEvent(HealData hd)
    {
        spawnHealBool = true;
        spawnHeals.Add(hd);
    }

    public void AddRemoveHealEvent(int healId)
    {
        spawnRemoveBool = true;
        removeHeals.Add(healId);
    }
}