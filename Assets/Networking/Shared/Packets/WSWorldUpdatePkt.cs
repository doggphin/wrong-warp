using LiteNetLib.Utils;
using System.Collections.Generic;

namespace Networking.Shared {

    public class WWorldUpdateTick {
        public Dictionary<int, INetSerializable> entityUpdates;
        public List<INetSerializable> generalUpdates;
    }

    class WSWorldUpdatePkt : INetSerializable {
        public WWorldUpdateTick[] worldUpdateTicks;

        public void Deserialize(NetDataReader reader) => throw new System.NotImplementedException();
        public void Serialize(NetDataWriter writer) => throw new System.NotImplementedException();
    }
}