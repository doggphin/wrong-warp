using System;
using Unity.VisualScripting;
using UnityEngine;

public abstract class BaseUiElement<T> : BaseSingleton<T>, IUiElement where T : BaseUiElement<T>
{
    public bool IsOpen { get; protected set; } = false;
    public abstract bool RequiresMouse { get; }
    public abstract bool AllowsMovement { get; }


    protected override void Awake()
    {
        gameObject.SetActive(false);
        base.Awake();
    }
    public virtual void Close()
    {
        IsOpen = false;
        gameObject.SetActive(false);
    }


    public virtual void Open()
    {
        IsOpen = true;
        gameObject.SetActive(true);
    }


    public virtual void Toggle()
    {
        if(IsOpen)
            Close();
        else
            Open();
    }
}