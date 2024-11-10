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

    public MainMenuManager mainMenuManager; // Asignar en el Inspector
    [HideInInspector] public string localPlayerId;
    public GameManager gameManager; // Asignar en el Inspector

    // Evento para notificar cuando se reciben datos de otros jugadores
    public delegate void PlayerDataReceivedHandler(string playerId, Vector3 position);
    public event PlayerDataReceivedHandler OnPlayerDataReceived;

    private float sendInterval = 0.1f;
    private float timeSinceLastSend = 0f;

    void Update()
    {
        if (startGame)
        {
            startGame = false;
            mainMenuManager.StartGame();
        }

        if (isRunning && gameManager != null && gameManager.GetLocalPlayer() != null)
        {
            timeSinceLastSend += Time.deltaTime;
            if (timeSinceLastSend >= sendInterval)
            {
                SendPlayerPosition();
                timeSinceLastSend = 0f;
            }
        }
    }

    void SendPlayerPosition()
    {
        Player localPlayer = gameManager.GetLocalPlayer();
        if (localPlayer != null)
        {
            Vector3 position = localPlayer.transform.position;
            string message = $"PlayerData:{localPlayer.playerId}:{position.x}:{position.y}:{position.z}";
            byte[] data = Encoding.ASCII.GetBytes(message);
            socket.SendTo(data, serverEndPoint);
        }
    }

    public bool ConnectToServer(string ip, int port)
    {
        try
        {
            
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 0)); // Bind to an available local port
            serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            // Enviar mensaje de conexión con el playerId
            string connectMessage = $"ClientConnected:{localPlayerId}";
            byte[] data = Encoding.ASCII.GetBytes(connectMessage);
            socket.SendTo(data, serverEndPoint);
            Debug.Log("Enviado 'ClientConnected' al servidor.");

            // Iniciar hilo de recepción
            isRunning = true;
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            return true;
        }
        catch (Exception ex)
        {
            Debug.Log("Error al conectar al servidor: " + ex.Message);
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

                    // Formato: "PlayerData:playerId:x:y:z"
                    string[] parts = message.Split(':');
                    if (parts.Length == 5)
                    {
                        string playerId = parts[1];
                        float x = float.Parse(parts[2]);
                        float y = float.Parse(parts[3]);
                        float z = float.Parse(parts[4]);
                        Vector3 position = new Vector3(x, y, z);

                        // Notificar al GameManager
                        OnPlayerDataReceived?.Invoke(playerId, position);
                    }
                }
            }
            catch (SocketException ex)
            {
                // Manejo de excepciones...
                isRunning = false;
            }
            catch (Exception ex)
            {
                // Manejo de excepciones...
                isRunning = false;
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
