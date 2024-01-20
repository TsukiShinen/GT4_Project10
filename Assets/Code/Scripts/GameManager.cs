using System.Collections.Generic;
using Network;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
	[SerializeField] private Transform m_PlayerPrefab;

	[SerializeField] private List<Transform> m_SpawnsTeam1;
	[SerializeField] private List<Transform> m_SpawnsTeam2;
	
	public override void OnNetworkSpawn()
	{
		if (!IsServer)
			return;

		// TODO : destroy lobby
		NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
	}

	private void SceneManager_OnLoadEventCompleted(string pSceneName, LoadSceneMode pLoadMode, List<ulong> pClientsCompleted, List<ulong> pClientTimouts)
	{
		var countTeam1 = 0;
		var countTeam2 = 0;
		foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
		{
			var playerData =
				MultiplayerManager.Instance.GetPlayerDataByIndex(
					MultiplayerManager.Instance.FindPlayerDataIndex(clientId));
			var player = Instantiate(m_PlayerPrefab);
			if (m_SpawnsTeam1 != null)
			{
				if (MultiplayerManager.Instance.GameModeConfig.HasTeams)
					player.transform.position = playerData.IsTeamOne ? m_SpawnsTeam1[countTeam1++].position : m_SpawnsTeam2[countTeam2++].position;
				else
					player.transform.position = m_SpawnsTeam1[countTeam1++].position;
			}
			player.GetComponent<NetworkObject>().SpawnWithOwnership(clientId, true);
		}
	}

}