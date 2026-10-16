using UnityEngine;

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
        if (player == null)
        {
            Debug.LogWarning("Cannot open the Bank without a player.");
            return;
        }

        if (!TryBindBankInventory()) return;

        player.OpenOtherInventory(inventoryComponent);
    }
}