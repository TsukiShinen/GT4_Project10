using System;
using System.Collections;
using System.Threading.Tasks;
using Network;
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
    JoinMenu = 4,
}

public class GameManager : MonoBehaviour
{
    public LocalLobby LocalLobby => m_LocalLobby;
    public Action<GameState> onGameStateChanged;

    public GameState LocalGameState { get; private set; }
    public LobbyManager LobbyManager { get; private set; }

    private LocalPlayer m_LocalUser;
    private LocalLobby m_LocalLobby;

    private static GameManager m_GameManagerInstance;

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

    public async void CreateLobby(string name, bool isPrivate, string password = null, int maxPlayers = 4)
    {
        try
        {
            var lobby = await LobbyManager.CreateLobbyAsync(
                name,
                maxPlayers,
                isPrivate,
                m_LocalUser,
                password);

            LobbyConverters.RemoteToLocal(lobby, m_LocalLobby);
            await CreateLobby();
        }
        catch (LobbyServiceException exception)
        {
            // TODO : Failed to create lobby
            // SetGameState(GameState.JoinMenu);
            // LogHandlerSettings.Instance.SpawnErrorPopup($"Error creating lobby : ({exception.ErrorCode}) {exception.Message}");
        }
    }

    public async void JoinLobby(string lobbyID, string lobbyCode, string password = null)
    {
        try
        {
            var lobby = await LobbyManager.JoinLobbyAsync(lobbyID, lobbyCode,
                m_LocalUser, password:password);

            LobbyConverters.RemoteToLocal(lobby, m_LocalLobby);
            await JoinLobby();
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
        {
            // TODO : Empty user name
            // LogHandlerSettings.Instance.SpawnErrorPopup(
            //    "Empty Name not allowed."); // Lobby error type, then HTTP error type.
            return;
        }

        m_LocalUser.DisplayName.Value = name;
        SendLocalUserData();
    }

    public void SetLocalUserStatus(PlayerStatus status)
    {
        m_LocalUser.UserStatus.Value = status;
        SendLocalUserData();
    }

    private async void SendLocalLobbyData()
    {
        await LobbyManager.UpdateLobbyDataAsync(LobbyConverters.LocalToRemoteLobbyData(m_LocalLobby));
    }

    private async void SendLocalUserData()
    {
        await LobbyManager.UpdatePlayerDataAsync(LobbyConverters.LocalToRemoteUserData(m_LocalUser));
    }

    public void HostSetRelayCode(string code)
    {
        m_LocalLobby.RelayCode.Value = code;
        SendLocalLobbyData();
    }

    void OnPlayersReady(int readyCount)
    {
        if (readyCount == m_LocalLobby.PlayerCount &&
            m_LocalLobby.LocalLobbyState.Value != LobbyState.CountDown)
        {
            m_LocalLobby.LocalLobbyState.Value = LobbyState.CountDown;
            SendLocalLobbyData();
        }
        else if (m_LocalLobby.LocalLobbyState.Value == LobbyState.CountDown)
        {
            m_LocalLobby.LocalLobbyState.Value = LobbyState.Lobby;
            SendLocalLobbyData();
        }
    }

    void OnLobbyStateChanged(LobbyState state)
    {
        // TODO : Start or cancel countdown
        // if (state == LobbyState.Lobby)
        //     CancelCountDown();
        // if (state == LobbyState.CountDown)
        //     BeginCountDown();
    }
    
    private async Task CreateLobby()
    {
        m_LocalUser.IsHost.Value = true;
        m_LocalLobby.onUserReadyChange = OnPlayersReady;
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
        m_LocalUser.IsHost.ForceSet(false);
        await BindLobby();
    }

    private async Task BindLobby()
    {
        await LobbyManager.BindLocalLobbyToRemote(m_LocalLobby.LobbyID.Value, m_LocalLobby);
        m_LocalLobby.LocalLobbyState.onChanged += OnLobbyStateChanged;
        SetLobbyView();
    }

    private IEnumerator RetryConnection(Action doConnection, string lobbyId)
    {
        yield return new WaitForSeconds(5);
        if (m_LocalLobby != null && m_LocalLobby.LobbyID.Value == lobbyId && !string.IsNullOrEmpty(lobbyId))
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
        m_LocalLobby.ResetLobby();
        m_LocalLobby.RelayServer = null;
    }

    public void LeaveLobby()
    {
        m_LocalUser.ResetState();
#pragma warning disable 4014
        LobbyManager.LeaveLobbyAsync();
#pragma warning restore 4014
        ResetLocalLobby();  
        // TODO : Clear Lobby List
        //LobbyList.Clear();
    }
    
    async void Awake()
    {
        Application.wantsToQuit += OnWantToQuit;
        m_LocalUser = new LocalPlayer("", 0, false, "LocalPlayer");
        m_LocalLobby = new LocalLobby { LocalLobbyState = { Value = LobbyState.Lobby } };
        LobbyManager = new LobbyManager();

        await InitializeServices();
        AuthenticatePlayer();
    }

    async Task InitializeServices()
    {
        await UnityServices.InitializeAsync();
#if UNITY_EDITOR
        if (ParrelSync.ClonesManager.IsClone())
        {
            // When using a ParrelSync clone, switch to a different authentication profile to force the clone
            // to sign in as a different anonymous user account.
            string customArgument = ParrelSync.ClonesManager.GetArgument();
            AuthenticationService.Instance.SwitchProfile($"Clone{customArgument}_Profile");
        }
#endif
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    void AuthenticatePlayer()
    {
        var localId = AuthenticationService.Instance.PlayerId;
        var randomName = $"Client-{Random.Range(0, 100)}";

        m_LocalUser.ID.Value = localId;
        m_LocalUser.DisplayName.Value = randomName;
    }
    
    IEnumerator LeaveBeforeQuit()
    {
        ForceLeaveAttempt();
        yield return null;
        Application.Quit();
    }

    bool OnWantToQuit()
    {
        bool canQuit = string.IsNullOrEmpty(m_LocalLobby?.LobbyID.Value);
        StartCoroutine(LeaveBeforeQuit());
        return canQuit;
    }

    void OnDestroy()
    {
        ForceLeaveAttempt();
        LobbyManager.Dispose();
    }

    void ForceLeaveAttempt()
    {
        if (string.IsNullOrEmpty(m_LocalLobby?.LobbyID.Value)) 
            return;
        
#pragma warning disable 4014
        LobbyManager.LeaveLobbyAsync();
#pragma warning restore 4014
        m_LocalLobby = null;
    }
}