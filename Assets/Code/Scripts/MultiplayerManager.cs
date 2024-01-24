using System;
using System.Collections.Generic;
using System.Reflection;
using Network;
using NUnit.Framework;
using ScriptableObjects.GameModes;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class MultiplayerManager : NetworkBehaviour
{
	private const string k_PlayerPref_PlayerName = "PlayerName";

	public int MaxPlayerAmount = 4;
	[FormerlySerializedAs("GameMode")] public GameModeConfig GameModeConfig;
	
	public event EventHandler OnTryingToJoinGame;
	public event EventHandler OnFailedToJoinGame;
	public event EventHandler OnPlayerDataNetworkListChanged;
	
	private NetworkList<PlayerData> m_PlayerDataNetworkList;
	private string m_PlayerName;

	// TODO : Better alterning teams
	static bool IsTeamOne = true;
	
    public string PlayerName
	{
		get => m_PlayerName;
		set
		{
			m_PlayerName = value;
			PlayerPrefs.SetString(k_PlayerPref_PlayerName, m_PlayerName);
		}
	}
	
	public static MultiplayerManager Instance { get; private set; }

    private void Awake()
	{
		if (Instance)
			Destroy(Instance.gameObject);

		Instance = this;
		DontDestroyOnLoad(gameObject);
			
		m_PlayerName = PlayerPrefs.GetString(k_PlayerPref_PlayerName, "Client" + Random.Range(100, 1000));
		m_PlayerDataNetworkList = new NetworkList<PlayerData>();
		m_PlayerDataNetworkList.OnListChanged += PlayerDataNetwork_OnListChanged;
	}

	private void PlayerDataNetwork_OnListChanged(NetworkListEvent<PlayerData> pChangeEvent)
	{
		OnPlayerDataNetworkListChanged?.Invoke(this, EventArgs.Empty);
	}

	private void Start()
	{
		SetLobbyManagerCallbacks();
	}

	private void SetLobbyManagerCallbacks()
	{
		LobbyManager.Instance.OnCreateLobbyStarted += Lobby_OnCreateLobbyStarted;
		LobbyManager.Instance.OnCreateLobbyFailed += Lobby_OnCreateLobbyFailed;
		LobbyManager.Instance.OnJoinLobbyStarted += Lobby_OnJoinLobbyStarted;
		LobbyManager.Instance.OnJoinLobbyFailed += Lobby_OnJoinLobbyFailed;
	}

	private void Lobby_OnCreateLobbyStarted(object sender, EventArgs e)
	{
		MessagePopUp.Instance.Open("Game", "Creating Lobby ...");
	}

	private void Lobby_OnCreateLobbyFailed(object sender, EventArgs e)
	{
		MessagePopUp.Instance.Open("Game", "Failed to create the lobby", ("Close", MessagePopUp.Instance.Hide));
	}

	private void Lobby_OnJoinLobbyStarted(object sender, EventArgs e)
	{
		MessagePopUp.Instance.Open("Game", "Joining Lobby ...");
	}

	private void Lobby_OnJoinLobbyFailed(object sender, EventArgs e)
	{
		MessagePopUp.Instance.Open("Game", "Failed to join the lobby", ("Close", MessagePopUp.Instance.Hide));
	}

	public void StartHost()
	{
		MessagePopUp.Instance.Open("Game", "Creating game ...");
		SetNetworkManagerCallbacks();
		NetworkManager.Singleton.StartHost();
	}


    private void SetNetworkManagerCallbacks()
	{
		NetworkManager.Singleton.ConnectionApprovalCallback += Network_ConnectionApprovalCallback;
		NetworkManager.Singleton.OnClientConnectedCallback += Network_OnClientConnectedCallback;
		NetworkManager.Singleton.OnClientDisconnectCallback += Network_OnClientDisconnectCallback;

    }

	private void Network_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest pConnectionApprovalRequest, NetworkManager.ConnectionApprovalResponse pConnectionApprovalResponse)
	{
		if (SceneManager.GetActiveScene().name != "Lobby")
		{
			pConnectionApprovalResponse.Approved = false;
			pConnectionApprovalResponse.Reason = "Game has alreadyStarted";
			return;
		}

		if (NetworkManager.Singleton.ConnectedClientsIds.Count >= MaxPlayerAmount)
		{
			pConnectionApprovalResponse.Approved = false;
			pConnectionApprovalResponse.Reason = "Game is full";
			return;
		}
		
		pConnectionApprovalResponse.Approved = true;
	}

	private void Network_OnClientConnectedCallback(ulong pClientId)
	{
		m_PlayerDataNetworkList.Add(new PlayerData
		{
			ClientId = pClientId,
			IsTeamOne = IsTeamOne,
			PlayerMaxHealth = 100,
			PlayerHealth = 100
		});
		IsTeamOne = !IsTeamOne;
		SetPlayerNameServerRpc(m_PlayerName);
		SetPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);
    }

	private void Network_OnClientDisconnectCallback(ulong pClientId)
	{
		m_PlayerDataNetworkList.Remove(m_PlayerDataNetworkList[FindPlayerDataIndex(pClientId)]);
	}

	public void StartClient()
	{
		MessagePopUp.Instance.Open("Game", "Connecting to game ...");
		OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);

		SetNetworkClientCallbacks();
        NetworkManager.Singleton.StartClient();
    }

	private void SetNetworkClientCallbacks()
	{
		NetworkManager.Singleton.OnClientConnectedCallback += Network_Client_OnClientConnectedCallback;
		NetworkManager.Singleton.OnClientDisconnectCallback += Network_Client_OnClientDisconnectCallback;
    }

    private void Network_Client_OnClientConnectedCallback(ulong pClientId)
	{
		SetPlayerNameServerRpc(m_PlayerName);
		SetPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);
	}

	private void Network_Client_OnClientDisconnectCallback(ulong pClientId)
	{
		MessagePopUp.Instance.Open("Disconnected from Game", NetworkManager.Singleton.DisconnectReason == "" ? "Failed to connect" : NetworkManager.Singleton.DisconnectReason, ("Close", MessagePopUp.Instance.Hide));
        
		NetworkManager.Singleton.Shutdown();
        Destroy(NetworkManager.Singleton.gameObject);
        SceneManager.LoadScene("Base", LoadSceneMode.Single);
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
        OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
	}

	[ServerRpc(RequireOwnership = false)]
	private void SetPlayerNameServerRpc(string pPlayerName, ServerRpcParams pServerRpcParams = default)
	{
		var playerDataIndex = FindPlayerDataIndex(pServerRpcParams.Receive.SenderClientId);
		var playerData = m_PlayerDataNetworkList[playerDataIndex];

		playerData.PlayerName = pPlayerName;
		
		m_PlayerDataNetworkList[playerDataIndex] = playerData;

		if(GameManager.Instance != null)
			GameManager.Instance.SetPlayerData(playerDataIndex, playerData.ClientId);
    }

	[ServerRpc(RequireOwnership = false)]
	public void SetPlayerTeamServerRpc(bool pIsTeamOne, ServerRpcParams pServerRpcParams = default)
	{
		var playerDataIndex = FindPlayerDataIndex(pServerRpcParams.Receive.SenderClientId);
		var playerData = m_PlayerDataNetworkList[playerDataIndex];

		playerData.IsTeamOne = pIsTeamOne;
		
		m_PlayerDataNetworkList[playerDataIndex] = playerData;

        if (GameManager.Instance != null)
            GameManager.Instance.SetPlayerData(playerDataIndex, playerData.ClientId);
    }

	[ServerRpc(RequireOwnership = false)]
	private void SetPlayerIdServerRpc(string pPlayerId, ServerRpcParams pServerRpcParams = default)
	{
		var playerDataIndex = FindPlayerDataIndex(pServerRpcParams.Receive.SenderClientId);
		var playerData = m_PlayerDataNetworkList[playerDataIndex];

		playerData.PlayerId = pPlayerId;
		
		m_PlayerDataNetworkList[playerDataIndex] = playerData;

        if (GameManager.Instance != null)
            GameManager.Instance.SetPlayerData(playerDataIndex, playerData.ClientId);
    }

    public void PlayerHit(float pDamage, Transform pGo, ulong pOwnerId)
    {
        var playerData = GameManager.Instance.FindPlayerData(pGo);
		var index = FindPlayerDataIndexByPlayerData(playerData);
        playerData.PlayerHealth -= pDamage;

        m_PlayerDataNetworkList[index] = playerData;

        if (GameManager.Instance != null)
            GameManager.Instance.SetPlayerData(index, playerData.ClientId);

        if (playerData.PlayerHealth <= 0)
        {
			if(GameModeConfig.CanRespawn)
			{
				// TODO : Respawn
				Debug.Log("TODO : RESPAWN");
				//GameManager.Instance.RespawnPlayer(playerData.ClientId);
				//playerData.PlayerHealth = playerData.PlayerMaxHealth;
            }
			else
			{
                PlayerData teamMateData;

				if (TryFindTeammate(playerData, out teamMateData))
				{
					Debug.Log(playerData.ClientId);
					Debug.Log(teamMateData.ClientId);

					GameManager.Instance.SetCamera_ServerRpc(playerData.ClientId, teamMateData.ClientId);				

				}
				GameManager.Instance.SetGameObject_ServerRpc(playerData.ClientId, false);
			}

            playerData.PlayerDeaths += 1;
            m_PlayerDataNetworkList[index] = playerData;

            if (GameManager.Instance != null)
                GameManager.Instance.SetPlayerData(index, playerData.ClientId);

            var indexKiller = FindPlayerDataIndex(pOwnerId);
            var playerDataKiller = GetPlayerDataByIndex(indexKiller);
			playerDataKiller.PlayerKills += 1;
            m_PlayerDataNetworkList[indexKiller] = playerDataKiller;

            if (GameManager.Instance != null)
                GameManager.Instance.SetPlayerData(indexKiller, playerDataKiller.ClientId);
        }
    }

    public bool IsPlayerIndexConnected(int pIndex)
	{
		return pIndex < m_PlayerDataNetworkList.Count;
	}

	public PlayerData GetPlayerDataByIndex(int pIndex)
	{
		return m_PlayerDataNetworkList[pIndex];
	}

	public int FindPlayerDataIndex(ulong pClientId)
	{
		for (var index = 0; index < m_PlayerDataNetworkList.Count; index++)
		{
			if (m_PlayerDataNetworkList[index].ClientId == pClientId)
			{
				return index;
			}
		}
		return -1; // Return -1 if no player data with the given client id is found
	}

    public int FindPlayerDataIndexByPlayerData(PlayerData pPlayerData)
    {
	    Debug.Log($"Search for {pPlayerData.ClientId} in {m_PlayerDataNetworkList.Count} ids");
        for (var index = 0; index < m_PlayerDataNetworkList.Count; index++)
        {
	        if (!m_PlayerDataNetworkList[index].Equals(pPlayerData)) continue;
	        
	        Debug.Log($"Index : {index}");
	        return index;
        }
        Debug.Log($"Not Found");
        return -1; // Return -1 if no player data with the given client id is found
    }

    public PlayerData FindPlayerData(ulong pClientId)
    {
	    foreach(var player in m_PlayerDataNetworkList)
		    if (pClientId == player.ClientId)
			    return player;

	    return default;
    }

    public NetworkList<PlayerData> GetPlayerDatas()
    {
		return m_PlayerDataNetworkList;
    }

    private bool TryFindTeammate(PlayerData playerData, out PlayerData teammateData)
    {
        teammateData = default;

        foreach (var teammate in m_PlayerDataNetworkList)
        {
            if (teammate.IsTeamOne == playerData.IsTeamOne && teammate.PlayerHealth > 0)
            {
				teammateData = teammate;
                return true;
            }
        }

		return false;
    }

}