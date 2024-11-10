using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class Server : MonoBehaviour
{
    public int port { get; private set; }

    private Socket socket;
    private List<EndPoint> connectedClients = new List<EndPoint>();
    private Thread listenThread;
    private bool isRunning = false;

    public MainMenuManager mainMenuManager;

    private bool enableStartButton = false;

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

                if (message.StartsWith("ClientConnected:"))
                {
                    // Extraer el playerId del cliente
                    string[] parts = message.Split(':');
                    string playerId = parts[1];

                    // Agregar el cliente a la lista si no está ya
                    lock (connectedClients)
                    {
                        if (!connectedClients.Contains(clientEndPoint))
                        {
                            connectedClients.Add(clientEndPoint);

                            // Si hay más de un cliente, señalamos que debemos habilitar el botón de inicio
                            if (connectedClients.Count > 1)
                            {
                                enableStartButton = true;
                            }
                        }
                    }

                    // Enviar confirmación al cliente
                    byte[] responseData = Encoding.ASCII.GetBytes("ServerConnected");
                    socket.SendTo(responseData, clientEndPoint);

                    // Enviar el playerId del servidor al cliente conectado
                    // Esto es útil para que el cliente sepa quién es el otro jugador
                    // En este ejemplo, asumiremos que el servidor es "Player1"
                }
                else if (message.StartsWith("PlayerData:"))
                {
                    Debug.Log("Server received PlayerData from client: " + message);
                    // Retransmit the message to all clients
                    lock (connectedClients)
                    {
                        foreach (EndPoint client in connectedClients)
                        {
                            socket.SendTo(data, receivedDataLength, SocketFlags.None, client);
                            Debug.Log("Server retransmitted PlayerData to client: " + client.ToString());
                        }
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

    public void StartGame()
    {
        // Enviar mensaje a todos los clientes para iniciar el juego
        byte[] startGameMessage = Encoding.ASCII.GetBytes("StartGame");

        lock (connectedClients)
        {
            foreach (EndPoint client in connectedClients)
            {
                socket.SendTo(startGameMessage, client);
            }
        }

        // El servidor también recibirá este mensaje a través de su cliente interno
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

        Debug.Log("Servidor detenido.");
    }
}
