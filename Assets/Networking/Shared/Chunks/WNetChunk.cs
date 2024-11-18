using System.Collections.Generic;

namespace Networking.Shared {
    public class WNetChunk {
        public HashSet<WNetEntity> presentEntities { get; set; }

        public void Load() {
            presentEntities = new();
        }

        public void Unload() {

        }

        public void Save() {

        }
    }
}
