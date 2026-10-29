using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

// Context menu that exposes only the actions valid for the selected inventory entry.
public class InventorySelectionPanel : MonoBehaviour
{
    public InventoryEntry entry {get; private set;}
    public Inventory sourceInventory {get; private set;}
    public Inventory destinationInventory {get; private set;}
    private ItemDefinition itemDefinition;
    private PlayerInventoryUI playerUI;

    [SerializeField] private TransferService transferService;
    [SerializeField] private GameObject transferButton;
    [SerializeField] private EquipmentService equipmentService;
    [SerializeField] private GameObject equipButton;
    [SerializeField] private ItemDropService itemDropService;
    [SerializeField] private GameObject dropButton;
    [SerializeField] private GameObject destroyButton;
    [SerializeField] private HandPanel handPanel;

    private RectTransform panelRect;
    private Camera uiCamera;
    private int openedFrame;

    public void InitializeSelectPanel(InventoryEntry selectedEntry, Inventory source, Inventory destination, PlayerInventoryUI ui, ItemDefinition definition)
    {
        // Cache the exact source and optional destination so actions operate on the displayed inventories.
        entry = selectedEntry;
        sourceInventory = source;
        destinationInventory = destination;
        playerUI = ui;
        itemDefinition = definition;

        bool isPlayerInventory = PlayerManager.Instance != null && sourceInventory == PlayerManager.Instance.PlayerInventory;
        bool isEquipped = isPlayerInventory && entry?.item != null && PlayerManager.Instance.PlayerEquipment.IsEquipped(entry.item.instanceID);

        if (transferButton != null) transferButton.SetActive(sourceInventory != null && destinationInventory != null && sourceInventory != destinationInventory && !isEquipped);
        if (equipButton != null) equipButton.SetActive(isPlayerInventory && definition != null && definition.EquipSlot.Count != 0 && !isEquipped);
        if (dropButton != null) dropButton.SetActive(isPlayerInventory && !isEquipped);
        if (destroyButton != null) destroyButton.SetActive(isPlayerInventory && !isEquipped);
    }

    private void Awake()
    {
        panelRect = GetComponent<RectTransform>();
        openedFrame = Time.frameCount;

        Canvas canvas = GetComponentInParent<Canvas>();
        uiCamera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    private void Update()
    {
        // Ignore the opening click, then dismiss the context menu when clicking elsewhere.
        if (Time.frameCount == openedFrame) return;
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        bool clickedInside = RectTransformUtility.RectangleContainsScreenPoint(panelRect, mousePosition, uiCamera);

        if (!clickedInside)
        {
            Destroy(gameObject);
        }
    }

    public void TransferButton()
    {
        // Equipped player items cannot leave the inventory until they are unequipped.
        if (entry?.item == null || transferService == null) return;
        if (sourceInventory == null || destinationInventory == null) return;
        if (sourceInventory == destinationInventory) return;

        bool isPlayerInventory = PlayerManager.Instance != null && sourceInventory == PlayerManager.Instance.PlayerInventory;
        if (isPlayerInventory && PlayerManager.Instance.PlayerEquipment.IsEquipped(entry.item.instanceID)) return;

        if (transferService.TryTransfer(sourceInventory, destinationInventory, entry.item, entry.quantity))
        {
            Destroy(gameObject);
        }
    }

    public void EquipButton()
    {
        // Multi-slot items open a second popup so the player can choose the destination hand.
        if (entry?.item == null || equipmentService == null || PlayerManager.Instance == null) return;
        if (sourceInventory != PlayerManager.Instance.PlayerInventory) return;



        if (itemDefinition.EquipSlot.Count != 1)
        {
            HandPanel newHandPanel = Instantiate(handPanel, transform.parent).GetComponent<HandPanel>();
            newHandPanel.IntializeHandPanel(this);
            gameObject.SetActive(false);
            
        }
        else
        {
            if (equipmentService.TryEquip(sourceInventory, PlayerManager.Instance.PlayerEquipment, entry.item, itemDefinition.EquipSlot[0]))
            {
                Destroy(gameObject);
            }
        }


    }

    public void DropButton()
    {
        // ItemDropService removes the entry only after the world pickup is created successfully.
        if (entry?.item == null || itemDropService == null || PlayerManager.Instance == null) return;
        if (PlayerManager.Instance.player == null) return;
        if (sourceInventory != PlayerManager.Instance.PlayerInventory) return;

        if (itemDropService.TryDrop(sourceInventory, entry.item, entry.quantity, PlayerManager.Instance.player.transform, PlayerManager.Instance.PlayerEquipment))
        {
            Destroy(gameObject);
        }
    }

    public void DestroyButton()
    {
        // Destruction is permanent and is therefore limited to unequipped player-owned items.
        if (entry?.item == null || itemDefinition == null || sourceInventory == null) return;
        if (PlayerManager.Instance == null) return;
        if (sourceInventory != PlayerManager.Instance.PlayerInventory) return;

        if (PlayerManager.Instance.PlayerEquipment.IsEquipped(entry.item.instanceID))
        {
            Debug.LogWarning("Cannot destroy an equipped item.");
            return;
        }

        if (sourceInventory.TryRemove(entry.item, entry.quantity, itemDefinition))
        {
            Debug.Log($"Destroyed {entry.quantity} {itemDefinition.ItemName}.");
            Destroy(gameObject);
        }
    }
}
