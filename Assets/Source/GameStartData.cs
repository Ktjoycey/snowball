using Unity.Netcode;
using UnityEngine;

public struct GameStartData : INetworkSerializable
{
    public bool IsHost;
    public string LevelName;
    public string PlayerName;
    public string PlayerTeamName;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref LevelName);
        serializer.SerializeValue(ref PlayerTeamName);
    }
}
