using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class FreeForAllManager : GameManager
{
    [Header("Heal Prefab")]
    [SerializeField] private Transform m_HealBonusPrefab;

    [Header("Win Condition")]
    [SerializeField] private int m_WinConditionKills;

    private void Update()
    {
        if (!NetworkManager.IsServer)
            return;

        List<PlayerData> playersData = new List<PlayerData>(); 
        foreach(var playerData in m_PlayersGameObjects.Keys)
        {
            playersData.Add(MultiplayerManager.Instance.FindPlayerData(playerData));
        }

        Server_CheckWinCondition(playersData.ToArray());
    }

    protected override void SceneManager_OnLoadEventCompleted(string pSceneName, LoadSceneMode pLoadMode, List<ulong> pClientsCompleted, List<ulong> pClientTimouts)
    {
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            var player = m_SpawnManager.Server_SpawnPlayer(clientId);
            m_PlayersGameObjects.Add(clientId, player);
        }
        m_SpawnManager.ResetAvailableSpawnPoints();
        base.SceneManager_OnLoadEventCompleted(pSceneName, pLoadMode, pClientsCompleted, pClientTimouts);
    }

    private void Server_CheckWinCondition(PlayerData[] players)
    {
        foreach (var player in players)
        {
            if (player.PlayerKills >= m_WinConditionKills)
            {
                DeclareWinner(player);
                break;
            }
        }
    }

    private void DeclareWinner(PlayerData player)
    {
        Debug.Log(player.PlayerName + " a gagné la partie !");
    }

    public override void Server_PlayerHit(float pDamage, Transform pGo, ulong pOwnerId)
    {
        base.Server_PlayerHit(pDamage, pGo, pOwnerId);
        PlayerData playerData = FindPlayerData(pGo);

        if (playerData.PlayerHealth > 0) return;

        if (UnityEngine.Random.value <= 1f)
        {
            if (m_HealBonusPrefab != null)
                Instantiate(m_HealBonusPrefab, pGo.position, Quaternion.identity);
        }

        int indexKiller = MultiplayerManager.Instance.FindPlayerDataIndex(pOwnerId);
        PlayerData playerDataKiller = MultiplayerManager.Instance.GetPlayerDataByIndex(indexKiller);

        Server_SetCamera(playerData.ClientId, playerDataKiller.ClientId);
        Server_SetGameObject(playerData.ClientId, false);
        m_SpawnManager.Server_RespawnPlayer(m_PlayersGameObjects[playerData.ClientId], playerData.ClientId);
    }
}
