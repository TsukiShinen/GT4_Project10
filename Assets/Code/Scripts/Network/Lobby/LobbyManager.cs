using System;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Network
{
	public class LobbyManager : MonoBehaviour
	{
		private const int k_MaxPlayerAmount = 4;

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
			
			var options = new InitializationOptions();
			options.SetProfile(Random.Range(0, 10000).ToString());
			
#if UNITY_EDITOR
			if (ParrelSync.ClonesManager.IsClone())
			{
				var customArgument = ParrelSync.ClonesManager.GetArgument();
				AuthenticationService.Instance.SwitchProfile($"Clone{customArgument}_Profile");
			}
#endif

			await UnityServices.InitializeAsync(options);

			await AuthenticationService.Instance.SignInAnonymouslyAsync();
			Debug.Log($"<color=green>===== Player Connected =====</color>");
		}

		public async  void CreateLobby(string pLobbyName, bool pIsPrivate = false)
		{
			try
			{
				Debug.Log($"<color=green>=== Lobby Creation</color>");
				m_JoinedLobby = await LobbyService.Instance.CreateLobbyAsync(pLobbyName, k_MaxPlayerAmount, new CreateLobbyOptions()
				{
					IsPrivate = pIsPrivate,
				});
				Debug.Log($"Created");
				Debug.Log($"Code : {m_JoinedLobby.LobbyCode}");
				
				GameManager.Instance.StartHost();
				// TODO : Load new Scene
				Debug.Log($"<color=green>==================</color>");
			}
			catch (LobbyServiceException e)
			{
				Debug.LogException(e);
			}
		}
	}
}