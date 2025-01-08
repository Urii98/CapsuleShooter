using System;
using UnityEngine;

[Serializable]
public struct PlayerSnapshot
{
    public float time;      
    public Vector3 position;
    public Quaternion rotation;

    public PlayerSnapshot(float time, Vector3 position, Quaternion rotation)
    {
        this.time = time;
        this.position = position;
        this.rotation = rotation;
    }
}
