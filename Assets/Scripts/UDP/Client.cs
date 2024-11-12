using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class Client : MonoBehaviour
{
    private Socket socket;
    private EndPoint serverEndPoint;
    private Thread receiveThread;
    private bool isRunning = false;

    // Variables compartidas
    private bool startGame = false;

    public MainMenuManager mainMenuManager;
    [HideInInspector] public string localPlayerId;
    public GameManager gameManager; 

    public delegate void PlayerDataReceivedHandler(string playerId, Vector3 position, Vector3 rotation);
    public event PlayerDataReceivedHandler OnPlayerDataReceived;


    void Start()
    {
        mainMenuManager = FindAnyObjectByType<MainMenuManager>();

        // Try to find the GameManager instance
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>(true);
            gameManager.client = this;
        }
    }

    void Update()
    {
        if (startGame)
        {
            startGame = false;
            mainMenuManager.StartGame();
        }

        if (isRunning && startGame)
        {
            if (gameManager == null)
            {
                Debug.LogError("Client: gameManager is null");
            }
            else if (gameManager.GetLocalPlayer() == null && gameManager.gameObject.activeInHierarchy)
            {
                Debug.LogError("Client: GetLocalPlayer() returned null");
            }
        }

        if (isRunning && gameManager != null && gameManager.GetLocalPlayer() != null)
        {
            SendPlayerPosition();
        }
    }

    void SendPlayerPosition()
    {
        Player localPlayer = gameManager.GetLocalPlayer();
        if (localPlayer != null)
        {
            Vector3 position = localPlayer.transform.position;
            Vector3 rotation = localPlayer.transform.eulerAngles; 

            string message = $"PlayerData:{localPlayer.playerId}:{position.x}:{position.y}:{position.z}:{rotation.x}:{rotation.y}:{rotation.z}";
            byte[] data = Encoding.ASCII.GetBytes(message);
            socket.SendTo(data, serverEndPoint);
        }
    }


    public bool ConnectToServer(string ip, int port)
    {
        try
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            // Send connection message
            string connectMessage = $"ClientConnected:{localPlayerId}";
            byte[] data = Encoding.ASCII.GetBytes(connectMessage);
            socket.SendTo(data, serverEndPoint);
            Debug.Log("Sent 'ClientConnected' to server.");

            // Start receive thread
            isRunning = true;
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            return true;
        }
        catch (Exception ex)
        {
            Debug.Log("Error connecting to server: " + ex.Message);
            return false;
        }
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

                string message = Encoding.ASCII.GetString(data, 0, receivedDataLength);

                Debug.Log($"Client {localPlayerId} received message from {remoteEndPoint}: {message}");

                if (message == "ServerConnected")
                {
                    // Conexión establecida
                    Debug.Log("Conectado al servidor.");
                }
                else if (message == "StartGame")
                {
                    startGame = true;
                }
                else if (message.StartsWith("PlayerData:"))
                {
                    Debug.Log("Client received PlayerData: " + message);

                    string[] parts = message.Split(':');
                    if (parts.Length == 8)
                    {
                        string playerId = parts[1];
                        float x = float.Parse(parts[2]);
                        float y = float.Parse(parts[3]);
                        float z = float.Parse(parts[4]);
                        float rotX = float.Parse(parts[5]);
                        float rotY = float.Parse(parts[6]);
                        float rotZ = float.Parse(parts[7]);

                        Vector3 position = new Vector3(x, y, z);
                        Vector3 rotation = new Vector3(rotX, rotY, rotZ);

                        // Notificar al GameManager
                        OnPlayerDataReceived?.Invoke(playerId, position, rotation);
                    }
                }
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

    private void OnDestroy()
    {
        Disconnect();
    }

    public void Disconnect()
    {
        isRunning = false;

        if (socket != null)
        {
            socket.Close();
            socket = null;
        }

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(); // Esperar a que el hilo termine
            receiveThread = null;
        }

        Debug.Log("Cliente desconectado, localplayerID: ." + localPlayerId);
    }
}
