using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
//using UnityEngine.tvOS;

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

    private bool enableStartButton = false;

    private void Start()
    {
        mainMenuManager = FindAnyObjectByType<MainMenuManager>();
    }

    public void StartServer()
    {
        port = 9000; // Puedes cambiar el puerto si lo deseas
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
        // Habilitar el botón de inicio si es necesario
        if (enableStartButton)
        {
            enableStartButton = false;
            mainMenuManager.EnableStartButton();
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
                    // Extraer el playerId del cliente
                    string[] parts = message.Split(':');
                    string playerId = parts[1];

                    lock (connectedClients)
                    {
                        // Check if the client is already connected
                        bool alreadyConnected = connectedClients.Exists(c => c.PlayerId == playerId);
                        if (!alreadyConnected)
                        {
                            ConnectedClient newClient = new ConnectedClient
                            {
                                PlayerId = playerId,
                                EndPoint = clientEndPoint
                            };
                            connectedClients.Add(newClient);

                            // Enable the start button if necessary
                            if (connectedClients.Count > 1)
                            {
                                enableStartButton = true;
                            }

                            Debug.Log($"Added new client: {playerId} from {clientEndPoint}");
                        }
                    }

                    // Enviar confirmación al cliente
                    byte[] responseData = Encoding.ASCII.GetBytes("ServerConnected");
                    socket.SendTo(responseData, clientEndPoint);

                }
                else if (message.StartsWith("PlayerData:"))
                {
                    Debug.Log("Server received PlayerData from client: " + message);

                    // Retransmit the message to all clients, including the sender
                    lock (connectedClients)
                    {
                        foreach (ConnectedClient client in connectedClients)
                        {
                            socket.SendTo(data, receivedDataLength, SocketFlags.None, client.EndPoint);
                            Debug.Log("Server retransmitted PlayerData to client: " + client.PlayerId + " at " + client.EndPoint.ToString());
                        }
                    }
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
        // Enviar mensaje a todos los clientes para iniciar el juego

        byte[] startGameMessage = Encoding.ASCII.GetBytes("StartGame");

        lock (connectedClients)
        {
            foreach (ConnectedClient client in connectedClients)
            {
                socket.SendTo(startGameMessage, client.EndPoint);
            }
        }
    }

    private void OnDestroy()
    {
        StopServer();
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
            listenThread.Join(); // Esperar a que el hilo termine
            listenThread = null;
        }

    }
}
