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
using UnityEngine;
using Random = UnityEngine.Random;


namespace Network
{
    public class ConnectionManager : MonoBehaviour
    {
        public static ConnectionManager Instance;
        public Lobby Lobby => m_JoinedLobby;
        public Action<List<LobbyPlayerJoined>> OnPlayerJoin;
        public Action<Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>>> OnPlayerDataChange;

        private ILobbyEvents m_LobbyEvents;
        private Lobby m_HostLobby;
        private Lobby m_JoinedLobby;
        private float m_HeartBeatTimer;
        private float m_LobbyPollTimer;

        private const string KEY_RELAY = "Relay";

        private async void Awake()
        {
            if (Instance)
            {
                Debug.Log("Can only have one instance of TestLobby!");
                Destroy(this);
            }

            Instance = this;
            await UnityServices.InitializeAsync();

            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log($"Signed in {AuthenticationService.Instance.PlayerId}");
            };
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

        private void Update()
        {
            HandleLobbyHeartBeat();
            HandlePollForUpdates();
        }

        private async void HandleLobbyHeartBeat()
        {
            if (m_HostLobby == null) return;

            m_HeartBeatTimer -= Time.deltaTime;
            if (m_HeartBeatTimer > 0f) return;

            const float heartBeatTimerMax = 15;
            m_HeartBeatTimer = heartBeatTimerMax;

            await LobbyService.Instance.SendHeartbeatPingAsync(m_HostLobby.Id);
        }

        private async void HandlePollForUpdates()
        {
            if (m_JoinedLobby == null)
                return;

            m_LobbyPollTimer -= Time.deltaTime;
            if (m_LobbyPollTimer > 0f) return;

            const float lobbyPollTimerMax = 1.1f;
            m_LobbyPollTimer = lobbyPollTimerMax;

            m_JoinedLobby = await LobbyService.Instance.GetLobbyAsync(m_JoinedLobby.Id);
        }

        public async Task CreateLobby(string pLobbyName, int pMaxPlayers, GameModeConfig pGameMode, string pHostName = "", bool pIsPrivate = false)
        {
            pHostName = pHostName != "" ? pHostName : $"Tsukishinen";

            var options = new CreateLobbyOptions
            {
                IsPrivate = pIsPrivate,
                Data = new Dictionary<string, DataObject>
                {
                    { "GAMEMODE", new DataObject(DataObject.VisibilityOptions.Public, pGameMode.Name, DataObject.IndexOptions.S1)},
                    { KEY_RELAY, new DataObject(DataObject.VisibilityOptions.Member, "0") }
                },
                Player = new Player(
                    id: AuthenticationService.Instance.PlayerId,
                    profile: new PlayerProfile(pHostName),
                    data: new Dictionary<string, PlayerDataObject>
                    {
                        {
                            "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "false")
                        }
                    }
                )
            };

            m_HostLobby = await LobbyService.Instance.CreateLobbyAsync(pLobbyName, pMaxPlayers, options);
            m_JoinedLobby = m_HostLobby;

            Debug.Log($"Created lobby! {pLobbyName} {pMaxPlayers} / {m_HostLobby.LobbyCode}");

            var callbacks = new LobbyEventCallbacks();
            callbacks.PlayerJoined += test1;
            callbacks.PlayerDataChanged += test2;

            try
            {
                m_LobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(m_HostLobby.Id, callbacks);
            }
            catch (LobbyServiceException ex)
            {
                switch (ex.Reason)
                {
                    case LobbyExceptionReason.AlreadySubscribedToLobby: Debug.LogWarning($"Already subscribed to lobby[{m_HostLobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}"); break;
                    case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy: Debug.LogError($"Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}"); throw;
                    case LobbyExceptionReason.LobbyEventServiceConnectionError: Debug.LogError($"Failed to connect to lobby events. Exception Message: {ex.Message}"); throw;
                    default: throw;
                }
            }

            CreateRelay();
        }

        public async Task<List<Lobby>> ListLobbies()
        {
            try
            {
                return (await Lobbies.Instance.QueryLobbiesAsync()).Results;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }

            return null;
        }

        public async Task JoinLobbyByCode(string pCode, string pClientName = "")
        {
            try
            {
                pClientName = pClientName != "" ? pClientName : $"Client{Random.Range(1, 100)}";
                var options = new JoinLobbyByCodeOptions
                {
                    Player = new Player (
                        id: AuthenticationService.Instance.PlayerId,
                        profile: new PlayerProfile(pClientName)
                    )
                };

                m_JoinedLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(pCode, options);
                Debug.Log($"Joined lobby with code : {pCode}");

                JoinRelay(m_JoinedLobby.Data[KEY_RELAY].Value);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }

        public async void CreateRelay()
        {
            try
            {
                var allocation = await RelayService.Instance.CreateAllocationAsync(8);
                var relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                Debug.Log($"Create Relay : {relayCode}");

                var relayServerData = new RelayServerData(allocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
                NetworkManager.Singleton.StartHost();

                if (m_JoinedLobby != null)
                {
                    await Lobbies.Instance.UpdateLobbyAsync(m_JoinedLobby.Id, new UpdateLobbyOptions
                    {
                        Data = new Dictionary<string, DataObject>
                        {
                            { KEY_RELAY, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                        }
                    });
                }
            }
            catch (RelayServiceException e)
            {
                Debug.Log(e);
            }
        }

        public async void JoinRelay(string pCode)
        {
            try
            {
                var allocation = await RelayService.Instance.JoinAllocationAsync(pCode);

                var relayServerData = new RelayServerData(allocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
                NetworkManager.Singleton.StartClient();
            }
            catch (RelayServiceException e)
            {
                Debug.Log(e);
            }
        }

        public Player GetOwnPlayer()
        {
            return m_JoinedLobby.Players.Find(player => player.Id == AuthenticationService.Instance.PlayerId);
        }

        public Player GetPlayerById(string id)
        {
            return m_JoinedLobby.Players.Find(player => player.Id == id);
        }

        public void test1(List<LobbyPlayerJoined> aOnPlayerJoin)
        {
            Debug.LogError("test1");
            OnPlayerJoin?.Invoke(aOnPlayerJoin);
        }

        public void test2(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> aOnPlayerDataChange)
        {
            Debug.LogError("test2");
            OnPlayerDataChange?.Invoke(aOnPlayerDataChange);
        }
    }
}