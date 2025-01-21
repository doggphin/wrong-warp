///<summary> This is smelly. This should be a subclass of NetPacketForClient but I don't know how to do it </summary>
public interface IEntityUpdate {
    ///<summary> Used in packets where entity ID can be inferred without explicitly being included in the packet </summary>
    public int CEntityId { get; set; }
}