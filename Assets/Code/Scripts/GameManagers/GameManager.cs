using Network;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SpawnType
{
	Null,
    Zone,
    Points
}

public enum GameState
{
	Playing,
	RoundStart,
	RoundEnd
}

public class GameManager : NetworkBehaviour
{

	[SerializeField] protected Transform m_PlayerPrefab;

	protected Dictionary<PlayerData, GameObject> m_PlayersGameObjects = new Dictionary<PlayerData, GameObject>();

    protected SpawnType m_SpawnType = SpawnType.Null;

    public static GameManager Instance { get; private set; }
    protected virtual void Awake()
	{
		if (Instance)
            Destroy(Instance);

		Instance = this;
	}

    private void Start()
    {
        LobbyManager.Instance.SetLobbyNull();
    }

    protected virtual void Update()
    {

    }

    public override void OnNetworkSpawn()
	{
		if (!IsServer)
			return;

		// TODO : destroy lobby
		NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
	}

    protected virtual void DetermineSpawnType()
	{
		if(m_SpawnType == SpawnType.Null)
            Debug.LogError("No spawn zone or spawn points defined.");

        Debug.Log(m_SpawnType);

    }

    protected virtual void SceneManager_OnLoadEventCompleted(string pSceneName, LoadSceneMode pLoadMode, List<ulong> pClientsCompleted, List<ulong> pClientTimouts)
    {
        //var countTeam1 = 0;
        //var countTeam2 = 0;
        //foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        //{
        //var playerData =
        //MultiplayerManager.Instance.GetPlayerDataByIndex(
        //MultiplayerManager.Instance.FindPlayerDataIndex(clientId));
        //var player = Instantiate(m_PlayerPrefab);
        //if (m_SpawnsTeam1 != null)
        //{
        //if (MultiplayerManager.Instance.GameModeConfig.HasTeams)
        //player.transform.position = playerData.IsTeamOne ? m_SpawnsTeam1[countTeam1++].position : m_SpawnsTeam2[countTeam2++].position;
        //else
        //player.transform.position = m_SpawnsTeam1[countTeam1++].position;
        //}
        //player.GetComponent<NetworkObject>().SpawnWithOwnership(clientId, true);
        //m_PlayersGameObjects.Add(playerData, player.gameObject);
        //  }
    }

    public virtual void RespawnPlayer(PlayerData pPlayerData)
	{
		//if(m_PlayersGameObjects.TryGetValue(pPlayerData, out GameObject player))
		//{
            //if (MultiplayerManager.Instance.GameModeConfig.HasTeams)
                //player.GetComponent<PlayerHealth>().RespawnPlayerClientRpc(pPlayerData.IsTeamOne ? m_SpawnsTeam1[0].position : m_SpawnsTeam2[0].position);
			//else
                //player.GetComponent<PlayerHealth>().RespawnPlayerClientRpc(m_SpawnsTeam1[0].position);
        //}
	}

    public void SetPlayerData(int pIndex, PlayerData pNewPlayerData)
    {
		var gameObject = m_PlayersGameObjects.ElementAt(pIndex).Value;
		m_PlayersGameObjects.Remove(m_PlayersGameObjects.ElementAt(pIndex).Key);
		m_PlayersGameObjects.Add(pNewPlayerData, gameObject);
    }

    public PlayerData FindPlayerData(GameObject pPlayerGameObject)
    {
        foreach(var player in m_PlayersGameObjects)
			if (pPlayerGameObject == player.Value)
				return player.Key;

		return default;
    }

    public GameObject FindPlayerGameObject(PlayerData pPlayerData)
    {
	    return m_PlayersGameObjects.GetValueOrDefault(pPlayerData);
    }

}