using Inventories;
using Networking.Shared;

public abstract class BaseInventoryActionListener<T> : BaseSingleton<T> where T : BaseInventoryActionListener<T> {
    protected override void Awake()
    {
        InventoryUiManager.RequestToMoveItem += HandleMoveSlotRequest;
        base.Awake();
    }

    protected override void OnDestroy()
    {
        InventoryUiManager.RequestToMoveItem -= HandleMoveSlotRequest;
        base.OnDestroy();
    }

    protected abstract void HandleMoveSlotRequest(CMoveSlotRequestPkt request);
}