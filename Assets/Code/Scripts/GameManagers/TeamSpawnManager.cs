using System;
using UnityEngine;

namespace GameManagers
{
	public class TeamSpawnManager : SpawnManager
	{
		[Header("Spawn Points")]
		[SerializeField] private BoxCollider m_SpawnTeam1;
		[SerializeField] private BoxCollider m_SpawnTeam2;
		
		private const int k_MaxAttempts = 10;
		
		protected override Tuple<Vector3, Quaternion> GetSpawnPoint(PlayerData pPlayerData)
		{
			var spawn = pPlayerData.IsTeamOne ? m_SpawnTeam1 : m_SpawnTeam2;
			if (spawn)
			{
				for (var i = 0; i < k_MaxAttempts; i++)
				{
					var randomPoint = new Vector3(
						UnityEngine.Random.Range(spawn.bounds.min.x, spawn.bounds.max.x),
						spawn.bounds.min.y,
						UnityEngine.Random.Range(spawn.bounds.min.z, spawn.bounds.max.z)
					);

					var isLocationValid = IsSpawnLocationValid(randomPoint);

					if (!isLocationValid) continue;
                
					var rotation = pPlayerData.IsTeamOne
						? m_SpawnTeam1.transform.rotation
						: m_SpawnTeam2.transform.rotation;
					return new Tuple<Vector3, Quaternion>( randomPoint, rotation ) ;
				}

				Debug.LogError("Failed to find a valid spawn location after multiple attempts.");
			}

			Debug.LogError("No spawn zone defined.");
			return null;
		}

		public override void ResetAvailableSpawnPoints() { }
    }
}