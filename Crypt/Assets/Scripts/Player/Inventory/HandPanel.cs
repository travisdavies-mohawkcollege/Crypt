using UnityEngine;
using UnityEngine.InputSystem;

// Temporary slot-choice popup shown when an item can use either hand.
public class HandPanel : MonoBehaviour
{
    private EquipmentSlot equipmentSlot;
    private InventorySelectionPanel selectionPanel;
    [SerializeField] private EquipmentService equipmentService;
    private RectTransform panelRect;
    private Camera uiCamera;
    private int openedFrame;

    private void Awake()
    {
        panelRect = GetComponent<RectTransform>();
        openedFrame = Time.frameCount;

        Canvas canvas = GetComponentInParent<Canvas>();
        uiCamera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    private void Update()
    {
        // Ignore the opening click, then close when a later click lands outside this panel.
        if (Time.frameCount == openedFrame) return;
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        if (!RectTransformUtility.RectangleContainsScreenPoint(panelRect, mousePosition, uiCamera))
        {
            selectionPanel.gameObject.SetActive(true);
            Destroy(gameObject);
        }
    }

    public void IntializeHandPanel(InventorySelectionPanel panel)
    {
        selectionPanel = panel;
    }
    public void MainHandButton()
    {
        // EquipmentService performs the validation before either popup is destroyed.
        equipmentService.TryEquip(selectionPanel.sourceInventory, PlayerManager.Instance.PlayerEquipment, selectionPanel.entry.item, EquipmentSlot.MainHand);
        Destroy(selectionPanel.gameObject);
        Destroy(gameObject);
    }

    public void OffHandButton()
    {
        equipmentService.TryEquip(selectionPanel.sourceInventory, PlayerManager.Instance.PlayerEquipment, selectionPanel.entry.item, EquipmentSlot.OffHand);
        Destroy(selectionPanel.gameObject);
        Destroy(gameObject);
    }
}
