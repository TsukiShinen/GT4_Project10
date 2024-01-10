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


namespace Network
{
    public class ConnectionManager : MonoBehaviour
    {
        public static ConnectionManager Instance;

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

        public async void CreateLobby(string pLobbyName, int pMaxPlayers, string pHostName = "", bool pIsPrivate = false)
        {
            pHostName = pHostName != "" ? pHostName : $"Host";
            
            var options = new CreateLobbyOptions
            {
                IsPrivate = pIsPrivate,
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY, new DataObject(DataObject.VisibilityOptions.Member, "0") }
                },
                Player = new Player(
                    id: AuthenticationService.Instance.PlayerId,
                    data: new Dictionary<string, PlayerDataObject>
                    {
                        {
                            "Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, pHostName)
                        }
                    })
            };
            
            m_HostLobby = await LobbyService.Instance.CreateLobbyAsync(pLobbyName, pMaxPlayers, options);
            Debug.Log($"Created lobby! {pLobbyName} {pMaxPlayers} / {m_HostLobby.LobbyCode}");
            
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

        public async void JoinLobbyByCode(string pCode, string pClientName = "")
        {
            try
            {
                var lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(pCode);
                pClientName = pClientName != "" ? pClientName : $"Client{lobby.Players.Count}";
                Debug.Log($"Joined lobby with code : {pCode}");
                var options = new UpdatePlayerOptions()
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        {
                            "Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, pClientName)
                        }
                    }
                };
                var playerId = AuthenticationService.Instance.PlayerId;
                await LobbyService.Instance.UpdatePlayerAsync(lobby.Id, playerId, options);
                
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
    }
}