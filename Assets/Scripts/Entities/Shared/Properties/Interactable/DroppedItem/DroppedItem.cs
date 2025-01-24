using Inventories;
using UnityEngine.Assertions.Must;

public class DroppedItem : Interactable
{
    private SlottedItem item;

    public override string GetHoverText() => item?.BaseItemRef.name ?? "No item found";

    public override InteractableIconType GetIconType() => InteractableIconType.Take;

    public override void InteractStart()
    {
        throw new System.NotImplementedException();
    }
}