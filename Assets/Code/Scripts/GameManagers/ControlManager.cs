using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class ControlPointGameManager : GameManager
{
	[SerializeField] private ControlPoint m_ControlPoint;
	private readonly int m_PointsToWin = 100;
	private NetworkVariable<float> m_ScoreTeam1;
	private NetworkVariable<float> m_ScoreTeam2;

	protected override void Awake()
	{
		base.Awake();

		m_ScoreTeam1 = new NetworkVariable<float>();
		m_ScoreTeam2 = new NetworkVariable<float>();
		
		m_Root.Q<TextElement>("Team1Score").text = "0%";
		m_ScoreTeam1.OnValueChanged += (v, value) => { m_Root.Q<TextElement>("Team1Score").text = Mathf.Round(100 * value / m_PointsToWin) + "%"; };
		m_Root.Q<TextElement>("Team2Score").text = "0%";
		m_ScoreTeam2.OnValueChanged += (v, value) => { m_Root.Q<TextElement>("Team2Score").text = Mathf.Round(100 * value / m_PointsToWin) + "%"; };
	}

	protected void Update()
	{
		if (!NetworkManager.IsServer)
			return;
		Server_UpdateScores();
	}

	protected override void SceneManager_OnLoadEventCompleted(string pSceneName, LoadSceneMode pLoadMode,
		List<ulong> pClientsCompleted, List<ulong> pClientTimouts)
	{
		foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
		{
			var player = m_SpawnManager.Server_SpawnPlayer(clientId);
			m_PlayersGameObjects.Add(clientId, player);
		}

		base.SceneManager_OnLoadEventCompleted(pSceneName, pLoadMode, pClientsCompleted, pClientTimouts);
	}

	private void Server_UpdateScores()
	{
		switch (m_ControlPoint.CurrentState)
		{
			case ControlPointState.Captured:
				var pointsMultiplier = m_ControlPoint.GetPlayersInside().Length;
				if (m_ControlPoint.CurrentTeam == 1)
					m_ScoreTeam1.Value += m_ControlPoint.PointsPerSeconds * Time.deltaTime * pointsMultiplier;
				else if (m_ControlPoint.CurrentTeam == 2)
					m_ScoreTeam2.Value += m_ControlPoint.PointsPerSeconds * Time.deltaTime * pointsMultiplier;

				if (m_ScoreTeam1.Value >= m_PointsToWin || m_ScoreTeam2.Value >= m_PointsToWin)
				{
					m_ControlPoint.CurrentState = ControlPointState.Controlled;
					EndGame_ClientRpc(m_ScoreTeam1.Value >= m_PointsToWin);
				}

				break;
		}
	}

	[ClientRpc]
	private void EndGame_ClientRpc(bool pIsTeamOnWin)
	{
		SetVictoryScreen(pIsTeamOnWin);
	}

	private void SetVictoryScreen(bool pIsTeamOneWin)
	{
		var playerData =
			MultiplayerManager.Instance.GetPlayerDataByIndex(
				MultiplayerManager.Instance.FindPlayerDataIndex(NetworkManager.Singleton.LocalClientId));
		var victory = (playerData.IsTeamOne && pIsTeamOneWin) || (!playerData.IsTeamOne && !pIsTeamOneWin);

		m_WinScreen.rootVisualElement.Q<TextElement>("Win").style.display = victory ? DisplayStyle.Flex : DisplayStyle.None;
		m_WinScreen.rootVisualElement.Q<TextElement>("Lose").style.display = victory ? DisplayStyle.None : DisplayStyle.Flex;
		m_WinScreen.rootVisualElement.style.display = DisplayStyle.Flex;
		
		
		UnityEngine.Cursor.lockState = CursorLockMode.None;
		UnityEngine.Cursor.visible = true;
	}
}