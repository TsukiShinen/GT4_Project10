using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum ControlPointState
{
	Neutral,
	Capturing,
	Contested,
	Captured,
	Controlled
}

public class ControlPoint : NetworkBehaviour
{
	[SerializeField] private BoxCollider m_BoxCollider;

	public ControlPointState CurrentState = ControlPointState.Neutral;
	public int CurrentTeam;
	public float PointsPerTick = 0.00001f;

	private readonly float m_ControlTimeRequired = 2f;
	private bool m_IsCapturing;

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.DrawWireCube(transform.position + m_BoxCollider.center, m_BoxCollider.size);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.TryGetComponent(out NetworkObject networkObject))
		{
			var clientId = networkObject.OwnerClientId;
			var playerData = MultiplayerManager.Instance.FindPlayerData(clientId);

			var playerTeam = playerData.IsTeamOne ? 1 : 2;

			switch (CurrentState)
			{
				case ControlPointState.Neutral:
					StartCapture(playerTeam);
					break;

				case ControlPointState.Capturing:
					var teamInside = GetTeamInside(GetPlayersInside());
					if (teamInside != -1 && teamInside != CurrentTeam)
						StopCapture();
					break;

				case ControlPointState.Captured:
					break;

				case ControlPointState.Contested:
					var teamInsideContested = GetTeamInside(GetPlayersInside());
					if (teamInsideContested != -1)
					{
						if (teamInsideContested == CurrentTeam)
						{
							CurrentState = ControlPointState.Captured;
							StartCapture(CurrentTeam);
						}
						else
						{
							StopCapture();
							StartCapture(teamInsideContested);
						}
					}

					break;

				case ControlPointState.Controlled:
					break;
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.TryGetComponent(out NetworkObject networkObject))
		{
			var clientId = networkObject.OwnerClientId;
			var playerData = MultiplayerManager.Instance.FindPlayerData(clientId);

			var playersInside = new List<PlayerData>(GetPlayersInside());
			playersInside.Remove(playerData);

			var sameTeamInside = playersInside.Exists(playerInside =>
				(playerInside.IsTeamOne && playerData.IsTeamOne) ||
				(!playerInside.IsTeamOne && !playerData.IsTeamOne));

			if (!sameTeamInside)
			{
				StopCapture();
				CurrentState = ControlPointState.Neutral;
				CurrentTeam = 0;
			}
		}
	}

	private void StartCapture(int team)
	{
		if (m_IsCapturing) return;

		m_IsCapturing = true;
		CurrentState = ControlPointState.Capturing;
		CurrentTeam = team;
		StartCoroutine(CaptureRoutine());
	}

	private IEnumerator CaptureRoutine()
	{
		yield return new WaitForSeconds(m_ControlTimeRequired);

		CurrentState = ControlPointState.Captured;
	}

	private void StopCapture()
	{
		CurrentState = ControlPointState.Contested;
		m_IsCapturing = false;
		StopAllCoroutines();
	}

	public PlayerData[] GetPlayersInside()
	{
		var colliders = Physics.OverlapBox(transform.TransformPoint(m_BoxCollider.center), m_BoxCollider.size / 2f,
			transform.rotation);
		var playersInside = new List<PlayerData>();

		foreach (var collider in colliders)
		{
			if (collider.transform == transform)
				continue;
			if (collider.TryGetComponent(out NetworkObject networkObject))
			{
				Debug.Log(collider.name);
				var clientId = networkObject.OwnerClientId;
				var player = MultiplayerManager.Instance.FindPlayerData(clientId);
				if (!player.Equals(default))
					playersInside.Add(player);
			}
		}

		return playersInside.ToArray();
	}

	public int GetTeamInside(PlayerData[] playersInside)
	{
		if (playersInside.Length == 0)
			return -1;

		var teamInside = playersInside[0].IsTeamOne ? 1 : 2;

		foreach (var playerData in playersInside)
			if ((playerData.IsTeamOne && teamInside != 1) || (!playerData.IsTeamOne && teamInside != 2))
				return -1;

		return teamInside;
	}
}