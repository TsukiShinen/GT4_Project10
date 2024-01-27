using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ParrelSync;
using ScriptableObjects;
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
using Random = UnityEngine.Random;

namespace Network
{
	public class LobbyManager : MonoBehaviour
	{
		private const string k_KeyRelayJoinCode = "RelayJoinCode";
		public const string k_KeyGameModeIndex = "GameModeIndex";

		[SerializeField] private LobbyInfo m_Info;
		[SerializeField] private string m_LobbyScene;
		[SerializeField] private GameModes m_GameModes;
		private float m_HeartBeatTimer;

		public Lobby Lobby { get; private set; }

		private bool IsLobbyHost =>
			Lobby != null && Lobby.HostId == AuthenticationService.Instance.PlayerId;

		public static LobbyManager Instance { get; private set; }

		private void Awake()
		{
			if (Instance)
				Destroy(Instance.gameObject);

			Instance = this;
			DontDestroyOnLoad(gameObject);

			InitializeUnityAuthentication();
		}

		private void Update()
		{
			HandleHeartBeat();
		}

		private void OnDestroy()
		{
			LeaveLobby();
		}

		public event EventHandler OnCreateLobbyStarted;
		public event EventHandler OnCreateLobbySucceed;
		public event EventHandler OnCreateLobbyFailed;
		public event EventHandler OnJoinLobbyStarted;
		public event EventHandler OnJoinLobbySucceed;
		public event EventHandler OnJoinLobbyFailed;
		public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;

		private static async void InitializeUnityAuthentication()
		{
			if (UnityServices.State == ServicesInitializationState.Initialized) return;
			await UnityServices.InitializeAsync();

#if UNITY_EDITOR
			if (ClonesManager.IsClone())
			{
				var customArgument = ClonesManager.GetArgument();
				AuthenticationService.Instance.SwitchProfile($"Clone{customArgument}Profile{Random.Range(0, 100000)}");
			}
#endif
			await AuthenticationService.Instance.SignInAnonymouslyAsync();
			Debug.Log("<color=green>===== Player Connected =====</color>");
		}

		private void HandleHeartBeat()
		{
			if (!IsLobbyHost)
				return;

			m_HeartBeatTimer -= Time.deltaTime;
			if (m_HeartBeatTimer > 0)
				return;

			m_HeartBeatTimer = 15;
			LobbyService.Instance.SendHeartbeatPingAsync(Lobby.Id);
		}

		public async void ListLobbies()
		{
			try
			{
				var options = new QueryLobbiesOptions
				{
					Filters = new List<QueryFilter>
					{
						new(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GE)
					}
				};
				var response = await LobbyService.Instance.QueryLobbiesAsync(options);
				OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs
				{
					LobbyList = response.Results
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
				var allocation =
					await Relay.Instance.CreateAllocationAsync(MultiplayerManager.Instance.MaxPlayerAmount - 1);

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

		public async void CreateLobby(string pLobbyName, GameModeConfig pGameMode, bool pIsPrivate = false)
		{
			Debug.Log("<color=green>=== Lobby Creation</color>");
			OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty);
			try
			{
				MultiplayerManager.Instance.MaxPlayerAmount =
					pGameMode.HasTeams ? pGameMode.MaxPlayer * 2 : pGameMode.MaxPlayer;
				MultiplayerManager.Instance.GameModeConfig = pGameMode;

				Lobby = await LobbyService.Instance.CreateLobbyAsync(pLobbyName,
					MultiplayerManager.Instance.MaxPlayerAmount, new CreateLobbyOptions
					{
						IsPrivate = pIsPrivate
					});
				m_Info.Name = Lobby.Name;
				m_Info.Code = Lobby.LobbyCode;
				Debug.Log($"Created {Lobby.Name} / Code : {Lobby.LobbyCode}");

				var allocation = await AllocateRelay();
				var relayJoinCode = await GetRelayJoinCode(allocation);
				Debug.Log($"Relay Created / Code {relayJoinCode}");

				await LobbyService.Instance.UpdateLobbyAsync(Lobby.Id, new UpdateLobbyOptions
				{
					Data = new Dictionary<string, DataObject>
					{
						{
							k_KeyGameModeIndex,
							new DataObject(DataObject.VisibilityOptions.Public,
								m_GameModes.GameModeConfigs.IndexOf(pGameMode).ToString())
						},
						{ k_KeyRelayJoinCode, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
					}
				});
				NetworkManager.Singleton.GetComponent<UnityTransport>()
					.SetRelayServerData(new RelayServerData(allocation, "dtls"));

				MultiplayerManager.Instance.StartHost();
				Debug.Log("Host Started");
				NetworkManager.Singleton.SceneManager.LoadScene(m_LobbyScene, LoadSceneMode.Single);
				OnCreateLobbySucceed?.Invoke(this, EventArgs.Empty);
			}
			catch (LobbyServiceException e)
			{
				Debug.LogException(e);
				OnCreateLobbyFailed?.Invoke(this, EventArgs.Empty);
			}

			Debug.Log("<color=green>==================</color>");
		}

		public async void JoinWithCode(string pLobbyCode)
		{
			Debug.Log("<color=green>=== Joining Lobby</color>");
			OnJoinLobbyStarted?.Invoke(this, EventArgs.Empty);
			try
			{
				Lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(pLobbyCode);
				Debug.Log("Joined");

				m_Info.Name = Lobby.Name;
				m_Info.Code = Lobby.LobbyCode;
				MultiplayerManager.Instance.GameModeConfig =
					m_GameModes.GameModeConfigs[int.Parse(Lobby.Data[k_KeyGameModeIndex].Value)];
				MultiplayerManager.Instance.MaxPlayerAmount =
					MultiplayerManager.Instance.GameModeConfig.HasTeams
						? MultiplayerManager.Instance.GameModeConfig.MaxPlayer * 2
						: MultiplayerManager.Instance.GameModeConfig.MaxPlayer;

				var allocation = await JoinRelay(Lobby.Data[k_KeyRelayJoinCode].Value);
				Debug.Log("Joined Relay");

				NetworkManager.Singleton.GetComponent<UnityTransport>()
					.SetRelayServerData(new RelayServerData(allocation, "dtls"));

				MultiplayerManager.Instance.StartClient();
				Debug.Log("Client Started");
				OnJoinLobbySucceed?.Invoke(this, EventArgs.Empty);
			}
			catch (LobbyServiceException e)
			{
				Debug.LogException(e);
				OnJoinLobbyFailed?.Invoke(this, EventArgs.Empty);
			}

			Debug.Log("<color=green>=================</color>");
		}

		public async void JoinWithId(string pLobbyId)
		{
			Debug.Log("<color=green>=== Joining Lobby</color>");
			OnJoinLobbyStarted?.Invoke(this, EventArgs.Empty);
			try
			{
				Lobby = await LobbyService.Instance.JoinLobbyByIdAsync(pLobbyId);
				Debug.Log("Joined");

				m_Info.Name = Lobby.Name;
				m_Info.Code = Lobby.LobbyCode;
				MultiplayerManager.Instance.GameModeConfig =
					m_GameModes.GameModeConfigs[int.Parse(Lobby.Data[k_KeyGameModeIndex].Value)];
				MultiplayerManager.Instance.MaxPlayerAmount =
					MultiplayerManager.Instance.GameModeConfig.HasTeams
						? MultiplayerManager.Instance.GameModeConfig.MaxPlayer * 2
						: MultiplayerManager.Instance.GameModeConfig.MaxPlayer;

				var allocation = await JoinRelay(Lobby.Data[k_KeyRelayJoinCode].Value);
				Debug.Log("Joined Relay");

				NetworkManager.Singleton.GetComponent<UnityTransport>()
					.SetRelayServerData(new RelayServerData(allocation, "dtls"));

				MultiplayerManager.Instance.StartClient();
				Debug.Log("Client Started");
				OnJoinLobbySucceed?.Invoke(this, EventArgs.Empty);
			}
			catch (LobbyServiceException e)
			{
				Debug.LogException(e);
				OnJoinLobbyFailed?.Invoke(this, EventArgs.Empty);
			}

			Debug.Log("<color=green>=================</color>");
		}

		public async void DeleteLobby()
		{
			if (Lobby == null)
				return;

			try
			{
				await LobbyService.Instance.DeleteLobbyAsync(Lobby.Id);
				Lobby = null;
			}
			catch (LobbyServiceException e)
			{
				Debug.LogException(e);
			}
		}

		public async void LeaveLobby()
		{
			if (Lobby == null)
				return;

			try
			{
				await LobbyService.Instance.RemovePlayerAsync(Lobby.Id, AuthenticationService.Instance.PlayerId);
				Lobby = null;
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
				await LobbyService.Instance.RemovePlayerAsync(Lobby.Id, pPlayerId);
			}
			catch (LobbyServiceException e)
			{
				Debug.LogException(e);
			}
		}

		public void SetLobbyNull()
		{
			Lobby = null;
		}

		public class OnLobbyListChangedEventArgs : EventArgs
		{
			public List<Lobby> LobbyList;
		}
	}
}