using System.Collections;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Windows;

public class Client : MonoBehaviour
{
    Thread messagingThread;
    Thread wait;

    string serverIP = "127.0.0.1";
    int serverPort;

    Socket socket;
    EndPoint remote;
    IPEndPoint endPoint;

    bool startGame;
    bool startConnection; 

    void Start()
    {
        startGame = false;
        startConnection = false;
        messagingThread = new Thread(ConnectServer);
        wait = new Thread(WaitForConnection);

        StartCoroutine(FillField());
    }
    void Update()
    {
        if (startConnection)
        {
            Connected();
            startConnection = false;
        }

        if (startGame)
        {
            ChangeScene();
            startGame = false;
        }
    }

    IEnumerator FillField()
    {
        // Get IP address
        string myIp = GetIP();
        int i = myIp.LastIndexOf('.');
        myIp = myIp.Substring(0, i + 1);

        // Fill the field
        InputField inputIP = GameObject.Find("ServerIP").GetComponent<InputField>();
        inputIP.text = myIp;
        inputIP.Select();

        InputField nameInput = GameObject.Find("User Name").GetComponent<InputField>();
        nameInput.text = "9000";

        yield return new WaitForEndOfFrame();

        //Needs to wait to set cursor
        inputIP.caretPosition = inputIP.text.Length;
        inputIP.ForceLabelUpdate();
    }
    public void SelectPort()
    {
        StartCoroutine(SelectPortCoroutine());
    }

    IEnumerator SelectPortCoroutine()
    {
        InputField inputPort = GameObject.Find("User Name").GetComponent<InputField>();
        inputPort.Select();

        yield return new WaitForEndOfFrame();

        inputPort.caretPosition = inputPort.text.Length;
        inputPort.ForceLabelUpdate();
    }

    public void SetIP()
    {
        InputField inputIP = GameObject.Find("ServerIP").GetComponent<InputField>();
        InputField portInput = GameObject.Find("User Name").GetComponent<InputField>();
        serverIP = inputIP.text.ToString();
        serverPort = int.Parse(portInput.text.ToString());

        StartConnection();
    }
    public void StartConnection()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        endPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);

        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        remote = (EndPoint)sender;

        messagingThread.Start();
    }

    void ChangeScene()
    {
        SceneManager.LoadScene("Lobby");
    }

    void Connected()
    {
        Multiplayer ms = FindObjectOfType<Multiplayer>();
        ms.socket = socket;
        ms.remote = remote;
        ms.isServer = false;
        wait.Start();
    }

    void WaitForConnection()
    {
        Debug.Log("Waiting for connection...");

        byte[] data = new byte[1024];
        int recv; 
        
        try
        {
            recv = socket.ReceiveFrom(data, ref remote);
        }
        catch
        {
            Debug.Log("Error receiving data");
            StopConnection();
            return; 
        }

        string message = Encoding.ASCII.GetString(data, 0, recv);

        // Start game
        if(message == "StartGame")
        {
            startGame = true;
        }
        else
        {
            Debug.Log("Cannot start game: " + message);
            StopConnection();
        }
    }

    public void StopConnection()
    {
        socket.Close();
        Debug.Log("Connection closed");
    }

    void ConnectServer()
    {
        // Send confirmation to server
        byte[] data = Encoding.ASCII.GetBytes("ClientConnected");
        socket.SendTo(data, data.Length, SocketFlags.None, endPoint);

        byte[] receivedData = new byte[1024];
        int recv;

        try
        {
            recv = socket.ReceiveFrom(receivedData, ref remote);
        }
        catch
        {
            Debug.Log("Error receiving data");
            StopConnection();
            return; 
        }

        string message = Encoding.ASCII.GetString(receivedData, 0, recv);

        if(message == "Connected")
        {
            startConnection = true; 
        }
        else
        {
            Debug.Log("Wrong confimation message: " + message);
            StopConnection();
        }

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
