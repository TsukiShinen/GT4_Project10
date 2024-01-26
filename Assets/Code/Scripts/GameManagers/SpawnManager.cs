using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace GameManagers
{
	public abstract class SpawnManager : NetworkBehaviour
	{
		[SerializeField] protected Transform m_PlayerPrefab;
		
		[Header("Player Parameters")]
		[SerializeField] protected float m_PlayerHeight = 2f;
		[SerializeField] protected float m_PlayerRadius = 0.5f;
		[SerializeField] private string m_PlayerTag = "Player";

		public Action<ulong> OnPlayerRespawn;
		
		private const float k_RayLength = 1f;
		private readonly Vector3[] m_RayDirections =
		{
			Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.forward + Vector3.left,
			Vector3.forward + Vector3.right, Vector3.back + Vector3.left, Vector3.back + Vector3.right
		};

		public Transform Server_SpawnPlayer(ulong pClientId)
		{
			if (!NetworkManager.IsServer)
				return null;
            
			var playerData = MultiplayerManager.Instance.FindPlayerData(pClientId);

			var player = Instantiate(m_PlayerPrefab);

			var (position, rotation) = GetSpawnPoint(playerData);
			player.transform.position = position;
			player.transform.rotation = rotation;
			
			player.GetComponent<NetworkObject>().SpawnWithOwnership(pClientId, true);
			
			return player;
		}

		public virtual void Server_RespawnPlayer(Transform pPlayerGameObject, ulong pClientId)
		{
			if (!NetworkManager.IsServer)
				return;
		
			var pPlayerData = MultiplayerManager.Instance.FindPlayerData(pClientId);

			// Reset Health TODO : Better
			pPlayerData.PlayerHealth = pPlayerData.PlayerMaxHealth;
			MultiplayerManager.Instance.GetPlayerDatas()[MultiplayerManager.Instance.FindPlayerDataIndex(pClientId)] =
				pPlayerData;

			// Reset position Player TODO : Not from PlayerHealth
			var (position, rotation) = GetSpawnPoint(pPlayerData);
			pPlayerGameObject.GetComponent<PlayerHealth>().RespawnPlayerClientRpc(position, rotation);
			
			OnPlayerRespawn?.Invoke(pClientId);
		}

		protected bool IsSpawnLocationValid(Vector3 spawnLocation)
		{
			// TODO : OverlapBox Non Alloc
			var colliders = Physics.OverlapBox(spawnLocation + Vector3.up * (m_PlayerHeight / 2f),
				new Vector3(m_PlayerRadius, m_PlayerHeight / 2f, m_PlayerRadius));

			// Collide with a player
			if (colliders.Any(c => c.CompareTag(m_PlayerTag)))
				return false;

			foreach (var direction in m_RayDirections)
				if (Physics.Raycast(spawnLocation, direction, out var hit, k_RayLength))
					return false;

			return true;
		}

		protected abstract Tuple<Vector3, Quaternion> GetSpawnPoint(PlayerData pPlayerData);
		
		public abstract void ResetAvailableSpawnPoints();

    }
}