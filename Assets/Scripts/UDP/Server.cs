using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Server : MonoBehaviour
{
    Thread waitClient;
    bool connected;

    Socket socket;
    EndPoint remote;
    int port;

    void Start()
    {
        waitClient = new Thread(WaitClient);
        connected = false;

        ServerSetup();
        waitClient.Start();

        //Set port in screen
        GameObject.Find("Port").GetComponent<Text>().text = "Port: " + port.ToString();
    }

    void ServerSetup()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        //Try different ports until one is free
        port = 9000;
        bool correctPort = false;

        while (!correctPort)
        {
            try
            {
                //Create IP info struct
                IPEndPoint ipep = new IPEndPoint(IPAddress.Any, port);

                //Bind Socket to ONLY recieve info from the said port
                socket.Bind(ipep);

                correctPort = true;
            }
            catch
            {
                port++;
            }
        }

        //Set port 0 to send the messages
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        remote = (EndPoint)(sender);
    }
    public void StopConnection()
    {
        socket.Close();
        Debug.Log("SERVER DISCONNECTED");
    }

    private void WaitClient()
    {
        byte[] recieveData = new byte[1024];
        int recv;

        //Recieve message
        try
        {
            recv = socket.ReceiveFrom(recieveData, ref remote);
        }
        catch
        {
            Debug.Log("Server stopped listening! ");
            StopConnection();
            return;
        }

        //Recieved message
        string message = Encoding.ASCII.GetString(recieveData, 0, recv);

        //Incorrect message
        if (message != "ClientConnected")
        {
            Debug.Log("Incorrect confirmation message: " + message);
            StopConnection();
            return;
        }

        //Send Confirmation Message
        byte[] sendData = Encoding.ASCII.GetBytes("ServerConnected");
        socket.SendTo(sendData, sendData.Length, SocketFlags.None, remote);

        connected = true;
    }

    public void StopSearching()
    {
        waitClient.Abort();
    }

    //GAME
    public void StartPlaying()
    {
        if (!connected) return;

        // Transfer socket and remote to Multiplayer
        Multiplayer ms = FindObjectOfType<Multiplayer>();
        ms.socket = socket;
        ms.remote = remote;
        ms.isServer = true;

        //SendStart message
        byte[] sendData = Encoding.ASCII.GetBytes("StartGame");
        socket.SendTo(sendData, sendData.Length, SocketFlags.None, remote);

        //ChangeScene
        ChangeScene();
    }

    void ChangeScene()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void GetIP(Text text)
    {
        text.text = GetIP();
    }

    public void GetPort(Text text)
    {
        text.text = port.ToString();
    }

    string GetIP()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        return "";
    }
}
