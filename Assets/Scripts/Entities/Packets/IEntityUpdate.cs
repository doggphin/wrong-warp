public interface INetEntityUpdatePacketForClient : INetPacketForClient {
    ///<summary> Used in packets where entity ID can be inferred without explicitly being included in the packet </summary>
    public int CEntityId { get; set; }
}