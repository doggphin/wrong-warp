namespace Inventories {
    // If more than 32 values are used here, modify InventoryTemplate to use a long for bitflags
    public enum ItemCategory : int {
        Helmet = 1 << 1,
        Chestplate = 1 << 2,
        Leggings = 1 << 3,
        Boots = 1 << 4,
        Gun = 1 << 5,
        Sword = 1 << 6,
        Consumable = 1 << 7,

        Everything = int.MaxValue
    }
}