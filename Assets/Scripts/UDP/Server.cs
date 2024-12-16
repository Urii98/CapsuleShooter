using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Concurrent; // Para ConcurrentQueue

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

public class Server : MonoBehaviour
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

    
    private ConcurrentQueue<int> healsToRemove = new ConcurrentQueue<int>();

    private void Start()
    {
        mainMenuManager = FindAnyObjectByType<MainMenuManager>();
        healSpawner = FindObjectOfType<HealSpawner>(true);
        healSpawner.server = this;
        healSpawner.enabled = true;
    }

    public void StartServer()
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

    private void Update()
    {
        if (enableStartButton)
        {
            enableStartButton = false;
            mainMenuManager.EnableStartButton();
        }

        while (healsToRemove.TryDequeue(out int healId))
        {
            RemoveHealOnServer(healId);
        }
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

                string message = Encoding.ASCII.GetString(data, 0, receivedDataLength);

                Debug.Log($"Server received message from {clientEndPoint}: {message}");

                if (message.StartsWith("ClientConnected:"))
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
                                EndPoint = clientEndPoint
                            };
                            connectedClients.Add(newClient);

                            if (connectedClients.Count > 1)
                            {
                                enableStartButton = true;
                            }

                            Debug.Log($"Added new client: {playerId} from {clientEndPoint}");
                        }
                    }

                    byte[] responseData = Encoding.ASCII.GetBytes("ServerConnected");
                    socket.SendTo(responseData, clientEndPoint);

                }
                else if (message.StartsWith("PlayerData:"))
                {
                    lock (connectedClients)
                    {
                        foreach (ConnectedClient client in connectedClients)
                        {
                            socket.SendTo(data, receivedDataLength, SocketFlags.None, client.EndPoint);
                        }
                    }
                }
                else if (message.StartsWith("HealPicked:"))
                {
                    
                    string json = message.Substring("HealPicked:".Length);
                    HealData healPicked = JsonUtility.FromJson<HealData>(json);

                    healsToRemove.Enqueue(healPicked.id);
                }
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
                socket.SendTo(startGameMessage, client.EndPoint);
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

        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddSpawnHealEventServer(healData);
        }
    }

    public void RemoveHealOnServer(int healId)
    {
        
        if (activeHeals.ContainsKey(healId))
        {
            activeHeals.Remove(healId);
            string msg = "RemoveHeal:{\"id\":" + healId + "}";
            BroadcastToAllClients(msg);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddRemoveHealEventServer(healId);
            }
        }
    }

    private void BroadcastToAllClients(string message)
    {
        byte[] data = Encoding.ASCII.GetBytes(message);
        lock (connectedClients)
        {
            foreach (ConnectedClient client in connectedClients)
            {
                socket.SendTo(data, client.EndPoint);
            }
        }
    }

    public void StopServer()
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

    private void OnDestroy()
    {
        StopServer();
    }
}
