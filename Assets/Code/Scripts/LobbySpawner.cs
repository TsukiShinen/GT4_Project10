using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DefaultNamespace
{
	public class LobbySpawner : MonoBehaviour
	{
		[SerializeField] private Transform m_PlayerPrefab;
		private Dictionary<ulong, GameObject> m_PlayerGameObjects = new ();

		private void Awake()
		{
			NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
			NetworkManager.Singleton.OnClientConnectedCallback += Network_OnClientConnectedCallback;
		}

		private void OnDestroy()
		{
			NetworkManager.Singleton.OnClientConnectedCallback -= Network_OnClientConnectedCallback;
		}

		private void Network_OnClientConnectedCallback(ulong pClientId)
		{
			SpawnPlayerServerRpc(pClientId);
		}

		public GameObject GetPlayerGameObject(ulong pId)
		{
			return m_PlayerGameObjects.TryGetValue(pId, out var playerGameObject) ? playerGameObject : null;
		}

		private void SceneManager_OnLoadEventCompleted(string pSceneName, LoadSceneMode pLoadMode, List<ulong> pClientsCompleted, List<ulong> pClientTimouts)
		{
			foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
				SpawnPlayerServerRpc(clientId);
		}

		[ServerRpc(RequireOwnership = false)]
		private void SpawnPlayerServerRpc(ulong pClientId, ServerRpcParams pServerRpcParams = default)
		{
			var player = Instantiate(m_PlayerPrefab);
			player.GetComponent<NetworkObject>().SpawnAsPlayerObject(pClientId, true);

			m_PlayerGameObjects.Add(pClientId, player.gameObject);
		}
	}
}