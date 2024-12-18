using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public interface Networking
{
    void OnStart();

    void OnPacketReceived(byte[] inputPacket, EndPoint fromAddress, int Length);

    void OnUpdate();

    void OnConnectionReset(EndPoint fromAddress);

    void SendPacket(byte[] packet, EndPoint toAddress);

    void OnDisconnect();

    void ReportError(string message);

}