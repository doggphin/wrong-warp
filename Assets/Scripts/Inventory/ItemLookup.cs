using UnityEngine;

namespace Inventory {
    public class ItemLookup : BaseSingleton<ItemLookup> {
        private static BaseLookup<BaseItemSO> baseLookup;

        public static void Init() {
            baseLookup = new();
            baseLookup.Init("Items");
        }

        public static BaseItemSO GetById(ItemType itemType) {
            return baseLookup.GetById((int)itemType);
        }
    }
}