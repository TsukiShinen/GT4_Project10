using System;

namespace Network
{
    [Flags]
    public enum PlayerStatus
    {
        None = 0,
        Connecting = 1,
        Lobby = 2,
        Ready = 4,
        InGame = 8,
        Menu = 16
    }
    
    [Serializable]
    public class LocalPlayer
    {
        public CallbackValue<bool> IsHost = new (false);
        public CallbackValue<string> DisplayName = new ("");
        public CallbackValue<PlayerStatus> UserStatus = new (PlayerStatus.None);
        public CallbackValue<string> ID = new ("");
        public CallbackValue<int> Index = new (0);

        public DateTime LastUpdated;

        public LocalPlayer(string id, int index, bool isHost, string displayName = default,
            PlayerStatus status = default)
        {
            ID.Value = id;
            IsHost.Value = isHost;
            Index.Value = index;
            DisplayName.Value = displayName;
            UserStatus.Value = status;
        }

        public void ResetState()
        {
            IsHost.Value = false;
            UserStatus.Value = PlayerStatus.Menu;
        }
    }
}