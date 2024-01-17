using System;
using Unity.Collections;
using Unity.Netcode;

public struct PlayerData : IEquatable<PlayerData>, INetworkSerializable
{
	public ulong ClientId;
	public FixedString64Bytes PlayerName;

	public bool Equals(PlayerData other)
	{
		return ClientId == other.ClientId && PlayerName == other.PlayerName;
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref ClientId);
		serializer.SerializeValue(ref PlayerName);
	}
}