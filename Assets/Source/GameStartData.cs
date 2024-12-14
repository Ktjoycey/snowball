using Unity.Netcode;
using UnityEngine;

public struct GameStartData : INetworkSerializable
{
    public bool IsHost;
    public string LevelName;
    public string PlayerName;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref LevelName);
    }
}
