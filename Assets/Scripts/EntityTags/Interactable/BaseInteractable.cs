using System;
using LiteNetLib;
using Networking.Shared;
using UnityEngine;


public abstract class BaseInteractable : MonoBehaviour
{
    public abstract string GetHoverText();
    public abstract InteractableIconType GetIconType();
    
    public virtual void InteractStart(BaseEntity interacter) {}
}
