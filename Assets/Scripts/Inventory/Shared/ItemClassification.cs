namespace Inventories {
    // If more than 32 values are used here, modify InventoryTemplate to use a long for bitflags
    public enum ItemClassification : int {
        Helmet = 0,
        Chestplate = 1,
        Boots = 2,
        Gun = 3,
        Sword = 4,
        Consumable = 5,
    }
}