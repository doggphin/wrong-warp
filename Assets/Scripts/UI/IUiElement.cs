public interface IUiElement {
    public void Open();
    public void Close();
    public void Toggle();
    public bool IsOpen { get; }
    public bool RequiresMouse { get; }
}