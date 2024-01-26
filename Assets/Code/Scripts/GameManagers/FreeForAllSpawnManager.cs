using GameManagers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeForAllSpawnManager : SpawnManager
{
    [Header("Spawn Points")]
    [SerializeField] private List<Transform> m_SpawnPoints;
    private List<Transform> m_AvailableSpawnPoints;

    [Header("Respawn Time")]
    [SerializeField] private int m_RespawnTime;

    private const int k_MaxAttempts = 10;

    private void Awake()
    {
        m_AvailableSpawnPoints = new List<Transform>(m_SpawnPoints);
    }

    public override void Server_RespawnPlayer(Transform pPlayerGameObject, ulong pClientId)
    {
        if (!NetworkManager.IsServer)
            return;

        StartCoroutine(RespawnCoroutine(pPlayerGameObject, pClientId));
    }

    protected override Tuple<Vector3, Quaternion> GetSpawnPoint(PlayerData pPlayerData)
    {
        if (m_AvailableSpawnPoints != null && m_AvailableSpawnPoints.Count > 0)
        {
            for (var i = 0; i < k_MaxAttempts; i++)
            {
                var randomSpawnIndex = UnityEngine.Random.Range(0, m_AvailableSpawnPoints.Count);
                var randomSpawnPoint = m_AvailableSpawnPoints[randomSpawnIndex];

                var randomPoint = randomSpawnPoint.position;

                var isLocationValid = IsSpawnLocationValid(randomPoint);
                if (!isLocationValid) continue;

                m_AvailableSpawnPoints.RemoveAt(randomSpawnIndex);

                var rotation = randomSpawnPoint.transform.rotation;
                return new Tuple<Vector3, Quaternion>(randomPoint, rotation);
            }

            Debug.LogError("Failed to find a valid spawn location after multiple attempts.");
        }
        else
        {
            Debug.LogError("No spawn points available.");
        }

        return null;
    }

    private IEnumerator RespawnCoroutine(Transform pPlayerGameObject, ulong pClientId)
    {
        var pPlayerData = MultiplayerManager.Instance.FindPlayerData(pClientId);

        // Reset Health TODO : Better
        pPlayerData.PlayerHealth = pPlayerData.PlayerMaxHealth;
        MultiplayerManager.Instance.GetPlayerDatas()[MultiplayerManager.Instance.FindPlayerDataIndex(pClientId)] =
            pPlayerData;

        // Reset position Player TODO : Not from PlayerHealth
        var (position, rotation) = GetSpawnPoint(pPlayerData);

        yield return new WaitForSeconds(m_RespawnTime);

        pPlayerGameObject.GetComponent<PlayerHealth>().RespawnPlayerClientRpc(position, rotation);

        OnPlayerRespawn?.Invoke(pClientId);

        ResetAvailableSpawnPoints();
    }

    public override void ResetAvailableSpawnPoints()
    {
        m_AvailableSpawnPoints.Clear();
        m_AvailableSpawnPoints.AddRange(m_SpawnPoints);
    }

}

