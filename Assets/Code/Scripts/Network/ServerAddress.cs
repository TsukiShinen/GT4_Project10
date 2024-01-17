using System;

namespace Network
{
    /// <summary>
    ///     Just for displaying the anonymous Relay IP.
    /// </summary>
    public class ServerAddress : IEquatable<ServerAddress>
	{
		public ServerAddress(string ip, int port)
		{
			IP = ip;
			Port = port;
		}

		public string IP { get; }

		public int Port { get; }

		public bool Equals(ServerAddress other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return IP == other.IP && Port == other.Port;
		}

		public override string ToString()
		{
			return $"{IP}:{Port}";
		}

#pragma warning disable CS0659
		public override bool Equals(object obj)
#pragma warning restore CS0659
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((ServerAddress)obj);
		}
	}
}