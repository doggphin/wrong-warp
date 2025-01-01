using UnityEngine;

public class BaseUiElement : MonoBehaviour, IUiElement
{
    public bool IsOpen { get; protected set; }
    public bool RequiresMouse { get; protected set; }
    
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
