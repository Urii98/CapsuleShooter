using System;
using System.Collections.Generic;
using System.Linq;
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

    // ACK management
    private int sequenceID = 0; 
    private Dictionary<int, byte[]> unacknowledgedPackets = new Dictionary<int, byte[]>(); 
    private float ackTimeout = 1.0f; 
    private Dictionary<int, DateTime> packetTimestamps = new Dictionary<int, DateTime>(); 
    private int lastReceivedSequenceID = 0;

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
            SendPacket(data, serverEndPoint);
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

        if (message.StartsWith("ACK:"))
        {
            int ackId = int.Parse(message.Substring("ACK:".Length));
            if (unacknowledgedPackets.ContainsKey(ackId))
            {
                unacknowledgedPackets.Remove(ackId);
                packetTimestamps.Remove(ackId);
                Debug.Log($"Acknowledged packet with sequenceId: {ackId}");
            }
        }
        else if (message.StartsWith("SyncSeqID:"))
        {
            Debug.Log("Conectado al servidor.");
            int syncSeqID = int.Parse(message.Substring("SyncSeqID:".Length));
            sequenceID = syncSeqID; 

            Debug.Log($"Synced sequenceID with server: {sequenceID}");
            SendAck(fromAddress);
        }
        else if (message.StartsWith("StartGame"))
        {
            startGame = true;
            SendAck(fromAddress);
        }
        else if (message.StartsWith("PlayerData:"))
        {
            PlayerState playerState = gameManager.replicationManager.FromBytes(inputPacket, inputPacket.Length);
            OnPlayerDataReceived?.Invoke(playerState);

            SendAck(fromAddress);
        }
        else if (message.StartsWith("SpawnHeal:"))
        {

            string json = message.Substring("SpawnHeal:".Length);
            int indexOfColon = json.LastIndexOf(':');
            if (indexOfColon != -1)
            {
                json = json.Substring(0, indexOfColon);
            }
            HealData healData = JsonUtility.FromJson<HealData>(json);
            gameManager.AddSpawnHealEvent(healData);

            SendAck(fromAddress);
        }
        else if (message.StartsWith("HealPicked:"))
        {

            string json = message.Substring("HealPicked:".Length);
            int indexOfColon = json.LastIndexOf(':');
            if (indexOfColon != -1)
            {
                json = json.Substring(0, indexOfColon);
            }
            HealData healData = JsonUtility.FromJson<HealData>(json);
            gameManager.AddRemoveHealEvent(healData.id);

            SendAck(fromAddress);
        }

    }

    private void SendAck(EndPoint toAddress)
    {
        string ackMessage = $"ACK:{sequenceID}";
        byte[] ackData = Encoding.ASCII.GetBytes(ackMessage);
        socket.SendTo(ackData, toAddress);
        Debug.Log($"Sent ACK for sequenceId: {sequenceID} to {toAddress}");
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
        sequenceID++;
        byte[] packetWithId = packet.Concat(Encoding.ASCII.GetBytes($":{sequenceID}")).ToArray(); 

        unacknowledgedPackets[sequenceID] = packetWithId; 
        packetTimestamps[sequenceID] = DateTime.Now; 

        socket.SendTo(packetWithId, toAddress);
        Debug.Log($"Sent packet with sequenceId: {sequenceID} and message: {Encoding.ASCII.GetString(packetWithId)} to {toAddress}");
    }
    private int GetSequenceIDFromPacket(byte[] packet)
    {
        int sequenceID = BitConverter.ToInt32(packet, packet.Length - sizeof(int));
        return sequenceID;
    }

    public void ReportError(string message)
    {
        Debug.LogWarning("Error: " + message);
    }

    void Update()
    {
        OnUpdate();

        //List<int> packetsToRetransmit = new List<int>();


        //foreach (var pair in packetTimestamps)
        //{
        //    if ((DateTime.Now - pair.Value).TotalSeconds > ackTimeout)
        //    {
        //        packetsToRetransmit.Add(pair.Key);
        //    }
        //}

        //foreach (int seqId in packetsToRetransmit)
        //{
        //    if (unacknowledgedPackets.TryGetValue(seqId, out byte[] packet))
        //    {
        //        socket.SendTo(packet, serverEndPoint);
        //        packetTimestamps[seqId] = DateTime.Now;
        //        Debug.Log($"Retransmitting packet with sequenceId {seqId} and message {Encoding.ASCII.GetString(packet)} ");
        //    }
        //}
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
