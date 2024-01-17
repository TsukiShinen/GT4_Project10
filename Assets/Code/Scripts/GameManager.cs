using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
	[SerializeField] private Transform m_PlayerPrefab;
	
	public override void OnNetworkSpawn()
	{
		if (!IsServer)
			return;

		NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
	}

	private void SceneManager_OnLoadEventCompleted(string pSceneName, LoadSceneMode pLoadMode, List<ulong> pClientsCompleted, List<ulong> pClientTimouts)
	{
		foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
		{
			var player = Instantiate(m_PlayerPrefab);
			player.GetComponent<NetworkObject>().SpawnWithOwnership(clientId, true);
		}
	}

}