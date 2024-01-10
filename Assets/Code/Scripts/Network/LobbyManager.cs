using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Network
{
    public class LobbyManager : IDisposable
    {
        private const string k_KeyRelayCode = nameof(LocalLobby.RelayCode);
        private const string k_KeyLobbyState = nameof(LocalLobby.LocalLobbyState);
        
        private const string k_KeyDisplayName = nameof(LocalPlayer.DisplayName);
        private const string k_KeyUserStatus = nameof(LocalPlayer.UserStatus);
        
        private Lobby m_CurrentLobby;
        private LobbyEventCallbacks m_LobbyEventCallbacks = new LobbyEventCallbacks();
        
        private Task m_HeartBeatTask;
        private float m_HeartBeatTimer;
        
        public Lobby CurrentLobby => m_CurrentLobby;

        public bool InLobby()
        {
            if (m_CurrentLobby != null) return true;
            
            Debug.LogWarning("LobbyManager not currently in a lobby. Did you CreateLobbyAsync or JoinLobbyAsync?");
            return false;
        }
        
        Dictionary<string, PlayerDataObject> CreateInitialPlayerData(LocalPlayer user)
        {
            var data = new Dictionary<string, PlayerDataObject>();

            var displayNameObject =
                new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, user.DisplayName.Value);
            data.Add("DisplayName", displayNameObject);
            return data;
        }
        
        

        public async Task<Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate,
            LocalPlayer localUser, string password)
        {
            var createOptions = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = new Player(id: AuthenticationService.Instance.PlayerId, data: CreateInitialPlayerData(localUser)),
                Password = password
            };
            m_CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createOptions);
            StartHeartBeat();
            Debug.Log($"Lobby Code : {m_CurrentLobby.LobbyCode}");

            return m_CurrentLobby;
        }
        
        public async Task<Lobby> JoinLobbyAsync(string lobbyId, string lobbyCode, LocalPlayer localUser,
            string password = null)
        {
            var playerData = CreateInitialPlayerData(localUser);

            if (!string.IsNullOrEmpty(lobbyId))
            {
                var joinOptions = new JoinLobbyByIdOptions
                {
                    Player = new Player(id: AuthenticationService.Instance.PlayerId, data: playerData), 
                    Password = password
                };
                m_CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions);
            }
            else
            {
                var joinOptions = new JoinLobbyByCodeOptions
                {
                    Player = new Player(id: AuthenticationService.Instance.PlayerId, data: playerData), 
                    Password = password
                };
                m_CurrentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);
            }

            return m_CurrentLobby;
        }
        
        public async Task BindLocalLobbyToRemote(string lobbyID, LocalLobby localLobby)
        {
            m_LobbyEventCallbacks.LobbyDeleted += async () =>
            {
                await LeaveLobbyAsync();
            };

            m_LobbyEventCallbacks.DataChanged += changes =>
            {
                foreach (var (changedKey, changedValue) in changes)
                {
                    switch (changedKey)
                    {
                        case k_KeyRelayCode:
                            localLobby.RelayCode.Value = changedValue.Value.Value;
                            break;
                        case k_KeyLobbyState:
                            localLobby.LocalLobbyState.Value = (LobbyState)int.Parse(changedValue.Value.Value);
                            break;
                    }
                }
            };

            m_LobbyEventCallbacks.DataAdded += changes =>
            {
                foreach (var (changedKey, changedValue) in changes)
                {
                    switch (changedKey)
                    {
                        case k_KeyRelayCode:
                            localLobby.RelayCode.Value = changedValue.Value.Value;
                            break;
                        case k_KeyLobbyState:
                            localLobby.LocalLobbyState.Value = (LobbyState)int.Parse(changedValue.Value.Value);
                            break;
                    }
                }
            };

            m_LobbyEventCallbacks.DataRemoved += changes =>
            {
                foreach (var changedKey in changes.Select(change => change.Key)
                             .Where(changedKey => changedKey == k_KeyRelayCode))
                {
                    localLobby.RelayCode.Value = "";
                }
            };

            m_LobbyEventCallbacks.PlayerLeft += players =>
            {
                foreach (var leftPlayerIndex in players)
                {
                    localLobby.RemovePlayer(leftPlayerIndex);
                }
            };

            m_LobbyEventCallbacks.PlayerJoined += players =>
            {
                foreach (var playerChanges in players)
                {
                    var joinedPlayer = playerChanges.Player;

                    var id = joinedPlayer.Id;
                    var index = playerChanges.PlayerIndex;
                    var isHost = localLobby.HostID.Value == id;

                    var newPlayer = new LocalPlayer(id, index, isHost);

                    foreach (var (key, dataObject) in joinedPlayer.Data)
                    {
                        ParseCustomPlayerData(newPlayer, key, dataObject.Value);
                    }

                    localLobby.AddPlayer(index, newPlayer);
                }
            };

            m_LobbyEventCallbacks.PlayerDataChanged += changes =>
            {
                foreach (var (playerIndex, playerChanges) in changes)
                {
                    var localPlayer = localLobby.GetLocalPlayer(playerIndex);
                    if (localPlayer == null)
                        continue;

                    foreach (var (key, changedValue) in playerChanges)
                    {
                        var playerDataObject = changedValue.Value;
                        ParseCustomPlayerData(localPlayer, key, playerDataObject.Value);
                    }
                }
            };

            m_LobbyEventCallbacks.PlayerDataAdded += changes =>
            {
                foreach (var (playerIndex, playerChanges) in changes)
                {
                    var localPlayer = localLobby.GetLocalPlayer(playerIndex);
                    if (localPlayer == null)
                        continue;

                    foreach (var (key, changedValue) in playerChanges)
                    {
                        var playerDataObject = changedValue.Value;
                        ParseCustomPlayerData(localPlayer, key, playerDataObject.Value);
                    }
                }
            };

            m_LobbyEventCallbacks.PlayerDataRemoved += changes =>
            {
                foreach (var (playerIndex, playerChanges) in changes)
                {
                    var localPlayer = localLobby.GetLocalPlayer(playerIndex);
                    if (localPlayer == null)
                        continue;

                    if (playerChanges == null)
                        continue;

                    foreach (var playerChange in playerChanges.Values)
                    {
                        Debug.LogWarning("This Sample does not remove Player Values currently.");
                    }
                }
            };

            m_LobbyEventCallbacks.LobbyChanged += async changes =>
            {
                //Lobby Fields
                if (changes.Name.Changed)
                    localLobby.LobbyName.Value = changes.Name.Value;
                if (changes.HostId.Changed)
                    localLobby.HostID.Value = changes.HostId.Value;
                if (changes.IsPrivate.Changed)
                    localLobby.Private.Value = changes.IsPrivate.Value;
                if (changes.IsLocked.Changed)
                    localLobby.Locked.Value = changes.IsLocked.Value;
                if (changes.AvailableSlots.Changed)
                    localLobby.AvailableSlots.Value = changes.AvailableSlots.Value;
                if (changes.MaxPlayers.Changed)
                    localLobby.MaxPlayerCount.Value = changes.MaxPlayers.Value;

                if (changes.LastUpdated.Changed)
                    localLobby.LastUpdated.Value = changes.LastUpdated.Value.ToFileTimeUtc();

                //Custom Lobby Fields

                if (changes.PlayerData.Changed)
                    PlayerDataChanged();

                void PlayerDataChanged()
                {
                    foreach (var (playerIndex, playerChanges) in changes.PlayerData.Value)
                    {
                        var localPlayer = localLobby.GetLocalPlayer(playerIndex);
                        if (localPlayer == null)
                            continue;
                        if (playerChanges.ConnectionInfoChanged.Changed)
                        {
                            var connectionInfo = playerChanges.ConnectionInfoChanged.Value;
                            Debug.Log(
                                $"ConnectionInfo for player {playerIndex} changed to {connectionInfo}");
                        }

                        if (playerChanges.LastUpdatedChanged.Changed) { }
                    }
                }
            };

            m_LobbyEventCallbacks.LobbyEventConnectionStateChanged += lobbyEventConnectionState =>
            {
                Debug.Log($"Lobby ConnectionState Changed to {lobbyEventConnectionState}");
            };

            m_LobbyEventCallbacks.KickedFromLobby += () =>
            {
                Debug.Log("Left Lobby");
                Dispose();
            };
            await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyID, m_LobbyEventCallbacks);
        }
        
        private static void ParseCustomPlayerData(LocalPlayer player, string dataKey, string playerDataValue)
        {
            switch (dataKey)
            {
                case k_KeyUserStatus:
                    player.UserStatus.Value = (PlayerStatus)int.Parse(playerDataValue);
                    break;
                case k_KeyDisplayName:
                    player.DisplayName.Value = playerDataValue;
                    break;
            }
        }

        public async Task<Lobby> GetLobbyAsync(string lobbyId = null)
        {
            if (!InLobby())
                return null;
            
            lobbyId ??= m_CurrentLobby.Id;
            return m_CurrentLobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
        }

        public async Task LeaveLobbyAsync()
        {
            if (!InLobby())
                return;
            var playerId = AuthenticationService.Instance.PlayerId;

            await LobbyService.Instance.RemovePlayerAsync(m_CurrentLobby.Id, playerId);
            Dispose();
        }

        public async Task UpdatePlayerDataAsync(Dictionary<string, string> data)
        {
            if (!InLobby())
                return;

            var playerId = AuthenticationService.Instance.PlayerId;
            var dataCurr = new Dictionary<string, PlayerDataObject>();
            foreach (var dataNew in data)
            {
                var dataObj = new PlayerDataObject(visibility: PlayerDataObject.VisibilityOptions.Member,
                    value: dataNew.Value);
                if (dataCurr.ContainsKey(dataNew.Key))
                    dataCurr[dataNew.Key] = dataObj;
                else
                    dataCurr.Add(dataNew.Key, dataObj);
            }

            var updateOptions = new UpdatePlayerOptions
            {
                Data = dataCurr,
                AllocationId = null,
                ConnectionInfo = null
            };
            m_CurrentLobby = await LobbyService.Instance.UpdatePlayerAsync(m_CurrentLobby.Id, playerId, updateOptions);
        }

        public async Task UpdatePlayerRelayInfoAsync(string lobbyID, string allocationId, string connectionInfo)
        {
            if (!InLobby())
                return;

            var playerId = AuthenticationService.Instance.PlayerId;

            var updateOptions = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>(),
                AllocationId = allocationId,
                ConnectionInfo = connectionInfo
            };
            m_CurrentLobby = await LobbyService.Instance.UpdatePlayerAsync(lobbyID, playerId, updateOptions);
        }

        public async Task UpdateLobbyDataAsync(Dictionary<string, string> data)
        {
            if (!InLobby())
                return;

            var dataCurr = m_CurrentLobby.Data ?? new Dictionary<string, DataObject>();

            var shouldLock = false;
            foreach (var dataNew in data)
            {
                var index = dataNew.Key == "LocalLobbyColor" ? DataObject.IndexOptions.N1 : 0;
                var dataObj = new DataObject(DataObject.VisibilityOptions.Public, dataNew.Value,
                    index);
                if (dataCurr.ContainsKey(dataNew.Key))
                    dataCurr[dataNew.Key] = dataObj;
                else
                    dataCurr.Add(dataNew.Key, dataObj);

                if (dataNew.Key != "LocalLobbyState") continue;
                
                Enum.TryParse(dataNew.Value, out LobbyState lobbyState);
                shouldLock = lobbyState != LobbyState.Lobby;
            }

            UpdateLobbyOptions updateOptions = new UpdateLobbyOptions { Data = dataCurr, IsLocked = shouldLock };
            m_CurrentLobby = await LobbyService.Instance.UpdateLobbyAsync(m_CurrentLobby.Id, updateOptions);
        }

        public async Task DeleteLobbyAsync()
        {
            if (!InLobby())
                return;

            await LobbyService.Instance.DeleteLobbyAsync(m_CurrentLobby.Id);
        }

        private async Task SendHeartbeatPingAsync()
        {
            if (!InLobby())
                return;

            await LobbyService.Instance.SendHeartbeatPingAsync(m_CurrentLobby.Id);
        }

        private void StartHeartBeat()
        {
#pragma warning disable 4014
            m_HeartBeatTask = HeartBeatLoop();
#pragma warning restore 4014
        }

        private async Task HeartBeatLoop()
        {
            while (m_CurrentLobby != null)
            {
                await SendHeartbeatPingAsync();
                await Task.Delay(8000);
            }
        }
        
        public void Dispose()
        {
            m_CurrentLobby = null;
            m_LobbyEventCallbacks = new LobbyEventCallbacks();
        }
    }
}