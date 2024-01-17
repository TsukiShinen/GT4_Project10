using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Network
{
	[Flags]
	public enum LobbyState
	{
		Lobby = 1,
		CountDown = 2,
		InGame = 4
	}

	[Serializable]
	public class LocalLobby
	{
		public CallbackValue<int> AvailableSlots = new();

		public CallbackValue<string> HostID = new();

		public CallbackValue<long> LastUpdated = new();

		public CallbackValue<string> LobbyCode = new();

		public CallbackValue<string> LobbyID = new();

		public CallbackValue<string> LobbyName = new();

		public CallbackValue<LobbyState> LocalLobbyState = new();

		public CallbackValue<bool> Locked = new();
		private ServerAddress m_RelayServer;

		public CallbackValue<int> MaxPlayerCount = new();
		public Action<LocalPlayer> OnUserJoined;

		public Action<int> OnUserLeft;

		public Action<int> OnUserReadyChange;

		public CallbackValue<bool> Private = new();

		public CallbackValue<string> RelayCode = new();

		public CallbackValue<ServerAddress> RelayServer = new();

		public LocalLobby()
		{
			LastUpdated.Value = DateTime.Now.ToFileTimeUtc();
			HostID.OnChanged += OnHostChanged;
		}

		public List<LocalPlayer> Players { get; } = new();

		public int PlayerCount => Players.Count;

		public List<LocalPlayer> LocalPlayers => Players;

		public void ResetLobby()
		{
			Players.Clear();

			LobbyName.Value = "";
			LobbyID.Value = "";
			LobbyCode.Value = "";
			Locked.Value = false;
			Private.Value = false;
			AvailableSlots.Value = 4;
			MaxPlayerCount.Value = 4;
			OnUserJoined = null;
			OnUserLeft = null;
		}

		~LocalLobby()
		{
			HostID.OnChanged -= OnHostChanged;
		}

		public LocalPlayer GetLocalPlayer(int index)
		{
			return PlayerCount > index ? Players[index] : null;
		}

		private void OnHostChanged(string newHostId)
		{
			foreach (var player in Players) player.IsHost.Value = player.ID.Value == newHostId;
		}

		public void AddPlayer(int index, LocalPlayer user)
		{
			Players.Insert(index, user);
			user.UserStatus.OnChanged += OnUserChangedStatus;
			OnUserJoined?.Invoke(user);
			Debug.Log($"Added User: '{user.DisplayName.Value}' to slot {index + 1}/{MaxPlayerCount.Value}");
		}

		public void RemovePlayer(int playerIndex)
		{
			Players[playerIndex].UserStatus.OnChanged -= OnUserChangedStatus;
			Players.RemoveAt(playerIndex);
			OnUserLeft?.Invoke(playerIndex);
		}

		private void OnUserChangedStatus(PlayerStatus status)
		{
			var readyCount = Players.Count(player => player.UserStatus.Value == PlayerStatus.Ready);

			OnUserReadyChange?.Invoke(readyCount);
		}

		public override string ToString()
		{
			var sb = new StringBuilder("Lobby : ");
			sb.AppendLine(LobbyName.Value);
			sb.Append("ID: ");
			sb.AppendLine(LobbyID.Value);
			sb.Append("Code: ");
			sb.AppendLine(LobbyCode.Value);
			sb.Append("Locked: ");
			sb.AppendLine(Locked.Value.ToString());
			sb.Append("Private: ");
			sb.AppendLine(Private.Value.ToString());
			sb.Append("AvailableSlots: ");
			sb.AppendLine(AvailableSlots.Value.ToString());
			sb.Append("Max Players: ");
			sb.AppendLine(MaxPlayerCount.Value.ToString());
			sb.Append("LocalLobbyState: ");
			sb.AppendLine(LocalLobbyState.Value.ToString());
			sb.Append("Lobby LocalLobbyState Last Edit: ");
			sb.AppendLine(new DateTime(LastUpdated.Value).ToString());
			sb.Append("RelayCode: ");
			sb.AppendLine(RelayCode.Value);

			return sb.ToString();
		}
	}
}