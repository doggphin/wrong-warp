using Inventories;
using UnityEngine.Assertions.Must;

public class InteractableTakeable : BaseInteractable
{
    private SlottedItem item;

    public override string GetHoverText() => $"{item?.stackSize.ToString() ?? "N/A"} {item?.BaseItemRef.name ?? "No item found"}";

    public override InteractableIconType GetIconType() => InteractableIconType.Take;

    public override InteractableType GetInteractableType() => InteractableType.Takeable;

    public void InteractStart()
    {
        throw new System.NotImplementedException();
    }
}