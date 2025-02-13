using System;
using Controllers.Shared;
using Inventories;
using LiteNetLib;
using Networking.Shared;
using UnityEngine;


public class InteractableTakeable : BaseInteractable
{   
    public static Action<InteractableTakeable, BaseEntity> InteractedStart;
    public SlottedItem item;

    public override string GetHoverText() => $"{item?.stackSize.ToString() ?? "N/A"} {item?.BaseItemRef.name ?? "No item found"}";
    public override InteractableIconType GetIconType() => InteractableIconType.Take;

    public override void InteractStart(BaseEntity interactor) {
        Debug.Log("It's interacting time!");
        InteractedStart?.Invoke(this, interactor);
    }
}