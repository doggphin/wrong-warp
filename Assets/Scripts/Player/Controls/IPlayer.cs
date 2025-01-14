using LiteNetLib.Utils;
using Networking.Shared;
using UnityEngine;

namespace Controllers.Shared {
    public interface IPlayer
    {
        public void EnablePlayer();
        public void DisablePlayer();

        public void Control(WInputsSerializable inputs, int onTick);

        public void AddRotationDelta(Vector2 delta);
        public Vector2? PollLook();
        public Vector2 GetLook();

        public void SetRotation(Vector2 look);
        public Vector2 GetRotation();

        public void RollbackToTick(int tick);
    }
}
