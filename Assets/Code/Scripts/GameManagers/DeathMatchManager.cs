using System;
using System.Collections.Generic;
using GameManagers;
using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class DeathMatchManager : GameManager
{
	[SerializeField] private UIDocument m_Document;
	[SerializeField] private VisualTreeAsset m_ScoreElement;
	[SerializeField] [Required] private RoundManager m_RoundManager;

	private NetworkVariable<GameState> m_GameState;

	private VisualElement m_Root;

	protected override void Awake()
	{
		base.Awake();

		m_Root = m_Document.rootVisualElement;

		m_GameState = new NetworkVariable<GameState>();

		MultiplayerManager.Instance.OnPlayerDataNetworkListChanged += OnPlayerDataNetworkListChanged;
		OnPlayerDataNetworkListChanged(null, null);

		m_Root.Q<TextElement>("Team1").text = m_RoundManager.ScoreTeam1.Value.ToString();
		m_RoundManager.OnScoreTeam1Changed += value => { m_Root.Q<TextElement>("Team1").text = value.ToString(); };
		m_Root.Q<TextElement>("Team2").text = m_RoundManager.ScoreTeam2.Value.ToString();
		m_RoundManager.OnScoreTeam2Changed += value => { m_Root.Q<TextElement>("Team2").text = value.ToString(); };
		m_Root.Q<TextElement>("Victory").style.display = DisplayStyle.None;

		if (!NetworkManager.IsServer)
			return;

		m_GameState.Value = GameState.Playing;

		m_RoundManager.OnRoundEnded += Server_OnRoundEnded;
		m_RoundManager.OnRoundStarting += Server_OnRoundStarting;
		m_RoundManager.OnRoundStarted += Server_OnRoundStarted;
		m_RoundManager.OnEndMatch += Server_OnEndMatch;
	}

	protected void Update()
	{
		switch (m_GameState.Value)
		{
			case GameState.Playing:
				if (Input.GetKeyDown(KeyCode.Tab))
					SetVisibleScoreBoard(true);
				else if (Input.GetKeyUp(KeyCode.Tab)) SetVisibleScoreBoard(false);

				if (NetworkManager.IsServer)
					Server_CheckTeamStatus();
				break;
			case GameState.RoundStart:
				break;
			case GameState.RoundEnd:
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	private void Server_OnRoundEnded(object sender, RoundManager.RoundEventArgs e)
	{
		m_GameState.Value = GameState.RoundEnd;

		Server_DisablePlayerMovementScripts();
		ShowEndRoundMessage_ClientRpc();
	}

	private void Server_OnRoundStarting(object sender, EventArgs e)
	{
		m_GameState.Value = GameState.RoundStart;

		Server_RespawnPlayers();
		Server_DisablePlayerMovementScripts();

		ShowStartRoundTimer_ClientRpc();
	}

	private void Server_OnRoundStarted(object sender, EventArgs e)
	{
		Server_EnablePlayerMovementScripts();

		m_GameState.Value = GameState.Playing;
	}

	private void Server_OnEndMatch(object sender, RoundManager.RoundEventArgs e)
	{
		Server_DisablePlayerMovementScripts();
		EndGame_ClientRpc(e.IsTeamOneWin);
	}

	protected override void SceneManager_OnLoadEventCompleted(string pSceneName, LoadSceneMode pLoadMode,
		List<ulong> pClientsCompleted, List<ulong> pClientTimouts)
	{
		foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
		{
			var player = m_SpawnManager.Server_SpawnPlayer(clientId);
			m_PlayersGameObjects.Add(clientId, player);
		}

		base.SceneManager_OnLoadEventCompleted(pSceneName, pLoadMode, pClientsCompleted, pClientTimouts);
	}

	private void Server_CheckTeamStatus()
	{
		var livingPlayersTeam1 = CountLivingPlayers(true);
		var livingPlayersTeam2 = CountLivingPlayers(false);

		if (livingPlayersTeam1 > 0 && livingPlayersTeam2 == 0)
			m_RoundManager.Server_EndRound(true);
		else if (livingPlayersTeam2 > 0 && livingPlayersTeam1 == 0) m_RoundManager.Server_EndRound(false);
	}

	private int CountLivingPlayers(bool isTeamOne)
	{
		var count = 0;
		foreach (var clientId in m_PlayersGameObjects.Keys)
		{
			var playerData = MultiplayerManager.Instance.FindPlayerData(clientId);
			if (playerData.IsTeamOne == isTeamOne && playerData.IsAlive)
				count++;
		}

		return count;
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
	private void EndGame_ClientRpc(bool pIsTeamOnWin)
	{
		SetVictoryScreen(pIsTeamOnWin);
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

	private void SetVictoryScreen(bool pIsTeamOneWin)
	{
		var playerData =
			MultiplayerManager.Instance.GetPlayerDataByIndex(
				MultiplayerManager.Instance.FindPlayerDataIndex(NetworkManager.Singleton.LocalClientId));
		var victory = (playerData.IsTeamOne && pIsTeamOneWin) || (!playerData.IsTeamOne && !pIsTeamOneWin);

		m_Root.Q<TextElement>("Victory").style.display = DisplayStyle.Flex;
		m_Root.Q<TextElement>("Victory").text = victory ? "Victory" : "Lose";
		m_Root.Q<TextElement>("Victory").style.color = victory ? Color.green : Color.red;
	}
}