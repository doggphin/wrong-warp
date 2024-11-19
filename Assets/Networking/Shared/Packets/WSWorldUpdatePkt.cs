using LiteNetLib.Utils;
using System.Collections.Generic;

namespace Networking.Shared {
    class WSWorldUpdatePkt : INetSerializable {
        public List<INetSerializable>[] generalUpdates;
        public Dictionary<int, List<INetSerializable>>[] entityUpdates;

        public void Deserialize(NetDataReader reader) {

        }

        public void Serialize(NetDataWriter writer) => throw new System.NotImplementedException();
    }
}