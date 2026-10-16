using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventorySelectionPanel : MonoBehaviour
{
    private InventoryEntry entry;
    private Inventory sourceInventory;
    private Inventory destinationInventory;
    private PlayerInventoryUI playerUI;

    [SerializeField] private TransferService transferService;

    public void InitializeSelectPanel(InventoryEntry selectedEntry, Inventory source, Inventory destination, PlayerInventoryUI ui)
    {
        entry = selectedEntry;
        sourceInventory = source;
        destinationInventory = destination;
        playerUI = ui;
    }

    private RectTransform panelRect;
    private Camera uiCamera;
    private int openedFrame;

    private void Awake()
    {
        panelRect = GetComponent<RectTransform>();
        openedFrame = Time.frameCount;

        Canvas canvas = GetComponentInParent<Canvas>();

        uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;
    }

    private void Update()
    {
        // Ignore the click that created this panel.
        if (Time.frameCount == openedFrame) return;
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        bool clickedInside = RectTransformUtility.RectangleContainsScreenPoint(
            panelRect,
            mousePosition,
            uiCamera);

        if (!clickedInside)
        {
            Destroy(gameObject);
        }
    }

    public void TransferButton()
    {
        if (entry?.item == null || transferService == null) return;
        if (sourceInventory == null || destinationInventory == null) return;
        if (sourceInventory == destinationInventory) return;

        // Transfer the whole selected stack.
        if (transferService.TryTransfer(
            sourceInventory,
            destinationInventory,
            entry.item,
            entry.quantity))
        {
            Destroy(gameObject);
            playerUI.Refresh();
        }
    }
}
