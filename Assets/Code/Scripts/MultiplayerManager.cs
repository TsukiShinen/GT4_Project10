using System;
using System.Reflection;
using Network;
using ScriptableObjects.GameModes;
using Unity.Netcode;
using Unity.Services.Authentication;
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
		{
			Destroy(this);
			return;
		}

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
		LobbyManager.Instance.OnCreateLobbySucceed += Lobby_OnCreateLobbySucceed;
		LobbyManager.Instance.OnCreateLobbyFailed += Lobby_OnCreateLobbyFailed;
		LobbyManager.Instance.OnJoinLobbyStarted += Lobby_OnJoinLobbyStarted;
		LobbyManager.Instance.OnJoinLobbySucceed += Lobby_OnJoinLobbySucceed;
		LobbyManager.Instance.OnJoinLobbyFailed += Lobby_OnJoinLobbyFailed;
	}

	private void Lobby_OnCreateLobbyStarted(object sender, EventArgs e)
	{
		MessagePopUp.Instance.Open("Game", "Creating Lobby ...");
	}

	private void Lobby_OnCreateLobbySucceed(object sender, EventArgs e)
	{
		MessagePopUp.Instance.Hide();
    }

	private void Lobby_OnCreateLobbyFailed(object sender, EventArgs e)
	{
		MessagePopUp.Instance.Open("Game", "Failed to create the lobby", ("Close", MessagePopUp.Instance.Hide));
	}

	private void Lobby_OnJoinLobbyStarted(object sender, EventArgs e)
	{
		MessagePopUp.Instance.Open("Game", "Joining Lobby ...");
	}

	private void Lobby_OnJoinLobbySucceed(object sender, EventArgs e)
	{
		MessagePopUp.Instance.Hide();
    }

	private void Lobby_OnJoinLobbyFailed(object sender, EventArgs e)
	{
		MessagePopUp.Instance.Open("Game", "Failed to join the lobby", ("Close", MessagePopUp.Instance.Hide));
	}

	public void StartHost()
	{
		SetNetworkManagerCallbacks();
		NetworkManager.Singleton.StartHost();
	}


    private void SetNetworkManagerCallbacks()
	{
		NetworkManager.Singleton.ConnectionApprovalCallback += Network_ConnectionApprovalCallback;
		NetworkManager.Singleton.OnClientConnectedCallback += Network_OnClientConnectedCallback;
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

	public void StartClient()
	{
		MessagePopUp.Instance.Open("Game", "Connecting to game ...");
		OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);

		SetNetworkClientCallbacks();
		NetworkManager.Singleton.StartClient();
		
		MessagePopUp.Instance.Hide();
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
			GameManager.Instance.SetPlayerData(playerDataIndex, playerData);
    }

	[ServerRpc(RequireOwnership = false)]
	public void SetPlayerTeamServerRpc(bool pIsTeamOne, ServerRpcParams pServerRpcParams = default)
	{
		var playerDataIndex = FindPlayerDataIndex(pServerRpcParams.Receive.SenderClientId);
		var playerData = m_PlayerDataNetworkList[playerDataIndex];

		playerData.IsTeamOne = pIsTeamOne;
		
		m_PlayerDataNetworkList[playerDataIndex] = playerData;

        if (GameManager.Instance != null)
            GameManager.Instance.SetPlayerData(playerDataIndex, playerData);
    }

	[ServerRpc(RequireOwnership = false)]
	private void SetPlayerIdServerRpc(string pPlayerId, ServerRpcParams pServerRpcParams = default)
	{
		var playerDataIndex = FindPlayerDataIndex(pServerRpcParams.Receive.SenderClientId);
		var playerData = m_PlayerDataNetworkList[playerDataIndex];

		playerData.PlayerId = pPlayerId;
		
		m_PlayerDataNetworkList[playerDataIndex] = playerData;

        if (GameManager.Instance != null)
            GameManager.Instance.SetPlayerData(playerDataIndex, playerData);
    }

    public void PlayerHit(float pDamage, GameObject pGo)
    {
        var playerData = GameManager.Instance.FindPlayerData(pGo);
		var index = FindPlayerDataIndexByPlayerData(playerData);
        playerData.PlayerHealth -= pDamage;

        m_PlayerDataNetworkList[index] = playerData;

        if (GameManager.Instance != null)
            GameManager.Instance.SetPlayerData(index, playerData);

        if (playerData.PlayerHealth <= 0)
        {
            GameManager.Instance.RespawnPlayer(playerData);
            playerData.PlayerHealth = playerData.PlayerMaxHealth;

            m_PlayerDataNetworkList[index] = playerData;

            if (GameManager.Instance != null)
                GameManager.Instance.SetPlayerData(index, playerData);
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
        for (var index = 0; index < m_PlayerDataNetworkList.Count; index++)
        {
            if (m_PlayerDataNetworkList[index].Equals(pPlayerData))
            {
                return index;
            }
        }
        return -1; // Return -1 if no player data with the given client id is found
    }
}