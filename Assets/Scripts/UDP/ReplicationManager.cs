using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ReplicationManager : MonoBehaviour
{
    // JSON utility
    public byte[] ToBytes(PlayerState player)
    {
        string json = "PlayerData:" + JsonUtility.ToJson(player);
        Debug.Log($"Sending JSON: {json}");
        return Encoding.ASCII.GetBytes(json);
    }

    public PlayerState FromBytes(byte[] data, int size)
    {
        string json = Encoding.ASCII.GetString(data, 0, size);
        Debug.Log($"Received JSON: {json}");
        if (json.StartsWith("PlayerData:"))
        {
            json = json.Substring("PlayerData:".Length);
        }

        try
        {
            return JsonUtility.FromJson<PlayerState>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing JSON: {e.Message}");
            return default;
        }
    }
}
