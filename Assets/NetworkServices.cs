using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkServices : MonoBehaviour
{
    public GameObject server;
    public GameObject client;

    public void CreateServer()
    {
        Instantiate(server, this.transform);
    }

    public void CreateClient()
    {
        Instantiate(client, this.transform);
    }
   
}
