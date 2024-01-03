using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
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

        public async void CreateLobby()
        {
            try
            {
                const string lobbyName = "Lobby Test";
                const int maxPlayer = 4;

                m_HostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayer);
                Debug.Log($"Created lobby! {lobbyName} {maxPlayer}");
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }

        public async void ListLobbies()
        {
            try
            {
                var queryResponse = await Lobbies.Instance.QueryLobbiesAsync();

                Debug.Log($"Lobbies found: {queryResponse.Results.Count}");
                foreach (var item in queryResponse.Results) Debug.Log($"{item.Name} {item.MaxPlayers}");
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }

        public async void JoinLobbyByCode(string pCode)
        {
            try
            {
                await Lobbies.Instance.JoinLobbyByIdAsync(pCode);
                Debug.Log($"Joined lobby with code : {pCode}");
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
                Debug.Log($"Create Relay : {await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId)}");

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key,
                    allocation.ConnectionData);
                NetworkManager.Singleton.StartHost();
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

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key,
                    allocation.ConnectionData, allocation.HostConnectionData);
                NetworkManager.Singleton.StartClient();
            }
            catch (RelayServiceException e)
            {
                Debug.Log(e);
            }
        }
    }
}