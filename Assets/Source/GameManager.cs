using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using Utils;

public enum PlayerClass
{
    Soldier,
    Queen
}

struct SpawnInfo
{
    public string TeamName;
    public Transform SpawnPoint;
}

public class GameManager : NetworkBehaviour
{
    public const int BULLETS_TO_SPAWN = 30;
    public const string PROJECTILE_RESOURCE = "Projectile";
    public const string PLAYER_RESOURCE = "PlayerPrefab";
    private const string CAMERA_NAME = "Main Camera";

    private GameStartData startData;
    private GameObject levelPrefab;
    private Dictionary<string, List<Transform>> spawnPoints = new Dictionary<string, List<Transform>>();
    private Dictionary<string, List<ulong>> teamRosters = new Dictionary<string, List<ulong>>();

    // NetworkVariable to store the level prefab name
    // private NetworkVariable<FixedString128Bytes> levelPrefabName = new NetworkVariable<FixedString128Bytes>();
    // [SerializeField] private string defaultLevelPrefabName = "TestAreanPrefab";
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
            LoadLevel();
            SpawnInfo spawnInfo = SelectTeamAndSpawnPos();
            SpawnPlayer(NetworkManager.LocalClientId, spawnInfo);
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

    private void SpawnPlayer(ulong clientId, SpawnInfo spawnInfo)
    {
        GameObject instantiatedPlayer = Instantiate(Resources.Load<GameObject>(PLAYER_RESOURCE));
        NetworkObject netObj = instantiatedPlayer.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(clientId, true);
        teamRosters[spawnInfo.TeamName].Add(clientId);
        instantiatedPlayer.transform.position = spawnInfo.SpawnPoint.position;
        instantiatedPlayer.transform.rotation = spawnInfo.SpawnPoint.rotation;
    }

    public void SetUpNewPlayer(Transform newPlayer)
    {
        GameObject cameraObj = GameObject.Find(CAMERA_NAME);
        if (cameraObj != null)
        {
            cameraObj.transform.SetParent(newPlayer.transform);
            cameraObj.transform.localPosition = new Vector3(0f, 0.5f, -5f);
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
        NetworkManager.Singleton.StartHost();
    }

    private void LoadLevel()
    {
        levelPrefab = Instantiate(Resources.Load<GameObject>(startData.LevelName));
        GameObject spawnPointContainer = UnityUtils.FindGameObject(levelPrefab, "SpawnPoints");
        List<GameObject> teamSpawnPoints = UnityUtils.GetTopLevelChildren(spawnPointContainer);
        for (int i = 0, count = teamSpawnPoints.Count; i < count; ++i)
        {
            string teamName = teamSpawnPoints[i].name;
            List<Transform> spawnPointsTransforms = UnityUtils.GetTopLevelChildTransforms(teamSpawnPoints[i]);
            spawnPoints.Add(teamName, spawnPointsTransforms);
            teamRosters.Add(teamName, new List<ulong>());
        }
        Service.EventManager.SendEvent(EventId.LevelLoadCompleted, null);
    }

    private SpawnInfo SelectTeamAndSpawnPos()
    {
        string selectedTeam = string.Empty;
        int smallestTeamCount = int.MaxValue;
        foreach (KeyValuePair<string, List<ulong>> roster in teamRosters)
        {
            int teamCount = roster.Value.Count;
            if (roster.Value.Count < smallestTeamCount)
            {
                selectedTeam = roster.Key;
                smallestTeamCount = teamCount;
            }
        }

        List<Transform> availableSpawns = spawnPoints[selectedTeam];
        Transform spawnPoint = availableSpawns[UnityEngine.Random.Range(0, availableSpawns.Count)];

        SpawnInfo result = new SpawnInfo();
        result.TeamName = selectedTeam;
        result.SpawnPoint = spawnPoint;
        return result;
    }

    [ServerRpc(RequireOwnership = false)]
    private void GetGameMetadataServerRpc(ulong clientId)
    {
        Debug.Log("Requesting game data from server");
        SpawnInfo spawnInfo = SelectTeamAndSpawnPos();
        SpawnPlayer(clientId, spawnInfo);
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
    public void FireProjectileServerRpc(Vector3 position, Vector3 euler, Vector3 fwd)
    {

        NetworkObject projectile = NetworkObjectPool.Singleton.GetNetworkObject(PROJECTILE_RESOURCE, position, Quaternion.Euler(euler));
        projectile.Spawn();
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        float forceMultiplier = 600f;
        rb.AddForce(new Vector3(fwd.x * forceMultiplier, 300f, fwd.z * forceMultiplier));
    }
}
