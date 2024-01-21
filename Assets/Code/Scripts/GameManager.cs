using System.Collections.Generic;
using System.Linq;
using Network;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Codice.Client.Commands.WkTree.WorkspaceTreeNode;

public class GameManager : NetworkBehaviour
{
	public static GameManager Instance { get; private set; }

	[SerializeField] private Transform m_PlayerPrefab;

	[SerializeField] private List<Transform> m_SpawnsTeam1;
	[SerializeField] private List<Transform> m_SpawnsTeam2;
	[SerializeField] private Dictionary<PlayerData, GameObject> m_PlayersGameObjects = new Dictionary<PlayerData, GameObject>();

	private void Awake()
	{
		if (Instance != null)
		{
            Destroy(this);
            return;
        }

		Instance = this;
	}

	public override void OnNetworkSpawn()
	{
		if (!IsServer)
			return;

		// TODO : destroy lobby
		NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
	}

	public void RespawnPlayer(PlayerData pPlayerData)
	{
		if(m_PlayersGameObjects.TryGetValue(pPlayerData, out GameObject player))
		{
            if (MultiplayerManager.Instance.GameModeConfig.HasTeams)
                player.GetComponent<PlayerHealth>().RespawnPlayerClientRpc(pPlayerData.IsTeamOne ? m_SpawnsTeam1[0].position : m_SpawnsTeam2[0].position);
			else
                player.GetComponent<PlayerHealth>().RespawnPlayerClientRpc(m_SpawnsTeam1[0].position);
        }
	}

    public void SetPlayerData(int pIndex, PlayerData pNewPlayerData)
    {
		var gameObject = m_PlayersGameObjects.ElementAt(pIndex).Value;
		m_PlayersGameObjects.Remove(m_PlayersGameObjects.ElementAt(pIndex).Key);
		m_PlayersGameObjects.Add(pNewPlayerData, gameObject);
    }

    public PlayerData FindPlayerData(GameObject pPlayerGameObjects)
    {
        foreach(var player in m_PlayersGameObjects)
		{
			if (pPlayerGameObjects == player.Value)
				return player.Key;
		}

		return default;
    }

    public GameObject FindPlayerGameObject(PlayerData pPlayerDatas)
    {
        if(m_PlayersGameObjects.TryGetValue(pPlayerDatas, out GameObject gameObject))
		{
			return gameObject;

        }

        return null;
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
			m_PlayersGameObjects.Add(playerData, player.gameObject);
        }
	}

}