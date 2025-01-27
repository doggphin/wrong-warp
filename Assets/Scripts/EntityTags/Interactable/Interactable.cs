using LiteNetLib.Utils;

public enum InteractableType {
    Takeable,
    Lootable,
    Mountable,
}

public abstract class BaseInteractable : INetSerializable
{
    public abstract string GetHoverText();
    public abstract InteractableIconType GetIconType();
    public abstract InteractableType GetInteractableType();

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(GetHoverText());
        writer.Put((byte)GetIconType());
    }

    public void Deserialize(NetDataReader reader)
    {
        throw new System.NotImplementedException();
    }
}
