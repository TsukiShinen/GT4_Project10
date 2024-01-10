using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        public Lobby Lobby => m_HostLobby;
        public Action OnLobbyChangeAction;
        public Action OnKickedFromLobbyAction;

        private ILobbyEvents m_LobbyEvents;
        private Lobby m_HostLobby;
        private float m_HeartBeatTimer;

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

        public async Task CreateLobby(string pLobbyName, int pMaxPlayers, string pHostName = "", bool pIsPrivate = false)
        {
            pHostName = pHostName != "" ? pHostName : $"Host";
            
            var options = new CreateLobbyOptions
            {
                IsPrivate = pIsPrivate,
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY, new DataObject(DataObject.VisibilityOptions.Member, "0") }
                },
                Player = new Player (
                    id: AuthenticationService.Instance.PlayerId,
                    profile: new PlayerProfile(pHostName)
                )
            };
            
            m_HostLobby = await LobbyService.Instance.CreateLobbyAsync(pLobbyName, pMaxPlayers, options);
            Debug.Log($"Created lobby! {pLobbyName} {pMaxPlayers} / {m_HostLobby.LobbyCode}");

            var callbacks = new LobbyEventCallbacks();
            callbacks.LobbyChanged += OnLobbyChanged;
            callbacks.KickedFromLobby += OnKickedFromLobby;
            try
            {
                m_LobbyEvents = await Lobbies.Instance.SubscribeToLobbyEventsAsync(m_HostLobby.Id, callbacks);
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
                
                var lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(pCode, options);
                Debug.Log($"Joined lobby with code : {pCode}");
                
                JoinRelay(lobby.Data[KEY_RELAY].Value);
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

                if (m_HostLobby != null)
                {
                    await Lobbies.Instance.UpdateLobbyAsync(m_HostLobby.Id, new UpdateLobbyOptions
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

        private void OnLobbyChanged(ILobbyChanges changes)
        {
            if (changes.LobbyDeleted)
            {
                // Handle lobby being deleted
                // Calling changes.ApplyToLobby will log a warning and do nothing
            }
            else
            {
                OnLobbyChangeAction?.Invoke();
            }
            changes.ApplyToLobby(m_HostLobby);
            // Refresh the UI in some way
        }

        private void OnKickedFromLobby()
        {
            m_LobbyEvents = null;
        }

        public Player GetOwnPlayer()
        {
            return m_HostLobby.Players.Find(player => player.Id == AuthenticationService.Instance.PlayerId);
        }
    }
}