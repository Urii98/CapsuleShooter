using System.Text;
using System.Threading;
using UnityEngine;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using System;

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
    public float time; 
    public Vector3 position;
    public Quaternion rotation;
    public float health;
    public string playerID;
    public int kills; 
    //public List<Events> events;
}
public class GameState : MonoBehaviour
{
    const int MESSAGE_SIZE = 1024;
    [SerializeField] float MESSAGE_DELAY = 1.0f;

    Player myPlayer;
    Player otherPlayer;

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
        events = new List<Events>();

        StartCoroutine(InitializePlayers());
        DataTransfer();
    }

    IEnumerator InitializePlayers()
    {
        // Esperar hasta que ambos jugadores estén en la escena
        while (FindObjectsOfType<Player>().Length < 2)
        {
            yield return null; // Esperar un frame
        }

        // Una vez que ambos jugadores están presentes, llamar a GetPlayers()
        GetPlayers();
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
            Debug.Log(player.playerId);
            if (player.playerId == GameManager.Instance.localPlayerId)
            {
                myPlayer = player;
            }
            else
            {
                otherPlayer = player;
                player.BlockMovement();
            }
        }
    }
    void UpdateState()
    {
        // Rellenar con cada uno de los estados, incluyendo los de los scripts de los jugadores
        otherPlayer.gameObject.transform.position = otherState.position;
        otherPlayer.gameObject.transform.rotation = otherState.rotation;

        //foreach(Events e in otherState.events)
        //{
        //    switch (e)
        //    {
        //        case Events.SHOOT:
        //            otherPlayer.GetComponent<Player>().weaponController.weapon.Shoot();
        //            break;
        //        case Events.KILL:
        //            myPlayer.gameObject.SetActive(false);
        //            break; 
        //        case Events.DISCONNECT:
        //            //Handle disconnection
        //            break;
        //        case Events.PAUSE:
        //            //Handle pause
        //            break;
        //        case Events.UNPAUSE:
        //            //Handle unpause
        //            break;
        //        case Events.RESET:
        //            //Handle reset
        //            break;
        //        case Events.HEAL:
        //            myPlayer.GetComponent<Player>().Heal(20);
        //            break;
        //    }
        //}
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
            

            byte[] data = ToBytes(GetMyState());
            multiplayerState.socket.SendTo(data, data.Length, SocketFlags.None, multiplayerState.remote);

            events.Clear();
        }
    }

    PlayerState GetMyState()
    {
        Player playerComponent = myPlayer.GetComponent<Player>();
        PlayerState state = new PlayerState
        {
            time = Time.time,
            position = myPlayer.gameObject.transform.position,
            rotation = myPlayer.gameObject.transform.rotation,
            health = playerComponent.health,
            playerID = playerComponent.playerId,
            kills = playerComponent.Kills,
            //events = events,
        };

        return state; 
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
            string checkMessage = Encoding.ASCII.GetString(data, 0, recv);
            if(!checkMessage.StartsWith("PlayerData:"))
                Debug.Log(checkMessage);
            if(checkMessage.StartsWith("time"))
            {
                PlayerState message = FromBytes(data, recv);
                if (message.time > otherState.time)
                {
                    otherState = message;
                    updated = true;
                }
                //else if(message.events.Count > 0)
                //{
                //    otherState.events = message.events;
                //    updated = true;
                //}
            }
        }
    }

    // SERIALIZATION
    byte[] ToBytes(PlayerState player)
    {
        string json = JsonUtility.ToJson(player);
        //Debug.Log($"Sending JSON: {json}");  // Agregar log para inspeccionar el contenido del JSON
        return Encoding.ASCII.GetBytes(json);
    }

    PlayerState FromBytes(byte[] data , int size)
    {
        string json = Encoding.ASCII.GetString(data, 0, size);
        Debug.Log($"Received JSON: {json}");  // Agregar log para inspeccionar el contenido del JSON

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

    // Send events to multiplayer
    public void SendEvent(Events e)
    {
        // WIP
        events.Add(e);
        if(e == Events.RESET)
        {
            byte[] data = ToBytes(GetMyState());
            multiplayerState.socket.SendTo(data, data.Length, SocketFlags.None, multiplayerState.remote);

            events.Clear();
        }
    }

    void KillGame()
    {
        StopDataTransfer();
        SceneManager.LoadScene(0);
    }
}
