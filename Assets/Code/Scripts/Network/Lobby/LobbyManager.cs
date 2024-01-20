using ScriptableObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ScriptableObjects.GameModes;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Network
{
	public class LobbyManager : MonoBehaviour
	{
		private const string k_KeyRelayJoinCode = "RelayJoinCode";
		public const string k_KeyGameModeIndex = "GameModeIndex";
		
		[SerializeField] private LobbyInfo m_Info;
		[SerializeField] private string m_LobbyScene;
		[SerializeField] private GameModes m_GameModes;
		
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

		private async Task<Allocation> AllocateRelay()
		{
			try
			{
				var allocation = await Relay.Instance.CreateAllocationAsync(MultiplayerManager.Instance.MaxPlayerAmount - 1);

				return allocation;
			}
			catch (RelayServiceException e)
			{
				Debug.LogException(e);
				return default;
			}
		}

		private async Task<string> GetRelayJoinCode(Allocation pAllocation)
		{
			try
			{
				var joinCode = await RelayService.Instance.GetJoinCodeAsync(pAllocation.AllocationId);
				return joinCode;
			}
			catch (RelayServiceException e)
			{
				Debug.LogException(e);
				return default;
			}
		}

		private async Task<JoinAllocation> JoinRelay(string pJoinCode)
		{
			try
			{
				var allocation = await RelayService.Instance.JoinAllocationAsync(pJoinCode);
				return allocation;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				return default;
			}
		}
		
		public async void CreateLobby(string pLobbyName, GameModeConfig pGameMode, int pMaxPlayers, bool pIsPrivate = false)
		{
			Debug.Log($"<color=green>=== Lobby Creation</color>");
			OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty);
			try
			{
				MultiplayerManager.Instance.MaxPlayerAmount = pMaxPlayers;
				MultiplayerManager.Instance.GameModeConfig = pGameMode;
				
				m_JoinedLobby = await LobbyService.Instance.CreateLobbyAsync(pLobbyName, pMaxPlayers, new CreateLobbyOptions()
				{
					IsPrivate = pIsPrivate,
				});
				m_Info.Name = m_JoinedLobby.Name;
				m_Info.Code = m_JoinedLobby.LobbyCode;
				Debug.Log($"Created {m_JoinedLobby.Name} / Code : {m_JoinedLobby.LobbyCode}");

				var allocation = await AllocateRelay();
				var relayJoinCode = await GetRelayJoinCode(allocation);
				Debug.Log($"Relay Created / Code {relayJoinCode}");

				await LobbyService.Instance.UpdateLobbyAsync(m_JoinedLobby.Id, new UpdateLobbyOptions
				{
					Data = new Dictionary<string, DataObject>
					{
						{ k_KeyGameModeIndex, new DataObject(DataObject.VisibilityOptions.Public, m_GameModes.GameModeConfigs.IndexOf(pGameMode).ToString()) },
						{ k_KeyRelayJoinCode, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)}
					}
				});
				NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));
				
				MultiplayerManager.Instance.StartHost();
				Debug.Log($"Host Started");
				NetworkManager.Singleton.SceneManager.LoadScene(m_LobbyScene, LoadSceneMode.Single);
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
				
				var allocation = await JoinRelay(m_JoinedLobby.Data[k_KeyRelayJoinCode].Value);
				Debug.Log($"Joined Relay");
				
				NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));

				MultiplayerManager.Instance.StartClient();
				Debug.Log($"Client Started");
				OnJoinLobbySucceed?.Invoke(this, EventArgs.Empty);
			}
			catch (LobbyServiceException e)
			{
				Debug.LogException(e);
				OnJoinLobbyFailed?.Invoke(this, EventArgs.Empty);
			}
			Debug.Log($"<color=green>=================</color>");
		}

		public async void JoinWithId(string pLobbyId)
		{
			Debug.Log($"<color=green>=== Joining Lobby</color>");
			OnJoinLobbyStarted?.Invoke(this, EventArgs.Empty);
			try
			{
				m_JoinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(pLobbyId);
				Debug.Log($"Joined");
				
				m_Info.Name = m_JoinedLobby.Name;
				m_Info.Code = m_JoinedLobby.LobbyCode;
				
				var allocation = await JoinRelay(m_JoinedLobby.Data[k_KeyRelayJoinCode].Value);
				Debug.Log($"Joined Relay");
				
				NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));

				MultiplayerManager.Instance.StartClient();
				Debug.Log($"Client Started");
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