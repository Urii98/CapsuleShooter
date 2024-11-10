using System;
using System.Net;
using System.Net.Sockets;
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

    void Update()
    {
        if (startGame)
        {
            startGame = false;
            mainMenuManager.StartGame();
        }
    }

    public bool ConnectToServer(string ip, int port)
    {
        try
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            // Enviar mensaje de conexión
            byte[] data = System.Text.Encoding.ASCII.GetBytes("ClientConnected");
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
                // Esperar datos del servidor
                byte[] data = new byte[1024];
                EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                int receivedDataLength = socket.ReceiveFrom(data, ref remoteEndPoint);

                string message = System.Text.Encoding.ASCII.GetString(data, 0, receivedDataLength);

                if (message == "ServerConnected")
                {
                    // Conexión establecida
                    Debug.Log("Conectado al servidor.");
                }
                else if (message == "StartGame")
                {
                    startGame = true;
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.Interrupted)
                {
                    // El socket ha sido cerrado
                    isRunning = false;
                }
                else
                {
                    Debug.Log("Error en el cliente: " + ex.Message);
                    isRunning = false;
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Error en el cliente: " + ex.Message);
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

        Debug.Log("Cliente desconectado.");
    }
}
