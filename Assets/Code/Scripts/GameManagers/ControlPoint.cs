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

	public int CurrentTeam;
	public int LastTeamOnPoint;
	public float PointsPerSeconds = 1f;

	private readonly float m_ControlTimeRequired = 2f;
	private float m_ControlValue;
	private bool m_IsCapturing;

	private ProgressBar m_ProgressBar;

	private bool IsContested;
	
	public bool IsCaptured => !IsContested && ((CurrentTeam == 1 && m_ControlValue >= m_ControlTimeRequired) || (CurrentTeam == 2 && m_ControlValue <= -m_ControlTimeRequired));

	private void Awake()
	{
		m_ProgressBar = m_Document.rootVisualElement.Q<ProgressBar>("CapturePoint");
		m_ProgressBar.lowValue = 0;
		m_ProgressBar.highValue = m_ControlTimeRequired;
		m_ProgressBar.value = 0;
	}

	private void Update()
	{
		var playersInside = new List<PlayerData>(GetPlayersInside());

		if (playersInside.Count == 0)
		{
			NeutralPoint();
			return;
		}
		
		var teamInside = GetTeamInside(GetPlayersInside());
		if (teamInside == -1)
		{
			Contested();
			return;
		}

		CurrentTeam = teamInside;
		CapturePoint();
	}

	private void Contested()
	{
		IsContested = true;
	}

	private void NeutralPoint()
	{
		if (Mathf.Abs(m_ControlValue) > float.Epsilon)
			return;

		if (LastTeamOnPoint == 1)
		{
			m_ControlValue -= Time.deltaTime;
			if (m_ControlValue <= 0)
				m_ControlValue = 0;
		}
		else
		{
			m_ControlValue += Time.deltaTime;
			if (m_ControlValue >= 0)
				m_ControlValue = 0;
		}
		m_ProgressBar.value = Mathf.Abs(m_ControlValue);
	}

	private void CapturePoint()
	{
		IsContested = false;
		if (IsCaptured) return;

		m_ControlValue += CurrentTeam == 1 ? Time.deltaTime : -Time.deltaTime;

		if (IsCaptured)
		{
			m_ControlValue = m_ControlTimeRequired * Mathf.Sign(m_ControlValue);
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

		LastTeamOnPoint = CurrentTeam;
	}

	public List<PlayerData> GetPlayersInside()
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

		return playersInside;
	}

	public int GetTeamInside(List<PlayerData> playersInside)
	{
		if (playersInside.Count == 0)
			return -1;

		var teamInside = playersInside[0].IsTeamOne ? 1 : 2;

		foreach (var playerData in playersInside)
			if ((playerData.IsTeamOne && teamInside != 1) || (!playerData.IsTeamOne && teamInside != 2))
				return -1;

		return teamInside;
	}
}