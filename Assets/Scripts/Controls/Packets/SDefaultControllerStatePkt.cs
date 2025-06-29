using LiteNetLib.Utils;
using Networking.Client;
using Networking.Shared;
using UnityEngine;

public class SDefaultControllerStatePkt : SPacket<SDefaultControllerStatePkt> {
    public Vector3 velocity;
    public bool canDoubleJump;
    public InputsSerializable previousInputs;
    public Vector2 boundedRotatorRotation;
    public Vector3 position;    // This is really just a convenience; could probably check transform update instead

    public override void Deserialize(NetDataReader reader)
    {
        velocity = reader.GetVector3();
        canDoubleJump = reader.GetBool();

        previousInputs = new InputsSerializable();
        previousInputs.Deserialize(reader);

        boundedRotatorRotation = reader.GetVector2();
        position = reader.GetVector3();
    }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(PacketIdentifier.SDefaultControllerState);

        writer.Put(velocity);
        writer.Put(canDoubleJump);
        previousInputs.Serialize(writer);
        writer.Put(boundedRotatorRotation);
        writer.Put(position);
    }

    public override bool ShouldCache => false;
}