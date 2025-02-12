using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine.DedicatedServer;

public class SBaseHandlerSingleton<T> : BaseSingleton<SBaseHandlerSingleton<T>> {
    List<(ref Action<object>, ref Action<object>) handlers = new();
    protected void AddHandler<PacketT>(ref Action<SPacket<PacketT>> action, ref Action<SPacket<PacketT>> function) where PacketT : SPacket<PacketT> {
        action += function;
    }

    protected
}