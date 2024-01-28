using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

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
	[SerializeField] private UIDocument m_Document;
	[SerializeField] private BoxCollider m_BoxCollider;

	public ControlPointState CurrentState = ControlPointState.Neutral;
	public int CurrentTeam;
	public float PointsPerSeconds = 1f;

	private readonly float m_ControlTimeRequired = 2f;
	private float m_ControlValue;
	private bool m_IsCapturing;

	private ProgressBar m_ProgressBar;

	private void Awake()
	{
		m_ProgressBar = m_Document.rootVisualElement.Q<ProgressBar>("CapturePoint");
		m_ProgressBar.lowValue = 0;
		m_ProgressBar.highValue = m_ControlTimeRequired;
		m_ProgressBar.value = 0;
	}

	private void Update()
	{
		CapturePoint();
	}

	private void CapturePoint()
	{
		if (CurrentState != ControlPointState.Capturing) return;

		m_ControlValue += CurrentTeam == 1 ? Time.deltaTime : -Time.deltaTime;

		if (Mathf.Abs(m_ControlValue) >= m_ControlTimeRequired)
		{
			m_ControlValue = m_ControlTimeRequired * Mathf.Sign(m_ControlValue);
			CurrentState = ControlPointState.Captured;
		}

		m_ProgressBar.value = Mathf.Abs(m_ControlValue);
		if (Mathf.Sign(m_ControlValue) > 0 && m_ProgressBar.ClassListContains("teamtwo"))
		{
			m_ProgressBar.RemoveFromClassList("teamtwo");
			m_ProgressBar.AddToClassList("teamone");
		}
		else if (Mathf.Sign(m_ControlValue) < 0 && m_ProgressBar.ClassListContains("teamone"))
		{
			m_ProgressBar.RemoveFromClassList("teamone");
			m_ProgressBar.AddToClassList("teamtwo");
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!other.TryGetComponent(out NetworkObject networkObject)) return;

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

	private void OnTriggerExit(Collider other)
	{
		if (!other.TryGetComponent(out NetworkObject networkObject)) return;

		var clientId = networkObject.OwnerClientId;
		var playerData = MultiplayerManager.Instance.FindPlayerData(clientId);

		var playersInside = new List<PlayerData>(GetPlayersInside());
		playersInside.Remove(playerData);

		var sameTeamInside = playersInside.Exists(playerInside =>
			(playerInside.IsTeamOne && playerData.IsTeamOne) ||
			(!playerInside.IsTeamOne && !playerData.IsTeamOne));

		if (sameTeamInside) return;

		StopCapture();
		CurrentState = ControlPointState.Neutral;
		CurrentTeam = 0;
	}

	private void StartCapture(int team)
	{
		if (m_IsCapturing) return;

		m_IsCapturing = true;
		CurrentState = ControlPointState.Capturing;
		CurrentTeam = team;
	}

	private void StopCapture()
	{
		CurrentState = ControlPointState.Contested;
		m_IsCapturing = false;
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
			if (!collider.TryGetComponent(out NetworkObject networkObject)) continue;

			var clientId = networkObject.OwnerClientId;
			var player = MultiplayerManager.Instance.FindPlayerData(clientId);
			if (!player.Equals(default))
				playersInside.Add(player);
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