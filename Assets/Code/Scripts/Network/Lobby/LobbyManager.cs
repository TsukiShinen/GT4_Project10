using System;
using System.Collections.Generic;
using ScriptableObjects;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Network
{
	public class LobbyManager : MonoBehaviour
	{
		[SerializeField] private LobbyInfo m_Info;

		public event EventHandler OnCreateLobbyStarted;
		public event EventHandler OnCreateLobbySucceed;
		public event EventHandler OnCreateLobbyFailed;
		public event EventHandler OnJoinLobbyStarted;
		public event EventHandler OnJoinLobbySucceed;
		public event EventHandler OnJoinLobbyFailed;
		public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
		public class OnLobbyListChangedEventArgs : EventArgs
		{
			public List<Lobby> LobbyList;
		}

		private Lobby m_JoinedLobby;
		public Lobby Lobby => m_JoinedLobby;
		private float m_HeartBeatTimer;

		private bool IsLobbyHost =>
			m_JoinedLobby != null && m_JoinedLobby.HostId == AuthenticationService.Instance.PlayerId;
		
		public static LobbyManager Instance { get; private set; }

		private void Awake()
		{
			if (Instance)
			{
				Destroy(this);
				return;
			}

			Instance = this;
			DontDestroyOnLoad(gameObject);

			InitializeUnityAuthentication();
		}

		private static async void InitializeUnityAuthentication()
		{
			if (UnityServices.State == ServicesInitializationState.Initialized) return;
			await UnityServices.InitializeAsync();

#if UNITY_EDITOR
			if (ParrelSync.ClonesManager.IsClone())
			{
				var customArgument = ParrelSync.ClonesManager.GetArgument();
				AuthenticationService.Instance.SwitchProfile($"Clone{customArgument}_Profile");
			}
#endif
			await AuthenticationService.Instance.SignInAnonymouslyAsync();
			Debug.Log($"<color=green>===== Player Connected =====</color>");
		}

		private void Update()
		{
			HandleHeartBeat();
		}

		private void HandleHeartBeat()
		{
			if (!IsLobbyHost)
				return;

			m_HeartBeatTimer -= Time.deltaTime;
			if (m_HeartBeatTimer > 0)
				return;

			m_HeartBeatTimer = 15;
			LobbyService.Instance.SendHeartbeatPingAsync(m_JoinedLobby.Id);
		}

		public async void ListLobbies()
		{
			try
			{
				var options = new QueryLobbiesOptions
				{
					Filters = new List<QueryFilter>
					{
						new (QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GE)
					}
				};
				var response = await LobbyService.Instance.QueryLobbiesAsync(options);
				OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs
				{
					LobbyList = response.Results,
				});
			}
			catch (LobbyServiceException e)
			{
				Debug.LogException(e);
			}
		}

		public async void CreateLobby(string pLobbyName, bool pIsPrivate = false)
		{
			Debug.Log($"<color=green>=== Lobby Creation</color>");
			OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty);
			try
			{
				m_JoinedLobby = await LobbyService.Instance.CreateLobbyAsync(pLobbyName, GameManager.k_MaxPlayerAmount, new CreateLobbyOptions()
				{
					IsPrivate = pIsPrivate,
				});
				m_Info.Name = m_JoinedLobby.Name;
				m_Info.Code = m_JoinedLobby.LobbyCode;
				Debug.Log($"Created {m_JoinedLobby.Name} / Code : {m_JoinedLobby.LobbyCode}");
				
				GameManager.Instance.StartHost();
				SceneManager.LoadSceneAsync("Lobby", LoadSceneMode.Additive);
				OnCreateLobbySucceed?.Invoke(this, EventArgs.Empty);
			}
			catch (LobbyServiceException e)
			{
				Debug.LogException(e);
				OnCreateLobbyFailed?.Invoke(this, EventArgs.Empty);
			}
			Debug.Log($"<color=green>==================</color>");
		}

		public async void JoinWithCode(string pLobbyCode)
		{
			Debug.Log($"<color=green>=== Joining Lobby</color>");
			OnJoinLobbyStarted?.Invoke(this, EventArgs.Empty);
			try
			{
				m_JoinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(pLobbyCode);
				Debug.Log($"Joined");
				
				m_Info.Name = m_JoinedLobby.Name;
				m_Info.Code = m_JoinedLobby.LobbyCode;
					
				GameManager.Instance.StartClient();
				OnJoinLobbySucceed?.Invoke(this, EventArgs.Empty);
			}
			catch (LobbyServiceException e)
			{
				Debug.LogException(e);
				OnJoinLobbyFailed?.Invoke(this, EventArgs.Empty);
			}
			Debug.Log($"<color=green>=================</color>");
		}

		public async void DeleteLobby()
		{
			if (m_JoinedLobby == null)
				return;
			
			try
			{
				await LobbyService.Instance.DeleteLobbyAsync(m_JoinedLobby.Id);
				m_JoinedLobby = null;
			}
			catch (LobbyServiceException e)
			{
				Debug.LogException(e);
			}
		}

		public async void LeaveLobby()
		{
			if (m_JoinedLobby == null)
				return;
			
			try
			{
				await LobbyService.Instance.RemovePlayerAsync(m_JoinedLobby.Id, AuthenticationService.Instance.PlayerId);
				m_JoinedLobby = null;
			}
			catch (LobbyServiceException e)
			{
				Debug.LogException(e);
			}
		}

		public async void KickLobby(string pPlayerId)
		{
			if (!IsLobbyHost)
				return;
			
			try
			{
				await LobbyService.Instance.RemovePlayerAsync(m_JoinedLobby.Id, pPlayerId);
			}
			catch (LobbyServiceException e)
			{
				Debug.LogException(e);
			}
		}
	}
}