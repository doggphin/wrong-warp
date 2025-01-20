using LiteNetLib.Utils;

public interface INetPacketForClient : INetSerializable {
    ///<summary>
    ///<para> If true, client will wait to run this packet until when it occured on the server. </para>
    ///<para> If false, client will run this packet immediatedly. </para>
    ///</summary>
    public bool ShouldCache { get; }

    public void ApplyOnClient(int tick);
}