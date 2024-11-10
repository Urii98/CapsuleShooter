using System.Text;
using System.Threading;
using UnityEngine;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public enum Events
{
    MENU,
    PLAYING,
    PAUSED,
    GAMEOVER
}

public struct PlayerState
{
    public float time; 
    public Vector3 position;
    public Quaternion rotation;
    public List<Events> events;
}
public class GameState : MonoBehaviour
{
    const int MESSAGE_SIZE = 1024;
    const string PLAYER_1 = "Player";
    const string PLAYER_2 = "Player_2";
    [SerializeField] float MESSAGE_DELAY = 1.0f;

    Transform myPlayer;
    Transform otherPlayer;

    Multiplayer multiplayerState;
    bool updated; 
    PlayerState otherState;

    Thread messages;
    [HideInInspector] public List<Events> events;

    void Start()
    {
        updated = false;
        messages = new Thread(RecieveState);
        
        multiplayerState = FindObjectOfType<Multiplayer>();
        if(multiplayerState == null)
        {
            Debug.Log("Multiplayer not found");
            return;
        }
        GetPlayers();
        DataTransfer();
    }

    void Update()
    {
        if (updated)
        {
            UpdateState();
            updated = false;
        }
    }

    void GetPlayers()
    {
        Player[] players = FindObjectsOfType<Player>();
        foreach (Player player in players)
        {
            if (player.gameObject.name == PLAYER_1)
            {
                if (multiplayerState.isServer)
                {
                    myPlayer = player.gameObject.transform;
                }
                else
                {
                    otherPlayer = player.gameObject.transform;
                    player.BlockMovement();
                }
            }
            else if (player.gameObject.name == PLAYER_2)
            {
                if (!multiplayerState.isServer)
                {
                    myPlayer = player.gameObject.transform;
                }
                else
                {
                    otherPlayer = player.gameObject.transform;
                    player.BlockMovement();
                }

            }
        }
    }
    void UpdateState()
    {
        // Rellenar con cada uno de los estados, incluyendo los de los scripts de los jugadores
        otherPlayer.position = otherState.position;
        otherPlayer.rotation = otherState.rotation;

        foreach(Events e in otherState.events)
        {
            switch (e)
            {
                case Events.PLAYING:
                    break;
                case Events.PAUSED:
                    break; 

            }
        }
    }
    

    void DataTransfer()
    {
        messages.Start();
        StartCoroutine(SendState());
    }

    void StopDataTransfer()
    {
        messages.Abort();
        StopCoroutine(SendState());
    }
    
    IEnumerator SendState()
    {
        while (true)
        {
            yield return new WaitForSeconds(MESSAGE_DELAY);

            PlayerState message = new PlayerState();
            message.time = Time.time;
            message.position = myPlayer.position;
            message.rotation = myPlayer.rotation;
            message.events = events;

            byte[] data = ToBytes(message);
            multiplayerState.socket.SendTo(data, data.Length, SocketFlags.None, multiplayerState.remote);

            events.Clear();
        }
    }
        
    void RecieveState()
    {
        while (true)
        {
            byte[] data = new byte[MESSAGE_SIZE];
            int recv = 0;

            try
            {
                recv = multiplayerState.socket.ReceiveFrom(data, ref multiplayerState.remote);
            }
            catch
            {
                return;
            }

            PlayerState message = FromBytes(data, recv);

            if(message.time > otherState.time)
            {
                otherState = message;
                updated = true;
            }
            else if(message.events.Count > 0)
            {
                otherState.events = message.events;
                updated = true;
            }
        }
    }

    // SERIALIZATION
    byte[] ToBytes(PlayerState player)
    {
        string json = JsonUtility.ToJson(player);
        return Encoding.ASCII.GetBytes(json);
    }

    PlayerState FromBytes(byte[] data , int size)
    {
        string json = Encoding.ASCII.GetString(data);
        return JsonUtility.FromJson<PlayerState>(json);
    }

    // Send events to multiplayer
    public void SendEvent(Events e)
    {
        // WIP
        events.Add(e);
    }
}
