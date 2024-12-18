using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class Client : MonoBehaviour, Networking
{
  

    private Socket socket;
    private EndPoint serverEndPoint;
    private Thread receiveThread;
    private bool isRunning = false;

    private bool startGame = false;
    private string ip;
    private int port;

    public MainMenuManager mainMenuManager;
    [HideInInspector] public string localPlayerId;
    public GameManager gameManager;

    public delegate void PlayerDataReceivedHandler(PlayerState recievedState);
    public event PlayerDataReceivedHandler OnPlayerDataReceived;

    void Start()
    {
        mainMenuManager = FindAnyObjectByType<MainMenuManager>();

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>(true);
            gameManager.client = this;
        }
    }

    public void OnStart()
    {
        try
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            string connectMessage = $"ClientConnected:{localPlayerId}";
            byte[] data = Encoding.ASCII.GetBytes(connectMessage);
            socket.SendTo(data, serverEndPoint);
            Debug.Log("Sent 'ClientConnected' to server.");

            isRunning = true;
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            
        }
        catch (Exception ex)
        {
            Debug.Log("Error connecting to server: " + ex.Message);
        }
    }

    public void OnPacketReceived(byte[] inputPacket, EndPoint fromAddress, int Length)
    {
        string message = Encoding.ASCII.GetString(inputPacket, 0, Length);

        Debug.Log($"Client {localPlayerId} received message from {fromAddress}: {message}");

        if (message.Equals("ServerConnected"))
        {
            Debug.Log("Conectado al servidor.");
        }
        else if (message == "StartGame")
        {
            startGame = true;
        }
        else if (message.StartsWith("PlayerData:"))
        {
            PlayerState playerState = gameManager.replicationManager.FromBytes(inputPacket, inputPacket.Length);
            OnPlayerDataReceived?.Invoke(playerState);
        }
        else if (message.StartsWith("SpawnHeal:"))
        {

            string json = message.Substring("SpawnHeal:".Length);
            HealData healData = JsonUtility.FromJson<HealData>(json);
            gameManager.AddSpawnHealEvent(healData);
        }
        else if (message.StartsWith("HealPicked:"))
        {

            string json = message.Substring("HealPicked:".Length);
            HealData healData = JsonUtility.FromJson<HealData>(json);
            gameManager.AddRemoveHealEvent(healData.id);
        }
    }

    public void OnUpdate()
    {
        if (startGame)
        {
            startGame = false;
            mainMenuManager.StartGame();
        }

        if (isRunning && gameManager != null && gameManager.GetLocalPlayer() != null)
        {
            SendPlayerPosition();
        }
    }

    public void OnConnectionReset(EndPoint fromAddress)
    {

    }

    public void SendPacket(byte[] packet, EndPoint toAddress)
    {
        socket.SendTo(packet, toAddress);
        Debug.Log($"Data sent from {localPlayerId}");
    }

    public void ReportError(string message)
    {
        Debug.LogWarning("Error: " + message);
    }

    void Update()
    {
        OnUpdate();
    }

    void SendPlayerPosition()
    {
        Player localPlayer = gameManager.GetLocalPlayer();
        if (localPlayer != null)
        {
            PlayerState state = gameManager.GetMyState(localPlayer);
            byte[] data = gameManager.replicationManager.ToBytes(state);
            SendPacket(data, serverEndPoint);

            gameManager.events.Clear();
        }
    }

    public void SetIpAndPort(string ip, int port)
    {
        this.ip = ip;
        this.port = port;
        OnStart();
    }

    private void ReceiveData()
    {
        while (isRunning)
        {
            try
            {
                byte[] data = new byte[1024];
                EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                int receivedDataLength = socket.ReceiveFrom(data, ref remoteEndPoint);

                OnPacketReceived(data, remoteEndPoint, receivedDataLength);
            }
            catch (SocketException ex)
            {
                Debug.LogWarning(ex.Message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);
            }
        }
    }

    public void SendToServer(string message)
    {
        if (socket != null && isRunning)
        {
            byte[] data = Encoding.ASCII.GetBytes(message);
            SendPacket(data, serverEndPoint);
        }
    }

    private void OnDestroy()
    {
        OnDisconnect();
    }

    public void OnDisconnect()
    {
        isRunning = false;

        if (socket != null)
        {
            socket.Close();
            socket = null;
        }

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join();
            receiveThread = null;
        }

        Debug.Log("Cliente desconectado, localplayerID: " + localPlayerId);
    }
}
