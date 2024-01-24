using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.FPS.Gameplay;
using UnityEngine.UIElements;
using System;
using Random = UnityEngine.Random;

public class DeathMatchManager : GameManager
{
    [SerializeField] private UIDocument m_Document;
    [SerializeField] private VisualTreeAsset m_ScoreElement;


    private int m_ScoreToWin = 3;
    private int m_ScoreTeam1 = 0;
    private int m_ScoreTeam2 = 0;

    private int m_MaxRounds = 5;
    private int m_CurrentRound = 1;

    private VisualElement m_Root;

    private GameState m_GameState = GameState.Playing;

    protected override void Awake()
    {
        base.Awake();

        m_Root = m_Document.rootVisualElement;

        MultiplayerManager.Instance.OnPlayerDataNetworkListChanged += OnPlayerDataNetworkListChanged;
        OnPlayerDataNetworkListChanged(null, null);
    }

    protected void Update()
    {
        switch (m_GameState)
        {
            case GameState.Playing:
                {
                    CheckTeamStatus();
                    m_Root.Q<TextElement>("Team1").text = m_ScoreTeam1.ToString();
                    m_Root.Q<TextElement>("Team2").text = m_ScoreTeam2.ToString();
                    if (Input.GetKeyDown(KeyCode.Tab))
                    {
                        SetVisibleScoreBoard(true);
                    }
                    else if (Input.GetKeyUp(KeyCode.Tab))
                    {
                        SetVisibleScoreBoard(false);
                    }
                }
                break;
            case GameState.RoundEnd:
                SetVictory();
                SetVisibleScoreBoard(true);
                break;
            case GameState.RoundStart:
                m_Root.Q<TextElement>("Victory").style.display = DisplayStyle.None;
                break;
        }


    }

    protected override void SceneManager_OnLoadEventCompleted(string pSceneName, LoadSceneMode pLoadMode, List<ulong> pClientsCompleted, List<ulong> pClientTimouts)
    {
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Debug.Log($"- {clientId}");
            var player = m_SpawnManager.SpawnPlayer(clientId);
            m_PlayersGameObjects.Add(clientId, player);
        }
        base.SceneManager_OnLoadEventCompleted(pSceneName, pLoadMode, pClientsCompleted, pClientTimouts);
    }

    private void CheckTeamStatus()
    {
        int livingPlayersTeam1 = CountLivingPlayers(true);
        int livingPlayersTeam2 = CountLivingPlayers(false);

        if (livingPlayersTeam1 > 0 && livingPlayersTeam2 == 0)
        {
            m_ScoreTeam1++;
            StartCoroutine(ManageRoundEnd());
            return;
        }
        if (livingPlayersTeam2 > 0 && livingPlayersTeam1 == 0)
        {
            m_ScoreTeam2++;
            StartCoroutine(ManageRoundEnd());
            return;
        }
    }

    private int CountLivingPlayers(bool isTeamOne)
    {
        int count = 0;
        foreach (var clientId in m_PlayersGameObjects.Keys)
        {
            var playerData = MultiplayerManager.Instance.FindPlayerData(clientId);
            if (playerData.IsTeamOne == isTeamOne && playerData.IsAlive)
            {
                count++;
            }
        }
        return count;
    }

    private IEnumerator ManageRoundEnd()
    {
        m_GameState = GameState.RoundEnd;

        DisablePlayerMovementScripts();

        ShowEndRoundMessage();

        yield return null;
    }

    private IEnumerator  StartNextRound()
    {
        m_GameState = GameState.RoundStart;

        RespawnPlayers();

        ShowStartRoundTimer();

        yield return new WaitForSeconds(3f);

        EnablePlayerMovementScripts();

        m_GameState = GameState.Playing;
    }

    private void ShowStartRoundTimer()
    {
        //TODO : Timer affiché à l'écran de chaque joueur de 3 secondes avec un cercle autour qui se rétracte

        return;
    }

    private void ShowEndRoundMessage()
    {
        //TODO : Afficher message à la fin du round pendant 3 ou 5 secondes avant de reset, respawn ect : Round Win ou Loose 
        //                                                                                                   1 - 0 ou 0 - 1

        if (m_ScoreTeam1 == m_ScoreToWin || m_ScoreTeam2 == m_ScoreToWin)
            EndGame();
        else
            StartCoroutine(StartNextRound());
    }

    private void EndGame()
    {
        //TODO : Ecran de fin (score, leaderboard?) puis retour au lobby après un certain temps
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
        }
        else
        {
            m_Root.Q<VisualElement>("EndRound").style.display = DisplayStyle.None;
        }
    }

    private void OnPlayerDataNetworkListChanged(object sender, EventArgs e)
    {
        var listView = m_Root.Q<ListView>("ScoreContainer");
        listView.Clear();
        var items = new List<VisualElement>();

        foreach (var playerData in MultiplayerManager.Instance.GetPlayerDatas())
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

    private void SetVictory()
    {
        var playerData = MultiplayerManager.Instance.GetPlayerDataByIndex(MultiplayerManager.Instance.FindPlayerDataIndex(OwnerClientId));
        bool team1Win = m_ScoreTeam1 == m_ScoreToWin;
        bool victory = (playerData.IsTeamOne && team1Win) || (!playerData.IsTeamOne && !team1Win);

        m_Root.Q<TextElement>("Victory").style.display = DisplayStyle.Flex;
        m_Root.Q<TextElement>("Victory").text = victory ? "Victory" : "Lose";
        m_Root.Q<TextElement>("Victory").style.color = victory ? Color.green : Color.red;
    }
}
