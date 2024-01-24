using System;
using Network;
using System.Collections.Generic;
using System.Linq;
using GameManagers;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
	Playing,
	RoundStart,
	RoundEnd
}

public abstract class GameManager : NetworkBehaviour
{
	protected Dictionary<ulong, Transform> m_PlayersGameObjects = new ();

	[SerializeField] protected SpawnManager m_SpawnManager;
	
    public static GameManager Instance { get; private set; }
    protected virtual void Awake()
	{
		if (Instance)
            Destroy(Instance);

		Instance = this;
	}

	private void OnEnable()
	{
		m_SpawnManager.OnPlayerRespawn += ResetPlayerGameObject_OnPlayerSpawn;
	}

	private void OnDisable()
	{
		m_SpawnManager.OnPlayerRespawn -= ResetPlayerGameObject_OnPlayerSpawn;
	}

	private void ResetPlayerGameObject_OnPlayerSpawn(ulong pClientId)
	{
		SetGameObject_ServerRpc(pClientId, true);
		SetCamera_ServerRpc(pClientId, pClientId);
	}

    private void Start()
    {
        LobbyManager.Instance.SetLobbyNull();
    }

    public override void OnNetworkSpawn()
    {
	    if (!IsServer)
		    return;
	    
		NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
	}

    protected virtual void SceneManager_OnLoadEventCompleted(string pSceneName, LoadSceneMode pLoadMode, List<ulong> pClientsCompleted, List<ulong> pClientTimouts)
    {
	    // TODO : Really needed?
    }

    protected void Server_RespawnPlayers()
    {
	    foreach (var clientId in m_PlayersGameObjects.Keys)
		    if (m_PlayersGameObjects.TryGetValue(clientId, out var playerGameObject)) 
				m_SpawnManager.RespawnPlayer(playerGameObject, clientId);
    }

    public PlayerData FindPlayerData(Transform pPlayerGameObject)
    {
        foreach(var player in m_PlayersGameObjects)
			if (pPlayerGameObject == player.Value)
				return MultiplayerManager.Instance.FindPlayerData(player.Key);

		return default;
    }

    public Transform FindPlayer(ulong pClientId)
    {
	    return m_PlayersGameObjects.GetValueOrDefault(pClientId);
    }

    [ServerRpc]
    public void SetCamera_ServerRpc(ulong pClientId, ulong pTargetId)
    {
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
        m_PlayersGameObjects[pClientId].GetComponent<SetGameObject>().SetGameObject_ClientRpc(pIsActive);
    }
}