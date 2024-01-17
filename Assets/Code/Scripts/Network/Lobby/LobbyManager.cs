using System;
using ScriptableObjects;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Network
{
	public class LobbyManager : MonoBehaviour
	{
		private const int k_MaxPlayerAmount = 4;

		[SerializeField] private LobbyInfo m_Info;

		private Lobby m_JoinedLobby;
		
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

		public async  void CreateLobby(string pLobbyName, bool pIsPrivate = false)
		{
			Debug.Log($"<color=green>=== Lobby Creation</color>");
			try
			{
				m_JoinedLobby = await LobbyService.Instance.CreateLobbyAsync(pLobbyName, k_MaxPlayerAmount, new CreateLobbyOptions()
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
				SceneManager.LoadSceneAsync("Lobby", LoadSceneMode.Additive);
			}
			catch (LobbyServiceException e)
			{
				Debug.LogException(e);
			}
			Debug.Log($"<color=green>=================</color>");
		}
	}
}