using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyReady : NetworkBehaviour
{
	[SerializeField] private string m_GameScene;
	
	private Dictionary<ulong, bool> m_PlayerReadyDictionary;

	public static LobbyReady Instance;
	private void Awake()
	{
		if (Instance != null)
		{
			Destroy(this);
			return;
		}

		Instance = this;
		
		m_PlayerReadyDictionary = new Dictionary<ulong, bool>();
	}

	public void SetPlayerReady()
	{
		SetPlayerReadyServerRpc();
	}

	[ServerRpc(RequireOwnership = false)]
	private void SetPlayerReadyServerRpc(ServerRpcParams pServerRpcParams = default)
	{
		if (!m_PlayerReadyDictionary.TryAdd(pServerRpcParams.Receive.SenderClientId, true))
			m_PlayerReadyDictionary[pServerRpcParams.Receive.SenderClientId] = !m_PlayerReadyDictionary[pServerRpcParams.Receive.SenderClientId];

		var allClientReady = NetworkManager.Singleton.ConnectedClientsIds.All(clientId => m_PlayerReadyDictionary.ContainsKey(clientId) && m_PlayerReadyDictionary[clientId]);
		
		if (!allClientReady)
			return;

		NetworkManager.Singleton.SceneManager.LoadScene(m_GameScene, LoadSceneMode.Single);
	}
}