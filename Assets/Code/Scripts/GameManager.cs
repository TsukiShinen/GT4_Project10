using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
	public const int k_MaxPlayerAmount = 4;
	private const string k_PlayerPref_PlayerName = "PlayerName";
	
	private NetworkList<PlayerData> m_PlayerDataNetworkList;
	private string m_PlayerName;

	public string PlayerName
	{
		get => m_PlayerName;
		set
		{
			m_PlayerName = value;
			PlayerPrefs.SetString(k_PlayerPref_PlayerName, m_PlayerName);
		}
	}
	
	public static GameManager Instance { get; private set; }

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
	}

	public void StartHost()
	{
		NetworkManager.Singleton.OnClientConnectedCallback += Network_OnClientConnectedCallback;
		NetworkManager.Singleton.StartHost();
	}

	private void Network_OnClientConnectedCallback(ulong pClientId)
	{
		m_PlayerDataNetworkList.Add(new PlayerData
		{
			ClientId = pClientId,
		});
		SetPlayerNameServerRpc(m_PlayerName);
	}

	public void StartClient()
	{
		NetworkManager.Singleton.OnClientConnectedCallback += Network_Client_OnClientConnectedCallback;
		NetworkManager.Singleton.StartClient();
	}

	private void Network_Client_OnClientConnectedCallback(ulong pClientId)
	{
		SetPlayerNameServerRpc(m_PlayerName);
	}

	[ServerRpc(RequireOwnership = false)]
	private void SetPlayerNameServerRpc(string pPlayerName, ServerRpcParams pServerRpcParams = default)
	{
		var playerDataIndex = GetPlayerDataIndexFromClientId(pServerRpcParams.Receive.SenderClientId);
		var playerData = m_PlayerDataNetworkList[playerDataIndex];

		playerData.PlayerName = pPlayerName;
		Debug.Log($"[ServerRpc] Player at index {playerDataIndex} change name to {pPlayerName}");
		
		m_PlayerDataNetworkList[playerDataIndex] = playerData;
	}

	private int GetPlayerDataIndexFromClientId(ulong pClientId)
	{
		// TODO : GetPLayerDataIndexFromClientId
		return 0;
	}
}