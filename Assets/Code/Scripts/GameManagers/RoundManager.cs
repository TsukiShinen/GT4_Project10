using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameManagers
{
	public class RoundManager : NetworkBehaviour
	{
		[SerializeField] private float m_TimeBetweenRound = 3;
		[SerializeField] private int m_ScoreToWin = 3;

		private int m_CurrentRound = 1;
		public EventHandler<RoundEventArgs> OnEndMatch;

		public EventHandler<RoundEventArgs> OnRoundEnded;
		public EventHandler OnRoundStarted;
		public EventHandler OnRoundStarting;

		public Action<int> OnScoreTeam1Changed;
		public Action<int> OnScoreTeam2Changed;

		private int m_MaxRounds => m_ScoreToWin * 2 - 1;

		private void Start()
		{
			MultiplayerManager.Instance.ScoreTeam1.OnValueChanged += (value, newValue) => { OnScoreTeam1Changed?.Invoke(newValue); };
			MultiplayerManager.Instance.ScoreTeam2.OnValueChanged += (value, newValue) => { OnScoreTeam2Changed?.Invoke(newValue); };
		}

		public void Server_EndRound(bool pIsTeamOneWin)
		{
			if (!NetworkManager.IsServer)
				return;

			MultiplayerManager.Instance.ScoreTeam1.Value += pIsTeamOneWin ? 1 : 0;
			MultiplayerManager.Instance.ScoreTeam2.Value += pIsTeamOneWin ? 0 : 1;

			OnRoundEnded?.Invoke(this, new RoundEventArgs { IsTeamOneWin = pIsTeamOneWin });

			if (MultiplayerManager.Instance.ScoreTeam1.Value == m_ScoreToWin || MultiplayerManager.Instance.ScoreTeam2.Value == m_ScoreToWin)
				OnEndMatch?.Invoke(this, new RoundEventArgs { IsTeamOneWin = pIsTeamOneWin });
			else
				StartCoroutine(StartNextRound());
		}

		private IEnumerator StartNextRound()
		{
			//OnRoundStarting?.Invoke(this, EventArgs.Empty);
			yield return new WaitForSeconds(m_TimeBetweenRound);
			ReloadScene();
			//OnRoundStarted?.Invoke(this, EventArgs.Empty);
		}

		public class RoundEventArgs : EventArgs
		{
			public bool IsTeamOneWin;
		}

        private void ReloadScene()
        {
			if (!NetworkManager.IsServer)
				return;
	
			NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }

    }
}