using LiteNetLib.Utils;

public interface INetPacketForClient : INetSerializable {
    public bool ShouldCache { get; }
    public void ApplyOnClient(int tick);
}