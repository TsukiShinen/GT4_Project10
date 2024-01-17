using System.Linq;
using Unity.Netcode;
using UnityEngine;

public enum SpawnType
{
	Zone,
	Points
}

public class PlayerSpawnManager : NetworkBehaviour
{
	private Transform[] m_spawnPoints;
	private SpawnType m_spawnType = SpawnType.Zone;
	private BoxCollider m_spawnZone;

	public void Start()
	{
		DetermineSpawnType();

		if (IsServer)
			if (IsLocalPlayer)
				SpawnPlayer();
	}

	private void DetermineSpawnType()
	{
		m_spawnZone = GameObject.FindGameObjectWithTag("ZoneSpawn")?.GetComponent<BoxCollider>();
		m_spawnPoints = GameObject.FindGameObjectsWithTag("PointsSpawn")?.Select(obj => obj.transform)?.ToArray();

		if (m_spawnZone != null)
			m_spawnType = SpawnType.Zone;
		else if (m_spawnPoints != null && m_spawnPoints.Length > 0)
			m_spawnType = SpawnType.Points;
		else
			Debug.LogError("No spawn zone or spawn points defined.");
	}

	private void SpawnPlayer()
	{
		var spawnPoint = Vector3.zero;

		if (m_spawnType == SpawnType.Zone)
			spawnPoint = GetRandomPointInSpawnZone();
		else if (m_spawnType == SpawnType.Points) spawnPoint = GetRandomSpawnPoint();

		if (NetworkManager.Singleton.ConnectedClients.ContainsKey(NetworkManager.Singleton.LocalClientId))
		{
			var localPlayerObject = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId]
				.PlayerObject?.gameObject;

			if (localPlayerObject != null)
			{
				Debug.Log(spawnPoint);
				localPlayerObject.transform.position = spawnPoint;
			}
			else
			{
				Debug.LogError("GameObject of local player not found.");
			}
		}
		else
		{
			Debug.LogError("Local client ID not found in the list of connected clients.");
		}
	}

	private Vector3 GetRandomPointInSpawnZone()
	{
		if (m_spawnZone != null)
		{
			var randomPoint = new Vector3(
				Random.Range(m_spawnZone.bounds.min.x, m_spawnZone.bounds.max.x),
				m_spawnZone.bounds.min.y,
				Random.Range(m_spawnZone.bounds.min.z, m_spawnZone.bounds.max.z)
			);

			return randomPoint;
		}

		Debug.LogError("No spawn zone defined.");
		return Vector3.zero;
	}

	private Vector3 GetRandomSpawnPoint()
	{
		if (m_spawnPoints != null && m_spawnPoints.Length > 0)
		{
			var randomSpawnPoint = m_spawnPoints[Random.Range(0, m_spawnPoints.Length)];
			return randomSpawnPoint.position;
		}

		Debug.LogError("No spawn point defined.");
		return Vector3.zero;
	}
}