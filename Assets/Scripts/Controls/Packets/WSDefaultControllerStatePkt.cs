using LiteNetLib.Utils;
using Networking.Shared;
using UnityEngine;

public class WSDefaultControllerStatePkt : INetPacketForClient {
    public Vector3 velocity;
    public bool canDoubleJump;
    public WInputsSerializable previousInputs;
    public Vector2 boundedRotatorRotation;
    public Vector3 position;    // This is really just a convenience; could probably check transform update instead

    public bool ShouldCache => true;

    public void Deserialize(NetDataReader reader)
    {
        velocity = reader.GetVector3();
        canDoubleJump = reader.GetBool();
        previousInputs = new WInputsSerializable(); previousInputs.Deserialize(reader);
        boundedRotatorRotation = reader.GetVector2();
        position = reader.GetVector3();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(WPacketType.SDefaultControllerState);

        writer.Put(velocity);
        writer.Put(canDoubleJump);
        previousInputs.Serialize(writer);
        writer.Put(boundedRotatorRotation);
        writer.Put(position);
    }

    public void ApplyOnClient(int tick)
    {
        throw new System.NotImplementedException();
    }
}