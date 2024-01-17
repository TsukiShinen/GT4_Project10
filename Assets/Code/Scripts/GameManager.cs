using System;
using System.Collections;
using System.Threading.Tasks;
using Network;
using ParrelSync;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using UnityEngine;
using Random = UnityEngine.Random;

[Flags]
public enum GameState
{
	Menu = 1,
	Lobby = 2,
	JoinMenu = 4
}

public class GameManager : MonoBehaviour
{
	private static GameManager m_GameManagerInstance;

	public Action<GameState> OnGameStateChanged;
	public LocalPlayer LocalUser { get; private set; }

	public LocalLobby LocalLobby { get; private set; }

	public GameState LocalGameState { get; }
	public LobbyManager LobbyManager { get; private set; }

	public static GameManager Instance
	{
		get
		{
			if (m_GameManagerInstance != null)
				return m_GameManagerInstance;
			m_GameManagerInstance = FindFirstObjectByType<GameManager>();
			return m_GameManagerInstance;
		}
	}

	private async void Awake()
	{
		Application.wantsToQuit += OnWantToQuit;
		LocalUser = new LocalPlayer("Tsukishinen", 0, false, "LocalPlayer");
		LocalLobby = new LocalLobby { LocalLobbyState = { Value = LobbyState.Lobby } };
		LobbyManager = new LobbyManager();

		await InitializeServices();
		AuthenticatePlayer();
	}

	private void OnDestroy()
	{
		ForceLeaveAttempt();
		LobbyManager.Dispose();
	}

	public async Task CreateLobby(string name, bool isPrivate, string password = null, int maxPlayers = 4)
	{
		try
		{
			var lobby = await LobbyManager.CreateLobbyAsync(
				name,
				maxPlayers,
				isPrivate,
				LocalUser,
				password);

			LobbyConverters.RemoteToLocal(lobby, LocalLobby);
			await CreateLobby();
			RelayManager.CreateRelay(lobby);
		}
		catch (LobbyServiceException exception)
		{
			// TODO : Failed to create lobby
			// SetGameState(GameState.JoinMenu);
			// LogHandlerSettings.Instance.SpawnErrorPopup($"Error creating lobby : ({exception.ErrorCode}) {exception.Message}");
		}
	}

	public async Task JoinLobby(string lobbyID, string lobbyCode, string password = null)
	{
		try
		{
			var lobby = await LobbyManager.JoinLobbyAsync(lobbyID, lobbyCode,
				LocalUser, password);

			LobbyConverters.RemoteToLocal(lobby, LocalLobby);
			await JoinLobby();
			RelayManager.JoinRelay(lobbyCode);
		}
		catch (LobbyServiceException exception)
		{
			// TODO : Failed to join Lobby
			// SetGameState(GameState.JoinMenu);
			// LogHandlerSettings.Instance.SpawnErrorPopup($"Error joining lobby : ({exception.ErrorCode}) {exception.Message}");
		}
	}

	public void SetLocalUserName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			// TODO : Empty user name
			// LogHandlerSettings.Instance.SpawnErrorPopup(
			//    "Empty Name not allowed."); // Lobby error type, then HTTP error type.
			return;

		LocalUser.DisplayName.Value = name;
		SendLocalUserData();
	}

	public void SetLocalUserStatus(PlayerStatus status)
	{
		LocalUser.UserStatus.Value = status;
		SendLocalUserData();
	}

	private async void SendLocalLobbyData()
	{
		await LobbyManager.UpdateLobbyDataAsync(LobbyConverters.LocalToRemoteLobbyData(LocalLobby));
	}

	private async void SendLocalUserData()
	{
		await LobbyManager.UpdatePlayerDataAsync(LobbyConverters.LocalToRemoteUserData(LocalUser));
	}

	public void HostSetRelayCode(string code)
	{
		LocalLobby.RelayCode.Value = code;
		SendLocalLobbyData();
	}

	private void OnPlayersReady(int readyCount)
	{
		if (readyCount == LocalLobby.PlayerCount &&
		    LocalLobby.LocalLobbyState.Value != LobbyState.CountDown)
		{
			LocalLobby.LocalLobbyState.Value = LobbyState.CountDown;
			SendLocalLobbyData();
		}
		else if (LocalLobby.LocalLobbyState.Value == LobbyState.CountDown)
		{
			LocalLobby.LocalLobbyState.Value = LobbyState.Lobby;
			SendLocalLobbyData();
		}
	}

	private void OnLobbyStateChanged(LobbyState state)
	{
		// TODO : Start or cancel countdown
		// if (state == LobbyState.Lobby)
		//     CancelCountDown();
		// if (state == LobbyState.CountDown)
		//     BeginCountDown();
	}

	private async Task CreateLobby()
	{
		LocalUser.IsHost.Value = true;
		LocalLobby.OnUserReadyChange = OnPlayersReady;
		try
		{
			await BindLobby();
		}
		catch (LobbyServiceException exception)
		{
			// TODO : Failed to create lobby
			// SetGameState(GameState.JoinMenu);
			// LogHandlerSettings.Instance.SpawnErrorPopup($"Couldn't join Lobby : ({exception.ErrorCode}) {exception.Message}");
		}
	}

	private async Task JoinLobby()
	{
		LocalUser.IsHost.ForceSet(false);
		await BindLobby();
	}

	private async Task BindLobby()
	{
		await LobbyManager.BindLocalLobbyToRemote(LocalLobby.LobbyID.Value, LocalLobby);
		LocalLobby.LocalLobbyState.OnChanged += OnLobbyStateChanged;
		SetLobbyView();
	}

	private IEnumerator RetryConnection(Action doConnection, string lobbyId)
	{
		yield return new WaitForSeconds(5);
		if (LocalLobby != null && LocalLobby.LobbyID.Value == lobbyId && !string.IsNullOrEmpty(lobbyId))
			doConnection?.Invoke();
	}

	private void SetLobbyView()
	{
		Debug.Log($"Setting Lobby user state {GameState.Lobby}");
		// TODO : Set Lobby View
		// SetGameState(GameState.Lobby);
		SetLocalUserStatus(PlayerStatus.Lobby);
	}

	private void ResetLocalLobby()
	{
		LocalLobby.ResetLobby();
		LocalLobby.RelayServer = null;
	}

	public void LeaveLobby()
	{
		LocalUser.ResetState();
#pragma warning disable 4014
		LobbyManager.LeaveLobbyAsync();
#pragma warning restore 4014
		ResetLocalLobby();
		// TODO : Clear Lobby List
		//LobbyList.Clear();
	}

	private async Task InitializeServices()
	{
		await UnityServices.InitializeAsync();
#if UNITY_EDITOR
		if (ClonesManager.IsClone())
		{
			// When using a ParrelSync clone, switch to a different authentication profile to force the clone
			// to sign in as a different anonymous user account.
			var customArgument = ClonesManager.GetArgument();
			AuthenticationService.Instance.SwitchProfile($"Clone{customArgument}_Profile");
		}
#endif
		await AuthenticationService.Instance.SignInAnonymouslyAsync();
	}

	private void AuthenticatePlayer()
	{
		var localId = AuthenticationService.Instance.PlayerId;
		var randomName = $"Client-{Random.Range(0, 100)}";

		LocalUser.ID.Value = localId;
		LocalUser.DisplayName.Value = randomName;
	}

	private IEnumerator LeaveBeforeQuit()
	{
		ForceLeaveAttempt();
		yield return null;
		Application.Quit();
	}

	private bool OnWantToQuit()
	{
		var canQuit = string.IsNullOrEmpty(LocalLobby?.LobbyID.Value);
		StartCoroutine(LeaveBeforeQuit());
		return canQuit;
	}

	private void ForceLeaveAttempt()
	{
		if (string.IsNullOrEmpty(LocalLobby?.LobbyID.Value))
			return;

#pragma warning disable 4014
		LobbyManager.LeaveLobbyAsync();
#pragma warning restore 4014
		LocalLobby = null;
	}
}