using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class FreeForAllManager : GameManager
{
	[Header("Heal Prefab")] [SerializeField]
	private Transform m_HealBonusPrefab;

	[Header("Win Condition")] [SerializeField]
	private int m_WinConditionKills;

	private VisualElement m_OwnData;
	private VisualElement m_EnemyData;
	protected override void Awake()
	{
		base.Awake();

		m_OwnData = m_Root.Q<VisualElement>("OwnPlayerFFA");
		m_OwnData.Q<TextElement>("PlayerName").text = MultiplayerManager.Instance
			.FindPlayerData(NetworkManager.Singleton.LocalClientId).PlayerName.ToString();
		m_OwnData.Q<TextElement>("PlayerKills").text = "0";
		m_EnemyData = m_Root.Q<VisualElement>("EnemyPlayerFFA");
		m_EnemyData.Q<TextElement>("PlayerKills").text = "0";
		foreach (var player in MultiplayerManager.Instance.GetPlayerDatas())
		{
			if (player.ClientId == NetworkManager.Singleton.LocalClientId)
				continue;
			
			m_EnemyData.Q<TextElement>("PlayerName").text = player.PlayerName.ToString();
			break;
		}
		
		MultiplayerManager.Instance.OnPlayerDataNetworkListChanged += UI_OnPlayerKill;
	}

	private void UI_OnPlayerKill(object sender, EventArgs e)
	{
		PlayerData enemyMostKill = default;
		enemyMostKill.PlayerKills = -1;
		foreach (var player in MultiplayerManager.Instance.GetPlayerDatas())
		{
			if (player.ClientId == NetworkManager.Singleton.LocalClientId)
			{
				m_OwnData.Q<TextElement>("PlayerKills").text = player.PlayerKills.ToString();
				continue;
			}
			
			if (enemyMostKill.PlayerKills < player.PlayerKills)
				enemyMostKill = player;
		}
		
		m_EnemyData.Q<TextElement>("PlayerName").text = enemyMostKill.PlayerName.ToString();
		m_EnemyData.Q<TextElement>("PlayerKills").text = enemyMostKill.PlayerKills.ToString();

        // Reorder the visual element player scores based on their max kills
        var scoreList = m_Root.Q("Score");
        var orderedPlayers = scoreList.Children().OrderByDescending(p => int.Parse(p.Q<TextElement>("PlayerKills").text)).ToArray();
        foreach (var player in orderedPlayers)
        {
            scoreList.Remove(player);
            scoreList.Add(player);
        }
	}

	private void Update()
	{
		if (!NetworkManager.IsServer)
			return;

		var playersData = new List<PlayerData>();
		foreach (var playerData in m_PlayersGameObjects.Keys)
			playersData.Add(MultiplayerManager.Instance.FindPlayerData(playerData));

		Server_CheckWinCondition(playersData.ToArray());
	}

	protected override void SceneManager_OnLoadEventCompleted(string pSceneName, LoadSceneMode pLoadMode,
		List<ulong> pClientsCompleted, List<ulong> pClientTimouts)
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
			if (player.PlayerKills >= m_WinConditionKills)
			{
				EndGame_ClientRpc(player);
				break;
			}
	}

	[ClientRpc]
	private void EndGame_ClientRpc(PlayerData pWinner)
	{
		var victory = pWinner.ClientId == NetworkManager.Singleton.LocalClientId;

		m_WinScreen.rootVisualElement.Q<TextElement>("Win").style.display = victory ? DisplayStyle.Flex : DisplayStyle.None;
		m_WinScreen.rootVisualElement.Q<TextElement>("Lose").style.display = victory ? DisplayStyle.None : DisplayStyle.Flex;
		m_WinScreen.rootVisualElement.style.display = DisplayStyle.Flex;
	
	
		UnityEngine.Cursor.lockState = CursorLockMode.None;
		UnityEngine.Cursor.visible = true;
	}

	public override void Server_PlayerHit(float pDamage, Transform pGo, ulong pOwnerId)
	{
		base.Server_PlayerHit(pDamage, pGo, pOwnerId);
		var playerData = FindPlayerData(pGo);

		if (playerData.PlayerHealth > 0) return;

		if (Random.value <= 0.5f)
			if (m_HealBonusPrefab != null)
				Instantiate(m_HealBonusPrefab, pGo.position, Quaternion.identity);

		var indexKiller = MultiplayerManager.Instance.FindPlayerDataIndex(pOwnerId);
		var playerDataKiller = MultiplayerManager.Instance.GetPlayerDataByIndex(indexKiller);

		Server_SetCamera(playerData.ClientId, playerDataKiller.ClientId);
		Server_SetGameObject(playerData.ClientId, false);
		m_SpawnManager.Server_RespawnPlayer(m_PlayersGameObjects[playerData.ClientId], playerData.ClientId);
	}
}