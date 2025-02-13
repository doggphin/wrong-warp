using LiteNetLib.Utils;
using UnityEngine;

public abstract class BaseEntityTag : MonoBehaviour, INetSerializable {
    protected static EntityTagType TagType { get; }

    public abstract void Deserialize(NetDataReader reader);

    public abstract void Serialize(NetDataWriter writer);
}