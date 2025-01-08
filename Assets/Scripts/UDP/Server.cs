using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Concurrent;
using System.Linq; // Para ConcurrentQueue

[Serializable]
public struct HealData
{
    public int id;
    public Vector3 position;
}

public class ConnectedClient
{
    public string PlayerId { get; set; }
    public EndPoint EndPoint { get; set; }
}

public class Server : MonoBehaviour, Networking
{
    public int port { get; private set; }

    private Socket socket;
    private List<ConnectedClient> connectedClients = new List<ConnectedClient>();
    private Thread listenThread;
    private bool isRunning = false;

    public MainMenuManager mainMenuManager;
    public HealSpawner healSpawner;

    private bool enableStartButton = false;

    private Dictionary<int, HealData> activeHeals = new Dictionary<int, HealData>();
    private int nextHealId = 0;

    // ACK management
    private Dictionary<EndPoint,int> clientSequenceIDs = new Dictionary<EndPoint, int>();
    private Dictionary<int, (byte[], EndPoint)> unacknowledgedPackets = new Dictionary<int, (byte[], EndPoint)>();
    private Dictionary<int, DateTime> packetTimestamps = new Dictionary<int, DateTime>(); 
    private float ackTimeout = 1.0f; 

    private ConcurrentQueue<int> healsToRemove = new ConcurrentQueue<int>();

    private void Start()
    {
        mainMenuManager = FindAnyObjectByType<MainMenuManager>();
        healSpawner = FindObjectOfType<HealSpawner>(true);
        healSpawner.server = this;
        healSpawner.enabled = true;
    }

    public void OnStart()
    {
        port = 9000;
        try
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, port));

            isRunning = true;
            listenThread = new Thread(ListenForClients);
            listenThread.IsBackground = true;
            listenThread.Start();

            Debug.Log("Servidor iniciado en el puerto " + port);
        }
        catch (Exception ex)
        {
            Debug.Log("Error al iniciar el servidor: " + ex.Message);
        }
    }

    public void OnPacketReceived(byte[] inputPacket, EndPoint fromAddress, int Length)
    {
        string message = Encoding.ASCII.GetString(inputPacket, 0, Length);

        Debug.Log($"Server received message from {fromAddress}: {message}");

        if (message.StartsWith("ACK:"))
        {
            int ackId = int.Parse(message.Substring("ACK:".Length));
            if (unacknowledgedPackets.TryGetValue(ackId, out var packetInfo))
            {
                if (Equals(packetInfo.Item2, fromAddress)) 
                {
                    unacknowledgedPackets.Remove(ackId);
                    packetTimestamps.Remove(ackId);
                    Debug.Log($"Acknowledged packet with sequenceId: {ackId} from {fromAddress}");
                }
            }
        }
        else if (message.StartsWith("ClientConnected:"))
        {
            string[] parts = message.Split(':');
            string playerId = parts[1];

            lock (connectedClients)
            {
                bool alreadyConnected = connectedClients.Exists(c => c.PlayerId == playerId);
                if (!alreadyConnected)
                {
                    ConnectedClient newClient = new ConnectedClient
                    {
                        PlayerId = playerId,
                        EndPoint = fromAddress
                    };
                    connectedClients.Add(newClient);

                    if (connectedClients.Count > 1)
                    {
                        enableStartButton = true;
                    }

                    Debug.Log($"Added new client: {playerId} from {fromAddress}");
                }
            }
            clientSequenceIDs[fromAddress] = GetSequenceIDFromPacket(inputPacket);
            string syncMessage = "SyncSeqID";
            byte[] syncData = Encoding.ASCII.GetBytes(syncMessage);
            SendPacket(syncData, fromAddress);
            Debug.Log($"Initialized sequenceID for {fromAddress}");
            SendAck(fromAddress);
        }
        else if (message.StartsWith("PlayerData:"))
        {
            lock (connectedClients)
            {
                foreach (ConnectedClient client in connectedClients)
                {
                    SendPacket(inputPacket, Length, SocketFlags.None, client.EndPoint);
                }
            }
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
            HealData healPicked = JsonUtility.FromJson<HealData>(json);

            lock (connectedClients)
            {
                if (activeHeals.ContainsKey(healPicked.id))
                {
                    activeHeals.Remove(healPicked.id);
                    foreach (ConnectedClient client in connectedClients)
                    {
                        SendPacket(inputPacket, Length, SocketFlags.None, client.EndPoint);
                    }
                }

            }
            SendAck(fromAddress);
        }

    }
    private int GetSequenceIDFromPacket(byte[] packet)
    {
        
        int sequenceID = BitConverter.ToInt32(packet, packet.Length - sizeof(int));
        return sequenceID;
    }

    private void SendAck(EndPoint toAddress)
    {
        string ackMessage = $"ACK:{clientSequenceIDs[toAddress]}";
        byte[] ackData = Encoding.ASCII.GetBytes(ackMessage);
        socket.SendTo(ackData, toAddress);
        Debug.Log($"Sent ACK for sequenceId: {clientSequenceIDs[toAddress]} to {toAddress}");
    }

    public void OnUpdate()
    {
        if (enableStartButton)
        {
            enableStartButton = false;
            mainMenuManager.EnableStartButton();
        }
    }

    public void OnConnectionReset(EndPoint fromAddress)
    {

    }

    public void SendPacket(byte[] packet, EndPoint toAddress)
    {
        int sequenceID;
        if (!clientSequenceIDs.TryGetValue(toAddress, out sequenceID))
        {
            sequenceID = 0; 
        }
        sequenceID++;
        clientSequenceIDs[toAddress] = sequenceID;

        byte[] packetWithId = packet.Concat(Encoding.ASCII.GetBytes($":{sequenceID}")).ToArray();

        unacknowledgedPackets[sequenceID] = (packetWithId, toAddress);
        packetTimestamps[sequenceID] = DateTime.Now;

        socket.SendTo(packetWithId, toAddress);
        
        Debug.Log($"Sent packet with sequenceId: {sequenceID} and message: {Encoding.ASCII.GetString(packetWithId)} to {toAddress}");
    }

    public void SendPacket(byte[] packet, int length, SocketFlags socketFlags, EndPoint toAddress)
    {
        int sequenceID;
        if (!clientSequenceIDs.TryGetValue(toAddress, out sequenceID))
        {
            sequenceID = 0; 
        }
        sequenceID++;
        clientSequenceIDs[toAddress] = sequenceID;

        byte[] packetWithId = packet.Concat(Encoding.ASCII.GetBytes($":{sequenceID}")).ToArray(); 

        unacknowledgedPackets[sequenceID] = (packetWithId, toAddress);
        packetTimestamps[sequenceID] = DateTime.Now;

        socket.SendTo(packetWithId, length, socketFlags, toAddress);

        Debug.Log($"Sent packet with sequenceId: {sequenceID} and message: {Encoding.ASCII.GetString(packetWithId)} to {toAddress}");
    }

    public void OnDisconnect()
    {

        isRunning = false;

        if (socket != null)
        {
            socket.Close();
            socket = null;
        }

        if (listenThread != null && listenThread.IsAlive)
        {
            listenThread.Join();
            listenThread = null;
        }
    }

    public void ReportError(string message)
    {
        Debug.LogWarning("Error: " + message);
    }

    private void Update()
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
        //    if (unacknowledgedPackets.TryGetValue(seqId, out (byte[] packet, EndPoint toAddress) entry))
        //    {
        //        socket.SendTo(entry.packet, entry.toAddress);
        //        packetTimestamps[seqId] = DateTime.Now;
        //        Debug.Log($"Retransmitting packet with sequenceId: {seqId} to {entry.toAddress}");   
        //    }
        //}
    }

    private void ListenForClients()
    {
        while (isRunning)
        {
            try
            {
                byte[] data = new byte[1024];
                EndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                int receivedDataLength = socket.ReceiveFrom(data, ref clientEndPoint);

                OnPacketReceived(data, clientEndPoint, receivedDataLength);
            }
            catch (SocketException ex)
            {
                Debug.LogWarning(ex.Message);
                isRunning = false;
            }
            catch (Exception ex)
            {
                isRunning = false;
                Debug.LogWarning(ex.Message);
            }
        }
    }

    public void StartGame()
    {
        byte[] startGameMessage = Encoding.ASCII.GetBytes("StartGame");

        lock (connectedClients)
        {
            foreach (ConnectedClient client in connectedClients)
            {
                SendPacket(startGameMessage, client.EndPoint);
            }
        }
    }

    public void SpawnHealOnServer(Vector3 position)
    {
       
        HealData healData = new HealData
        {
            id = nextHealId++,
            position = position
        };
        activeHeals[healData.id] = healData;

        string msg = "SpawnHeal:" + JsonUtility.ToJson(healData);
        BroadcastToAllClients(msg);

    }

    private void BroadcastToAllClients(string message)
    {
        byte[] data = Encoding.ASCII.GetBytes(message);
        lock (connectedClients)
        {
            foreach (ConnectedClient client in connectedClients)
            {
                SendPacket(data, client.EndPoint);
            }
        }
    }

    private void OnDestroy()
    {
        OnDisconnect();
    }
}
