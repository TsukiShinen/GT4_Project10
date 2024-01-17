using System;
using ScriptableObjects;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Network
{
	public class LobbyManager : MonoBehaviour
	{
		[SerializeField] private LobbyInfo m_Info;

		private Lobby m_JoinedLobby;
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

		public async void CreateLobby(string pLobbyName, bool pIsPrivate = false)
		{
			Debug.Log($"<color=green>=== Lobby Creation</color>");
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
			}
			catch (LobbyServiceException e)
			{
				Debug.LogException(e);
			}
			Debug.Log($"<color=green>==================</color>");
		}

		public async void JoinWithCode(string pLobbyCode)
		{
			Debug.Log($"<color=green>=== Joining Lobby</color>");
			try
			{
				m_JoinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(pLobbyCode);
				Debug.Log($"Joined");
				
				m_Info.Name = m_JoinedLobby.Name;
				m_Info.Code = m_JoinedLobby.LobbyCode;
					
				GameManager.Instance.StartClient();
			}
			catch (LobbyServiceException e)
			{
				Debug.LogException(e);
			}
			Debug.Log($"<color=green>=================</color>");
		}
	}
}