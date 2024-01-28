using System;
using System.Collections.Generic;
using GameManagers;
using NaughtyAttributes;
using Unity.FPS.Gameplay;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class DeathMatchManager : GameManager
{
	[SerializeField] private VisualTreeAsset m_ScoreElement;
	[SerializeField] [Required] private RoundManager m_RoundManager;

	private NetworkVariable<GameState> m_GameState;

	protected override void Awake()
	{
		base.Awake();

		m_GameState = new NetworkVariable<GameState>();

		m_Root.Q<TextElement>("Team1Score").text = m_RoundManager.ScoreTeam1.Value.ToString();
		m_RoundManager.OnScoreTeam1Changed += value => { m_Root.Q<TextElement>("Team1Score").text = value.ToString(); };
		m_Root.Q<TextElement>("Team2Score").text = m_RoundManager.ScoreTeam2.Value.ToString();
		m_RoundManager.OnScoreTeam2Changed += value => { m_Root.Q<TextElement>("Team2Score").text = value.ToString(); };

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
		Server_RespawnPlayers();
		Server_DisablePlayerMovementScripts();
		EndGame_ClientRpc(e.IsTeamOneWin);
	}

	protected override void SceneManager_OnLoadEventCompleted(string pSceneName, LoadSceneMode pLoadMode,
		List<ulong> pClientsCompleted, List<ulong> pClientTimouts)
	{
		m_SpawnManager.SearchSpawns();
		foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
		{
			var player = m_SpawnManager.Server_SpawnPlayer(clientId);
			m_PlayersGameObjects.Add(clientId, player);

            PlayerData playerData = MultiplayerManager.Instance.FindPlayerData(clientId);
			if(playerData.PlayerActiveWeaponId != 0)
			{
				Debug.Log("Switch to last weapon");
				player.GetComponent<PlayerWeaponsManager>().SwitchToWeaponIndex(playerData.PlayerActiveWeaponId);
			}
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

	private void SetVictoryScreen(bool pIsTeamOneWin)
	{
		var playerData =
			MultiplayerManager.Instance.GetPlayerDataByIndex(
				MultiplayerManager.Instance.FindPlayerDataIndex(NetworkManager.Singleton.LocalClientId));
		var victory = (playerData.IsTeamOne && pIsTeamOneWin) || (!playerData.IsTeamOne && !pIsTeamOneWin);

		m_WinScreen.rootVisualElement.Q<TextElement>("Win").style.display = victory ? DisplayStyle.Flex : DisplayStyle.None;
		m_WinScreen.rootVisualElement.Q<TextElement>("Lose").style.display = victory ? DisplayStyle.None : DisplayStyle.Flex;
		m_WinScreen.rootVisualElement.style.display = DisplayStyle.Flex;
		
		
		UnityEngine.Cursor.lockState = CursorLockMode.None;
		UnityEngine.Cursor.visible = true;
	}
}