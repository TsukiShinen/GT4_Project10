using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathmatchManager : GameManager
{
    private BoxCollider m_SpawnTeam1;
    private BoxCollider m_SpawnTeam2;

    protected override void Awake()
    {
        Debug.Log("Awake");
        base.Awake();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("Spawn");
        base.OnNetworkSpawn();
    }

    protected override void DetermineSpawnType()
    {
        Debug.Log("SpawnType");
        GameObject[] spawnZones = GameObject.FindGameObjectsWithTag("ZoneSpawn");
        m_SpawnTeam1 = spawnZones[0]?.GetComponent<BoxCollider>();
        m_SpawnTeam2 = spawnZones[1]?.GetComponent<BoxCollider>();

        if (m_SpawnTeam1 != null && m_SpawnTeam2 != null)
            m_SpawnType = SpawnType.Zone;

        base.DetermineSpawnType();
    }

    protected override void SceneManager_OnLoadEventCompleted(string pSceneName, LoadSceneMode pLoadMode, List<ulong> pClientsCompleted, List<ulong> pClientTimouts)
    {
        DetermineSpawnType();
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            var playerData =
                MultiplayerManager.Instance.GetPlayerDataByIndex(
                    MultiplayerManager.Instance.FindPlayerDataIndex(clientId));

            var player = Instantiate(m_PlayerPrefab);

            if (m_SpawnTeam1 != null && m_SpawnTeam2 != null)
                    player.transform.position = playerData.IsTeamOne ? GetRandomPointInSpawnZone(m_SpawnTeam1) : GetRandomPointInSpawnZone(m_SpawnTeam2);

            player.GetComponent<NetworkObject>().SpawnWithOwnership(clientId, true);
            m_PlayersGameObjects.Add(playerData, player.gameObject);
        }

        base.SceneManager_OnLoadEventCompleted(pSceneName, pLoadMode, pClientsCompleted, pClientTimouts);
    }

    private Vector3 GetRandomPointInSpawnZone(BoxCollider spawn)
    {
        Debug.Log("RandomPoint");
        if (spawn != null)
        {
            const int maxAttempts = 10;
            for (int i = 0; i < maxAttempts; i++)
            {
                var randomPoint = new Vector3(
                    Random.Range(spawn.bounds.min.x, spawn.bounds.max.x),
                    spawn.bounds.min.y,
                    Random.Range(spawn.bounds.min.z, spawn.bounds.max.z)
                );

                bool isLocationValid = IsSpawnLocationValid(randomPoint);

                if (isLocationValid)
                {
                    return randomPoint;
                }
            }

            Debug.LogError("Failed to find a valid spawn location after multiple attempts.");
        }

        Debug.LogError("No spawn zone defined.");

        return Vector3.zero;
    }

    private bool IsSpawnLocationValid(Vector3 spawnLocation)
    {
        float playerHeight = 2f;
        float playerRadius = 0.5f;
        string playerTag = "Player";

        Collider[] colliders = Physics.OverlapBox(spawnLocation + Vector3.up * (playerHeight / 2f), new Vector3(playerRadius, playerHeight / 2f, playerRadius));

        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag(playerTag))
                return false;
        }

        Vector3[] rayDirections = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.forward + Vector3.left, Vector3.forward + Vector3.right, Vector3.back + Vector3.left, Vector3.back + Vector3.right };

        float rayLength = 1f;

        foreach (Vector3 direction in rayDirections)
        {
            if (Physics.Raycast(spawnLocation, direction, out RaycastHit hit, rayLength))
                return false;
        }

        return true;
    }


    public override void RespawnPlayer(PlayerData pPlayerData)
    {
        if (m_PlayersGameObjects.TryGetValue(pPlayerData, out GameObject player))
        {
            player.GetComponent<PlayerHealth>().RespawnPlayerClientRpc(pPlayerData.IsTeamOne ? GetRandomPointInSpawnZone(m_SpawnTeam1) : GetRandomPointInSpawnZone(m_SpawnTeam2));
        }

        base.RespawnPlayer(pPlayerData);
    }

    public override void SetPlayerData(int pIndex, PlayerData pNewPlayerData)
    {
        base.SetPlayerData(pIndex, pNewPlayerData);
    }

    public override PlayerData FindPlayerData(GameObject pPlayerGameObjects)
    {
        return base.FindPlayerData(pPlayerGameObjects);
    }

    public override GameObject FindPlayerGameObject(PlayerData pPlayerDatas)
    {
        return base.FindPlayerGameObject(pPlayerDatas);
    }
}
