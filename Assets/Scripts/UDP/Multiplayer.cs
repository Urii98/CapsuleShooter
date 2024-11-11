using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Multiplayer : MonoBehaviour
{
    public Socket socket;
    public EndPoint remote;

    public bool isServer = true;

    void Start()
    {
        DontDestroyOnLoad(this);
    }
}
