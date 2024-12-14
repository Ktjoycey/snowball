using Unity.Collections;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

public enum PlayerClass
{
    Soldier,
    Queen
}

public class GameManager : NetworkBehaviour
{
    public const int BULLETS_TO_SPAWN = 30;
    public const string PROJECTILE_RESOURCE = "Projectile";
    public const string PLAYER_RESOURCE = "PlayerPrefab";
    private const string CAMERA_NAME = "Main Camera";

    private GameStartData startData;
    private GameObject levelPrefab;

    // NetworkVariable to store the level prefab name
    private NetworkVariable<FixedString128Bytes> levelPrefabName = new NetworkVariable<FixedString128Bytes>();
    [SerializeField] private string defaultLevelPrefabName = "TestAreanPrefab";
    void Start()
    {
        Debug.Log("Hello World!");
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("Engine: OnNetworkSpawn");
        if (IsServer)
        {
            // Debug.Log("Server spawning projectiles!");
            // SpawnProjectilesServer();
        }
        
        if (IsHost)
        {
            Debug.Log("Host");
            SpawnPlayer(NetworkManager.LocalClientId);
        }
        else if (IsClient)
        {
            Debug.Log("Client");
            GetGameMetadataServerRpc(NetworkManager.LocalClientId);
        }
    }

    // private void SpawnProjectilesServer()
    // {
    //     for (int i = 0; i < BULLETS_TO_SPAWN; ++i)
    //     {
    //         GameObject instantiatedProjectile = Instantiate(Resources.Load<GameObject>(PROJECTILE_RESOURCE));
    //         NetworkObject netObj = instantiatedProjectile.GetComponent<NetworkObject>();
    //         netObj.Spawn();
    //         // foreach (var clientId in NetworkManager.ConnectedClientsIds)
    //         // {
    //         //     netObj.NetworkHide(clientId);
    //         // }
    //         netObj.Despawn(false);
    //     }
    // }

    public void SpawnPlayer(ulong clientId)
    {
        GameObject instantiatedPlayer = Instantiate(Resources.Load<GameObject>(PLAYER_RESOURCE));
        NetworkObject netObj = instantiatedPlayer.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(clientId, true);
    }

    public void SetUpNewPlayer(Transform newPlayer)
    {
        GameObject cameraObj = GameObject.Find(CAMERA_NAME);
        if (cameraObj != null)
        {
            cameraObj.transform.SetParent(newPlayer.transform);
        }
    }

    public void StartClient(GameStartData startData)
    {
        this.startData = startData;
        NetworkManager.Singleton.StartClient();
    }

    public void StartHost(GameStartData startData)
    {
        this.startData = startData;
        LoadLevel();
        NetworkManager.Singleton.StartHost();
    }

    private void LoadLevel()
    {
        levelPrefab = Instantiate(Resources.Load<GameObject>(startData.LevelName));
    }

    [ServerRpc(RequireOwnership = false)]
    private void GetGameMetadataServerRpc(ulong clientId)
    {
        Debug.Log("Requesting game data from server");
        SpawnPlayer(clientId);
        ReceiveGameMetadataClientRpc(startData);
    }

    [ClientRpc]
    private void ReceiveGameMetadataClientRpc(GameStartData serverStartData)
    {
        Debug.Log("Received start data, level: " + serverStartData.LevelName);
        startData.LevelName = serverStartData.LevelName;
        LoadLevel();
    }

    [ServerRpc(RequireOwnership = false)]
    public void FireProjectileServerRpc(Vector3 position, Quaternion rotation)
    {
        
        NetworkObject projectile = NetworkObjectPool.Singleton.GetNetworkObject(PROJECTILE_RESOURCE, position, rotation);
        projectile.Spawn();
    }
}
