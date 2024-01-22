using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeathmatchManager : GameManager
{
    private BoxCollider m_SpawnTeam1;
    private BoxCollider m_SpawnTeam2;

    private int m_ScoreToWin = 3;
    private int m_ScoreTeam1;
    private int m_ScoreTeam2;

    private int m_MaxRounds = 5;
    private int m_CurrentRound = 1;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Update()
    {
        base.Update();

        CheckTeamStatus();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    protected override void DetermineSpawnType()
    {
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
            player.GetComponent<PlayerHealth>().RespawnPlayerClientRpc(pPlayerData.IsTeamOne ? GetRandomPointInSpawnZone(m_SpawnTeam1) : GetRandomPointInSpawnZone(m_SpawnTeam2));

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

    private void CheckTeamStatus()
    {
        int livingPlayersTeam1 = CountLivingPlayers(true);
        int livingPlayersTeam2 = CountLivingPlayers(false);

        if (livingPlayersTeam1 > 0 && livingPlayersTeam2 == 0)
        {
            m_ScoreTeam1++;
            StartNextRound();
        }
        else if (livingPlayersTeam2 > 0 && livingPlayersTeam1 == 0)
        {
            m_ScoreTeam2++;
            StartNextRound();
        }

    }

    private int CountLivingPlayers(bool isTeamOne)
    {
        int count = 0;
        foreach (var playerData in m_PlayersGameObjects.Keys)
        {
            if (playerData.IsTeamOne == isTeamOne && IsPlayerAlive(playerData))
            {
                count++;
            }
        }
        return count;
    }

    private bool IsPlayerAlive(PlayerData playerData)
    {
        if (playerData.PlayerHealth <= 0)
            return false;
        return true;
    }

    private void StartNextRound()
    {
        //TO DO : Figer les joueurs restants, afficher message de fin de round,
        //Respawn, Update Score (UI?), Timer affiché à l'écran avant de laisser les joueurs se déplacer ect
        m_CurrentRound++;
        if (m_ScoreTeam1 == m_ScoreToWin || m_ScoreTeam2 == m_ScoreToWin)
        {
            EndGame();
            return;
        }
        else
            StartCoroutine(ShowEndRoundMessage());
    }

    private IEnumerator ShowEndRoundMessage()
    {
        //TO DO : Afficher message à la fin du round pendant quelques second avant de reset, respawn ect : Round Win ou Loose 
        //                                                                                                   1 - 0 ou 0 - 1
        yield return null;
    }

    private void EndGame()
    {
        //TO DO : Ecran de fin (score, leaderboard?) puis retour au lobby après un certain temps
    }
}
