using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
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

    protected virtual void Update()
    {

    }

    public override void OnNetworkSpawn()
    {
	    if (!IsServer)
		    return;
		NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
	}

    protected virtual void DetermineSpawnType()
	{
		if(m_SpawnType == SpawnType.Null)
            Debug.LogError("No spawn zone or spawn points defined.");
    }

    protected virtual void SceneManager_OnLoadEventCompleted(string pSceneName, LoadSceneMode pLoadMode, List<ulong> pClientsCompleted, List<ulong> pClientTimouts)
    {
    }

    public virtual void RespawnPlayer(PlayerData pPlayerData)
	{
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

    [ServerRpc]
    public void SetCamera_ServerRpc(ulong pClientId, ulong pTargetId)
    {
	    Debug.Log("SetCamera_ServerRpc");
	    m_PlayersGameObjects[MultiplayerManager.Instance.FindPlayerData(pTargetId)].GetComponent<SetPlayerCamera>().Set_ClientRpc(new ClientRpcParams
	    {
		    Send = new ClientRpcSendParams
		    {
			    TargetClientIds = new List<ulong> { pClientId }
		    }
	    });
    }
}