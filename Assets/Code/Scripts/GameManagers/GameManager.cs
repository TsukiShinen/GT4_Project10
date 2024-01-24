using Network;
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

	protected Dictionary<ulong, GameObject> m_PlayersGameObjects = new ();

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

    public virtual void RespawnPlayer(ulong pCliendId)
	{
	}

    public void SetPlayerData(int pIndex, ulong pClientId)
    {
		var gameObject = m_PlayersGameObjects.ElementAt(pIndex).Value;
		m_PlayersGameObjects.Remove(m_PlayersGameObjects.ElementAt(pIndex).Key);
		m_PlayersGameObjects.Add(pClientId, gameObject);
    }

    public PlayerData FindPlayerData(GameObject pPlayerGameObject)
    {
        foreach(var player in m_PlayersGameObjects)
			if (pPlayerGameObject == player.Value)
				return MultiplayerManager.Instance.FindPlayerData(player.Key);

		return default;
    }

    public GameObject FindPlayerGameObject(ulong pClientId)
    {
	    return m_PlayersGameObjects.GetValueOrDefault(pClientId);
    }

    [ServerRpc]
    public void SetCamera_ServerRpc(ulong pClientId, ulong pTargetId)
    {
	    Debug.Log("SetCamera_ServerRpc");
	    m_PlayersGameObjects[pTargetId].GetComponent<SetPlayerCamera>().Set_ClientRpc(new ClientRpcParams
	    {
		    Send = new ClientRpcSendParams
		    {
			    TargetClientIds = new List<ulong> { pClientId }
		    }
	    });
    }

    [ServerRpc]
    public void SetGameObject_ServerRpc(ulong pClientId, bool pIsActive)
    {
        Debug.Log("SetGameObject_ServerRpc");
        m_PlayersGameObjects[pClientId].GetComponent<SetGameObject>().SetGameObject_ClientRpc(pIsActive);
    }
}