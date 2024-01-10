using System.Collections.Generic;
using System.Linq;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Network
{
    public static class LobbyConverters
    {
        const string k_KeyRelayCode = nameof(LocalLobby.RelayCode);
        const string k_KeyLobbyState = nameof(LocalLobby.LocalLobbyState);
        const string k_KeyLastEdit = nameof(LocalLobby.LastUpdated);

        const string k_KeyDisplayName = nameof(LocalPlayer.DisplayName);
        const string k_KeyUserStatus = nameof(LocalPlayer.UserStatus);

        public static Dictionary<string, string> LocalToRemoteLobbyData(LocalLobby lobby)
        {
            var data = new Dictionary<string, string>
            {
                { k_KeyRelayCode, lobby.RelayCode.Value },
                { k_KeyLobbyState, ((int)lobby.LocalLobbyState.Value).ToString() },
                { k_KeyLastEdit, lobby.LastUpdated.Value.ToString() }
            };

            return data;
        }

        public static Dictionary<string, string> LocalToRemoteUserData(LocalPlayer user)
        {
            var data = new Dictionary<string, string>();
            if (user == null || string.IsNullOrEmpty(user.ID.Value))
                return data;
            data.Add(k_KeyDisplayName, user.DisplayName.Value);
            data.Add(k_KeyUserStatus, ((int)user.UserStatus.Value).ToString());
            return data;
        }

        public static void RemoteToLocal(Lobby remoteLobby, LocalLobby localLobby)
        {
            if (remoteLobby == null)
            {
                Debug.LogError("Remote lobby is null, cannot convert.");
                return;
            }

            if (localLobby == null)
            {
                Debug.LogError("Local Lobby is null, cannot convert");
                return;
            }

            localLobby.LobbyID.Value = remoteLobby.Id;
            localLobby.HostID.Value = remoteLobby.HostId;
            localLobby.LobbyName.Value = remoteLobby.Name;
            localLobby.LobbyCode.Value = remoteLobby.LobbyCode;
            localLobby.Private.Value = remoteLobby.IsPrivate;
            localLobby.AvailableSlots.Value = remoteLobby.AvailableSlots;
            localLobby.MaxPlayerCount.Value = remoteLobby.MaxPlayers;
            localLobby.LastUpdated.Value = remoteLobby.LastUpdated.ToFileTimeUtc();

            localLobby.RelayCode.Value = remoteLobby.Data?.ContainsKey(k_KeyRelayCode) == true
                ? remoteLobby.Data[k_KeyRelayCode].Value
                : localLobby.RelayCode.Value;
            localLobby.LocalLobbyState.Value = remoteLobby.Data != null && remoteLobby.Data.TryGetValue(k_KeyLobbyState, out var value1)
                ? (LobbyState)int.Parse(value1.Value)
                : LobbyState.Lobby;

            var remotePlayerIDs = new List<string>();
            var index = 0;
            foreach (var player in remoteLobby.Players)
            {
                var id = player.Id;
                remotePlayerIDs.Add(id);
                var isHost = remoteLobby.HostId.Equals(player.Id);
                var displayName = player.Data != null && player.Data.TryGetValue(k_KeyDisplayName, out var value)
                    ? value.Value
                    : default;
                var userStatus = player.Data != null && player.Data.TryGetValue(k_KeyUserStatus, out var value2)
                    ? (PlayerStatus)int.Parse(value2.Value)
                    : PlayerStatus.Lobby;

                var localPlayer = localLobby.GetLocalPlayer(index);

                if (localPlayer == null)
                {
                    localPlayer = new LocalPlayer(id, index, isHost, displayName, userStatus);
                    localLobby.AddPlayer(index, localPlayer);
                }
                else
                {
                    localPlayer.ID.Value = id;
                    localPlayer.Index.Value = index;
                    localPlayer.IsHost.Value = isHost;
                    localPlayer.DisplayName.Value = displayName;
                    localPlayer.UserStatus.Value = userStatus;
                }

                index++;
            }
        }

        public static List<LocalLobby> QueryToLocalList(QueryResponse response)
        {
            return response.Results.Select(RemoteToNewLocal).ToList();
        }

        private static LocalLobby RemoteToNewLocal(Lobby lobby)
        {
            var data = new LocalLobby();
            RemoteToLocal(lobby, data);
            return data;
        }
    }
}