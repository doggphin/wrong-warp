using System.Collections.Generic;

namespace Inventories {
    public class InventoryTemplateLookup : BaseLookup<InventoryTemplateType, InventoryTemplateSO>
    {
        protected override string ResourcesPath => "InventoryTemplates";
    }
}