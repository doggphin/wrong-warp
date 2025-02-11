namespace Inventories {
    // If more than 32 values are used here, modify InventoryTemplate to use a long for bitflags
    public enum ItemCategory : int {
        Helmet = 1 << 1,
        Chestplate = 1 << 2,
        Boots = 1 << 3,
        Gun = 1 << 4,
        Sword = 1 << 5,
        Consumable = 1 << 6,

        Everything = int.MaxValue
    }
}