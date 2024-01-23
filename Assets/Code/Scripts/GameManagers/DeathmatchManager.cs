using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.FPS.Gameplay;
using UnityEngine.UIElements;
using Unity.Services.Lobbies.Models;
using Network;
using ScriptableObjects.GameModes;
using static UnityEngine.Rendering.GPUSort;

public enum GameState
{
    Playing,
    RoundStart,
    RoundEnd
}

public class DeathmatchManager : GameManager
{
    [SerializeField] private UIDocument m_Document;
    [SerializeField] private VisualTreeAsset m_ScoreElement;

    private BoxCollider m_SpawnTeam1;
    private BoxCollider m_SpawnTeam2;

    private int m_ScoreToWin = 3;
    private int m_ScoreTeam1;
    private int m_ScoreTeam2;

    private int m_MaxRounds = 5;
    private int m_CurrentRound = 1;

    private VisualElement m_Root;

    private GameState m_GameState = GameState.Playing;

    protected override void Awake()
    {
        base.Awake();

        m_Root = m_Document.rootVisualElement;
    }

    protected override void Update()
    {
        base.Update();
        switch (m_GameState)
        {
            case GameState.Playing:
                CheckTeamStatus();
                //m_Root.Q<TextElement>("Team1").text = m_ScoreTeam1.ToString();
                m_Root.Q<TextElement>("Team1").text = "test";
                m_Root.Q<TextElement>("Team2").text = m_ScoreTeam2.ToString();
                SetVisibleScoreBoard(true);
                break;
            case GameState.RoundEnd:
                // Attendez que la coroutine ShowEndRoundMessage termine son ex�cution.
                SetVisibleScoreBoard(true);
                break;
            case GameState.RoundStart:
                // Attendez que la coroutine StartNextRound termine son ex�cution.
                break;
        }
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
            Debug.Log("Team 1 Win Round");
            m_ScoreTeam1++;
            StartCoroutine(ManageRoundEnd());
            return;
        }
        else if (livingPlayersTeam2 > 0 && livingPlayersTeam1 == 0)
        {
            Debug.Log("Team 2 Win Round");
            m_ScoreTeam2++;
            StartCoroutine(ManageRoundEnd());
            return;
        }

        return;
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

    private IEnumerator ManageRoundEnd()
    {
        m_GameState = GameState.RoundEnd;

        DisablePlayerMovementScripts();

        // Afficher le message de fin de round pendant 3 secondes
        yield return new WaitForSeconds(3f);

        ShowEndRoundMessage();

        StartCoroutine(StartNextRound());
    }

    private IEnumerator StartNextRound()
    {
        m_GameState = GameState.RoundStart;

        RespawnPlayers();

        ShowStartRoundTimer();

        yield return new WaitForSeconds(3f);

        EnablePlayerMovementScripts();

        m_GameState = GameState.Playing;
    }

    /*private void StartNextRound()
    {
        //TO DO : Figer les joueurs restants, afficher message de fin de round,
        //Respawn, Update Score (UI?), Timer affich� � l'�cran avant de laisser les joueurs se d�placer ect
        m_CurrentRound++;
        if (m_ScoreTeam1 == m_ScoreToWin || m_ScoreTeam2 == m_ScoreToWin)
        {
            EndGame();
            return;
        }
        else
            StartCoroutine(ShowEndRoundMessage());
    }*/

    private void RespawnPlayers()
    {
        foreach (var playerData in m_PlayersGameObjects.Keys)
        {
            RespawnPlayer(playerData);
        }

        return;
    }

    private void ShowStartRoundTimer()
    {
        //TO DO : Timer affich� � l'�cran de chaque joueur de 3 secondes avec un cercle autour qui se r�tracte

        return;
    }

    private IEnumerator ShowEndRoundMessage()
    {
        DisablePlayerMovementScripts();
        //TO DO : Afficher message � la fin du round pendant quelques second avant de reset, respawn ect : Round Win ou Loose 
        //                                                                                                   1 - 0 ou 0 - 1

        yield return new WaitForSeconds(3f); //3s ou 5s

        if (m_ScoreTeam1 == m_ScoreToWin || m_ScoreTeam2 == m_ScoreToWin)
            EndGame();
        else
            StartNextRound();
    }

    private void EndGame()
    {
        //TO DO : Ecran de fin (score, leaderboard?) puis retour au lobby apr�s un certain temps
    }

    private void EnablePlayerMovementScripts()
    {
        foreach (var playerObject in m_PlayersGameObjects.Values)
        {
            var movementScript = playerObject.GetComponent<PlayerCharacterController>();
            if (movementScript != null)
                movementScript.enabled = true;
        }

        return;
    }

    private void DisablePlayerMovementScripts()
    {
        foreach (var playerObject in m_PlayersGameObjects.Values)
        {
            var movementScript = playerObject.GetComponent<PlayerCharacterController>();
            if (movementScript != null)
                movementScript.enabled = false;
        }

        return;
    }

    private void SetVisibleScoreBoard(bool pIsActive)
    {
        if (pIsActive)
        {
            m_Root.Q<VisualElement>("EndRound").style.display = DisplayStyle.Flex;
            m_Root.Q<VisualElement>("ScoreContainer");

            var listView = m_Root.Q<ListView>("ScoreContainer");
            listView.Clear();
            var items = new List<VisualElement>();

            foreach (var playerData in m_PlayersGameObjects.Keys)
            {
                var scoreRow = m_ScoreElement.CloneTree();
                scoreRow.Q<TextElement>("Name").text = playerData.PlayerName.ToString();
                scoreRow.Q<TextElement>("Kills").text = playerData.PlayerKills.ToString();
                scoreRow.Q<TextElement>("Deaths").text = playerData.PlayerDeaths.ToString();
                items.Add(scoreRow);
            }

            listView.makeItem = () => new VisualElement();
            listView.bindItem = (element, i) =>
            {
                element.Clear();
                element.Add(items[i]);
            };
            listView.itemsSource = items;
            listView.Rebuild();
        }
        else
        {
            m_Root.Q<TextElement>("EndRound").style.display = DisplayStyle.None;
        }
    }
}
