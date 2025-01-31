using System;
using LiteNetLib;
using LiteNetLib.Utils;

///<summary> Implement from NetPacketForClient<T> instead! </summary>
public abstract class BasePacket : INetSerializable {
    ///<summary>
    ///<para> This property is only used in reading packets received on the client
    ///<para> If true, client will wait to run this packet until when it occured on the server. </para>
    ///<para> If false, client will run this packet immediatedly. </para>
    ///</summary>
    public virtual bool ShouldCache { get => false; }
    /// <summary>
    /// If this packet is received late, should it be ran anyways?
    /// </summary>
    public virtual bool ShouldRunEvenIfLate { get => false; }

    ///<summary> This field is only used in reading packets received on the server </summary>
    public NetPeer Sender { get; set; }

    ///<summary> Intended to be overwritten by NetPacketForClient(T) </summary>
    protected abstract void OverridableBroadcastApply(int tick);

    ///<summary> Applies this packet on the client. </summary>
    public void BroadcastApply(int tick) => OverridableBroadcastApply(tick);

    public abstract void Deserialize(NetDataReader reader);
    public abstract void Serialize(NetDataWriter writer);
}

///<summary> Represents a packet received on a client. </summary>
public abstract class SPacket<T> : BasePacket where T : SPacket<T> {
    public static Action<int, T> Apply;
    public static Action<T> ApplyUnticked;
    private T GetBaseClass() => (T)this;
    protected override void OverridableBroadcastApply(int tick) {
        Apply?.Invoke(tick, GetBaseClass());
        ApplyUnticked?.Invoke(GetBaseClass());
    }
}

///<summary> Represents a packet received on the server. </summary>
public abstract class CPacket<T> : BasePacket where T : CPacket<T> {
    public static Action<int, T, NetPeer> Apply;
    public static Action<T, NetPeer> ApplyUnticked;
    private T GetBaseClass() => (T)this;
    protected override void OverridableBroadcastApply(int tick)
    {
        Apply?.Invoke(tick, GetBaseClass(), Sender);
        ApplyUnticked?.Invoke(GetBaseClass(), Sender);
    }
}

