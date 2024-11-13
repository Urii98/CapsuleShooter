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

    public delegate void PlayerDataReceivedHandler(PlayerState recievedState);
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
            PlayerState state = gameManager.GetMyState(localPlayer);
            byte[] data = gameManager.ToBytes(state);
            socket.SendTo(data, serverEndPoint);

            //After sending the info, clear the events so they are not sent again
            gameManager.events.Clear();
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

                    PlayerState playerState = gameManager.FromBytes(data, receivedDataLength);
                    OnPlayerDataReceived?.Invoke(playerState);
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
