using System;
using System.Collections.Generic;
using GameManagers;
using Network;
using Unity.FPS.Gameplay;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public enum GameState
{
	Playing,
	RoundStart,
	RoundEnd
}

public abstract class GameManager : NetworkBehaviour
{
	[SerializeField] protected UIDocument m_Document;
	[SerializeField] protected UIDocument m_WinScreen;
	[SerializeField] protected SpawnManager m_SpawnManager;
	protected Dictionary<ulong, Transform> m_PlayersGameObjects = new();

	public bool IsClientPaused;

	/// <summary>
	/// Event that is raised when a player kills another player.
	/// </summary>
	/// <param name="killer">The data of the player who performed the kill.</param>
	/// <param name="victim">The data of the player who was killed.</param>
	public Action<PlayerData, PlayerData> OnPlayerKill;

	protected VisualElement m_Root;
	
	public static GameManager Instance { get; private set; }

	protected virtual void Awake()
	{
		if (Instance)
			Destroy(Instance);

		Instance = this;

		m_Root = m_Document.rootVisualElement;
		m_WinScreen.rootVisualElement.style.display = DisplayStyle.None;
		m_WinScreen.rootVisualElement.Q<Button>("ToMenu").clicked += () =>
		{
			if (ScoreManager.Instance)
				Destroy(ScoreManager.Instance.gameObject);
			NetworkManager.Singleton.Shutdown();
			Destroy(NetworkManager.Singleton.gameObject);
			SceneManager.LoadScene("Base", LoadSceneMode.Single);
		};
		
		
		UnityEngine.Cursor.lockState = CursorLockMode.Locked;
		UnityEngine.Cursor.visible = false;
		
		MultiplayerManager.Instance.GetPlayerDatas().OnListChanged += OnPlayerDataNetworkListChanged;
	}

	private void OnPlayerDataNetworkListChanged(NetworkListEvent<PlayerData> e)
	{
		if (e.Value.ClientId != NetworkManager.LocalClientId)
			return;

		UpdatePlayerLife(e.Value);
	}

	private void UpdatePlayerLife(PlayerData e)
	{
		var health = m_Root.Q<ProgressBar>("Health");
		health.highValue = e.PlayerMaxHealth;
		health.value = e.PlayerHealth;
	}

	protected virtual void Start()
	{
		LobbyManager.Instance.SetLobbyNull();
		UpdatePlayerLife(MultiplayerManager.Instance.FindPlayerData(NetworkManager.Singleton.LocalClientId));
	}

	private void OnEnable()
	{
		m_SpawnManager.OnPlayerRespawn += ResetPlayerGameObject_OnPlayerSpawn;
	}

	private void OnDisable()
	{
		m_SpawnManager.OnPlayerRespawn -= ResetPlayerGameObject_OnPlayerSpawn;
	}

	private void ResetPlayerGameObject_OnPlayerSpawn(ulong pClientId)
	{
		Server_SetGameObject(pClientId, true);
		Server_SetCamera(pClientId, pClientId);
	}

	public override void OnNetworkSpawn()
	{
		if (!NetworkManager.IsServer)
			return;

		NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
	}

	public override void OnNetworkDespawn()
	{
		NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= SceneManager_OnLoadEventCompleted;
	}

	protected virtual void SceneManager_OnLoadEventCompleted(string pSceneName, LoadSceneMode pLoadMode,
		List<ulong> pClientsCompleted, List<ulong> pClientTimouts)
	{
		// TODO : Really needed?
	}

	protected void Server_RespawnPlayers()
	{
		if (!NetworkManager.IsServer)
			return;

		foreach (var clientId in m_PlayersGameObjects.Keys)
			if (m_PlayersGameObjects.TryGetValue(clientId, out var playerGameObject))
				m_SpawnManager.Server_RespawnPlayer(playerGameObject, clientId);
	}

	public PlayerData FindPlayerData(Transform pPlayerGameObject)
	{
		foreach (var player in m_PlayersGameObjects)
			if (pPlayerGameObject == player.Value)
				return MultiplayerManager.Instance.FindPlayerData(player.Key);

		return default;
	}

	public Transform FindPlayer(ulong pClientId)
	{
		return m_PlayersGameObjects.GetValueOrDefault(pClientId);
	}

	protected void Server_EnablePlayerMovementScripts()
	{
		if (!NetworkManager.IsServer)
			return;

		foreach (var playerObject in m_PlayersGameObjects.Values)
		{
			var movementScript = playerObject.GetComponent<PlayerCharacterController>();
			if (!movementScript) continue;

			movementScript.SetActive_ClientRpc(true);
		}
	}

	protected void Server_DisablePlayerMovementScripts()
	{
		if (!NetworkManager.IsServer)
			return;

		foreach (var playerObject in m_PlayersGameObjects.Values)
		{
			var movementScript = playerObject.GetComponent<PlayerCharacterController>();
			if (!movementScript) continue;

			movementScript.SetActive_ClientRpc(false);
		}
	}

	public void Server_SetCamera(ulong pClientId, ulong pTargetId)
	{
		if (!NetworkManager.IsServer)
			return;

		m_PlayersGameObjects[pTargetId].GetComponentInChildren<SetPlayerCamera>().Set_ClientRpc(new ClientRpcParams
		{
			Send = new ClientRpcSendParams
			{
				TargetClientIds = new List<ulong> { pClientId }
			}
		});
	}

	public void Server_SetGameObject(ulong pClientId, bool pIsActive)
	{
		if (!NetworkManager.IsServer)
			return;

		m_PlayersGameObjects[pClientId].GetComponent<SetGameObject>().SetGameObject_ClientRpc(pIsActive);
	}


	public virtual void Server_PlayerHit(float pDamage, Transform pGo, ulong pOwnerId)
	{
		if (!NetworkManager.IsServer)
			return;

		var playerData = FindPlayerData(pGo);
		var index = MultiplayerManager.Instance.FindPlayerDataIndexByPlayerData(playerData);
		playerData.PlayerHealth -= pDamage;

		MultiplayerManager.Instance.GetPlayerDatas()[index] = playerData;

		if (playerData.PlayerHealth > 0) return;

		if (MultiplayerManager.Instance.GameModeConfig.CanRespawn &&
		    MultiplayerManager.Instance.GameModeConfig.ModeName != "Free For All")
		{
			m_SpawnManager.Server_RespawnPlayer(m_PlayersGameObjects[playerData.ClientId], playerData.ClientId);
			playerData.PlayerHealth = playerData.PlayerMaxHealth;
		}
		else if (MultiplayerManager.Instance.GameModeConfig.ModeName != "Free For All")
		{
			if (TryFindTeammate(playerData, out var teammateData))
				Server_SetCamera(playerData.ClientId, teammateData.ClientId);

			Server_SetGameObject(playerData.ClientId, false);
		}

		playerData.PlayerDeaths += 1;
		MultiplayerManager.Instance.GetPlayerDatas()[index] = playerData;

		var indexKiller = MultiplayerManager.Instance.FindPlayerDataIndex(pOwnerId);
		var playerDataKiller = MultiplayerManager.Instance.GetPlayerDataByIndex(indexKiller);
		playerDataKiller.PlayerKills += 1;
		MultiplayerManager.Instance.GetPlayerDatas()[indexKiller] = playerDataKiller;
		
		OnPlayerKill?.Invoke(playerDataKiller, playerData);
	}

	private bool TryFindTeammate(PlayerData playerData, out PlayerData teammateData)
	{
		foreach (var teammate in MultiplayerManager.Instance.GetPlayerDatas())
		{
			if (teammate.IsTeamOne != playerData.IsTeamOne || !(teammate.PlayerHealth > 0)) continue;

			teammateData = teammate;
			return true;
		}

		teammateData = default;
		return false;
	}
}