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
    private NetworkVariable<int> m_ScoreTeam1;
    private NetworkVariable<int> m_ScoreTeam2;

    private int m_MaxRounds = 5;
    private int m_CurrentRound = 1;

    private VisualElement m_Root;

    private NetworkVariable<GameState> m_GameState;

    protected override void Awake()
    {
        base.Awake();

        m_Root = m_Document.rootVisualElement;
        
        m_ScoreTeam1 = new NetworkVariable<int>();
        m_ScoreTeam2 = new NetworkVariable<int>();
        m_GameState = new NetworkVariable<GameState>();

        MultiplayerManager.Instance.OnPlayerDataNetworkListChanged += OnPlayerDataNetworkListChanged;
        OnPlayerDataNetworkListChanged(null, null);

        m_Root.Q<TextElement>("Team1").text = m_ScoreTeam1.Value.ToString();
        m_ScoreTeam1.OnValueChanged += (value, newValue) =>
        {
            m_Root.Q<TextElement>("Team1").text = newValue.ToString();
        };
        m_Root.Q<TextElement>("Team2").text = m_ScoreTeam2.Value.ToString();
        m_ScoreTeam2.OnValueChanged += (value, newValue) =>
        {
            m_Root.Q<TextElement>("Team2").text = newValue.ToString();
        };
        
        m_GameState.Value = GameState.Playing;
    }

    protected void Update()
    {
        switch (m_GameState.Value)
        {
            case GameState.Playing:
                {
                    if (Input.GetKeyDown(KeyCode.Tab))
                    {
                        SetVisibleScoreBoard(true);
                    }
                    else if (Input.GetKeyUp(KeyCode.Tab))
                    {
                        SetVisibleScoreBoard(false);
                    }
                    
                    if (IsServer)
                        Server_CheckTeamStatus();
                }
                break;
            case GameState.RoundEnd:
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
            var player = m_SpawnManager.SpawnPlayer(clientId);
            m_PlayersGameObjects.Add(clientId, player);
        }
        base.SceneManager_OnLoadEventCompleted(pSceneName, pLoadMode, pClientsCompleted, pClientTimouts);
    }

    private void Server_CheckTeamStatus()
    {
        int livingPlayersTeam1 = CountLivingPlayers(true);
        int livingPlayersTeam2 = CountLivingPlayers(false);

        if (livingPlayersTeam1 > 0 && livingPlayersTeam2 == 0)
        {
            m_ScoreTeam1.Value++;
            StartCoroutine(Server_ManageRoundEnd());
        }
        else if (livingPlayersTeam2 > 0 && livingPlayersTeam1 == 0)
        {
            m_ScoreTeam2.Value++;
            StartCoroutine(Server_ManageRoundEnd());
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

    private IEnumerator Server_ManageRoundEnd()
    {
        m_GameState.Value = GameState.RoundEnd;

        DisablePlayerMovementScripts_ServerRpc();

        ShowEndRoundMessage_ClientRpc();
        
        if (m_ScoreTeam1.Value == m_ScoreToWin || m_ScoreTeam2.Value == m_ScoreToWin)
            EndGame_ClientRpc();
        else
            StartCoroutine(Server_StartNextRound());
        
        yield return null;
    }

    private IEnumerator Server_StartNextRound()
    {
        m_GameState.Value = GameState.RoundStart;

        Server_RespawnPlayers();
        DisablePlayerMovementScripts_ServerRpc();

        ShowStartRoundTimer_ClientRpc();

        yield return new WaitForSeconds(3f);

        EnablePlayerMovementScripts_ServerRpc();

        m_GameState.Value = GameState.Playing;
    }

    [ClientRpc]
    private void ShowStartRoundTimer_ClientRpc()
    {
        //TODO : Timer affiché à l'écran de chaque joueur de 3 secondes avec un cercle autour qui se rétracte
    }

    [ClientRpc]
    private void ShowEndRoundMessage_ClientRpc()
    {
        //TODO : Afficher message à la fin du round pendant 3 ou 5 secondes avant de reset, respawn ect : Round Win ou Loose 
        //                                                                                                   1 - 0 ou 0 - 1
    }

    [ClientRpc]
    private void EndGame_ClientRpc()
    {
        SetVictoryScreen();
    }

    [ServerRpc]
    private void EnablePlayerMovementScripts_ServerRpc()
    {
        foreach (var playerObject in m_PlayersGameObjects.Values)
        {
            var movementScript = playerObject.GetComponent<PlayerCharacterController>();
            if (!movementScript) continue;
            
            movementScript.SetActive_ClientRpc(true);
        }
    }

    [ServerRpc]
    private void DisablePlayerMovementScripts_ServerRpc()
    {
        
        foreach (var playerObject in m_PlayersGameObjects.Values)
        {
            var movementScript = playerObject.GetComponent<PlayerCharacterController>();
            if (!movementScript) continue;
            
            movementScript.SetActive_ClientRpc(false);
        }
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

    private void SetVictoryScreen()
    {
        var playerData = MultiplayerManager.Instance.GetPlayerDataByIndex(MultiplayerManager.Instance.FindPlayerDataIndex(NetworkManager.Singleton.LocalClientId));
        bool team1Win = m_ScoreTeam1.Value == m_ScoreToWin;
        bool victory = (playerData.IsTeamOne && team1Win) || (!playerData.IsTeamOne && !team1Win);

        m_Root.Q<TextElement>("Victory").style.display = DisplayStyle.Flex;
        m_Root.Q<TextElement>("Victory").text = victory ? "Victory" : "Lose";
        m_Root.Q<TextElement>("Victory").style.color = victory ? Color.green : Color.red;
    }
}
