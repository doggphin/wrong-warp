using System.Collections.Generic;

namespace Inventories {
    public static class InventoryTemplateLookup {
        private static readonly Dictionary<InventoryTemplateType, InventoryTemplate> templates = new() {
            { InventoryTemplateType.Player, new InventoryTemplate(23, true,
                // 0, 1, 2, 3 are hotbar
                (4, InventoryTemplate.GenerateItemRestrictionFlags(ItemClassification.Helmet)),
                (5, InventoryTemplate.GenerateItemRestrictionFlags(ItemClassification.Chestplate)),
                (6, InventoryTemplate.GenerateItemRestrictionFlags(ItemClassification.Boots)))
            },
            { InventoryTemplateType.LootChest, new InventoryTemplate(4, false) }
        };

        public static InventoryTemplate GetTemplate(InventoryTemplateType templateType) => templates[templateType];
    }
}