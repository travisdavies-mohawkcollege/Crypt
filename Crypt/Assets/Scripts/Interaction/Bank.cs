using UnityEngine;

// Exposes the persistent bank inventory through the same UI used by other inventory containers.
public class Bank : MonoBehaviour, IInteractable
{
    [SerializeField] private InventoryComponent inventoryComponent;

    private string interactText = "Open Bank";
    public string InteractText => interactText;

    private void Awake()
    {
        if (inventoryComponent == null) inventoryComponent = GetComponent<InventoryComponent>();
    }

    private void Start()
    {
        TryBindBankInventory();
    }

    private bool TryBindBankInventory()
    {
        // The scene component is temporary, but the actual bank data belongs to PlayerManager.
        if (inventoryComponent == null)
        {
            Debug.LogError("Bank does not have an InventoryComponent.");
            return false;
        }

        if (inventoryComponent.HasInventory) return true;

        if (PlayerManager.Instance == null)
        {
            Debug.LogError("Bank could not find PlayerManager.");
            return false;
        }

        inventoryComponent.BindInventory(PlayerManager.Instance.BankInventory);
        return inventoryComponent.HasInventory;
    }

    public void Interact(PlayerController player)
    {
        // Opening the bank binds both inventories so the UI can transfer items in either direction.
        if (player == null)
        {
            Debug.LogWarning("Cannot open the Bank without a player.");
            return;
        }

        if (!TryBindBankInventory()) return;

        player.OpenOtherInventory(inventoryComponent);
    }
}
