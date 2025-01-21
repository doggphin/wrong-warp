using System;
using LiteNetLib.Utils;

///<summary> Implement from NetPacketForClient<T> instead! </summary>
public abstract class NetPacketForClient : INetSerializable {
    ///<summary>
    ///<para> If true, client will wait to run this packet until when it occured on the server. </para>
    ///<para> If false, client will run this packet immediatedly. </para>
    ///</summary>
    public abstract bool ShouldCache { get; }
    ///<summary> Intended to be overwritten by NetPacketForClient(T) </summary>
    protected abstract void BroadcastApply(int tick);
    ///<summary> Applies this packet on the client. </summary>
    public void ApplyOnClient(int tick) => BroadcastApply(tick);
    public abstract void Deserialize(NetDataReader reader);
    public abstract void Serialize(NetDataWriter writer);
}

public abstract class NetPacketForClient<T> : NetPacketForClient where T : NetPacketForClient<T> {
    public static Action<int, T> Apply;
    public static Action<T> ApplyUnticked;
    private T GetBaseClass() => (T)this;
    protected override void BroadcastApply(int tick) {
        Apply?.Invoke(tick, GetBaseClass());
        ApplyUnticked?.Invoke(GetBaseClass());
    }
}