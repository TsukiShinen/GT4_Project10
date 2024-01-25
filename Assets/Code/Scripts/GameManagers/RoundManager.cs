using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace GameManagers
{
	public class RoundManager : NetworkBehaviour
	{
		[SerializeField] private float m_TimeBetweenRound = 3;
		[SerializeField] private int m_ScoreToWin = 3;

		public NetworkVariable<int> ScoreTeam1;
		public NetworkVariable<int> ScoreTeam2;
		
		public EventHandler<RoundEventArgs> OnRoundEnded;
		public EventHandler OnRoundStarting;
		public EventHandler OnRoundStarted;
		public EventHandler<RoundEventArgs> OnEndMatch;

		public Action<int> OnScoreTeam1Changed;
		public Action<int> OnScoreTeam2Changed;
		
		private int m_MaxRounds => m_ScoreToWin * 2 - 1;
		private int m_CurrentRound = 1;
		
		public class RoundEventArgs : EventArgs
		{
			public bool IsTeamOneWin;
		}

		private void Awake()
		{
			ScoreTeam1 = new NetworkVariable<int>();
			ScoreTeam2 = new NetworkVariable<int>();
			ScoreTeam1.OnValueChanged += (value, newValue) =>
			{
				OnScoreTeam1Changed?.Invoke(newValue);
				
			};
			ScoreTeam2.OnValueChanged += (value, newValue) =>
			{
				OnScoreTeam2Changed?.Invoke(newValue);
				
			};

		}

		public void Server_EndRound(bool pIsTeamOneWin)
		{
			if (!NetworkManager.IsServer)
				return;

			ScoreTeam1.Value += pIsTeamOneWin ? 1 : 0;
			ScoreTeam2.Value += pIsTeamOneWin ? 0 : 1;
			
			OnRoundEnded?.Invoke(this, new RoundEventArgs { IsTeamOneWin = pIsTeamOneWin });
        
			if (ScoreTeam1.Value == m_ScoreToWin || ScoreTeam2.Value == m_ScoreToWin)
				OnEndMatch?.Invoke(this, new RoundEventArgs { IsTeamOneWin = pIsTeamOneWin });
			else
				StartCoroutine(StartNextRound());
		}

		private IEnumerator StartNextRound()
		{
			OnRoundStarting?.Invoke(this, EventArgs.Empty);
			yield return new WaitForSeconds(m_TimeBetweenRound);
			OnRoundStarted?.Invoke(this, EventArgs.Empty);
		}
	}
}