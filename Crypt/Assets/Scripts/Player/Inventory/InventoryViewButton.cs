using UnityEngine;
using UnityEngine.UI;

public enum InventoryViewTarget
{
    Player,
    Bank
}

[RequireComponent(typeof(Button))]
public class InventoryViewButton : MonoBehaviour
{
    [SerializeField] private PlayerInventoryUI inventoryUI;
    [SerializeField] private InventoryViewTarget targetInventory;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (inventoryUI == null) inventoryUI = GetComponentInParent<PlayerInventoryUI>(true);
    }

    private void OnEnable()
    {
        button.onClick.RemoveListener(SwitchInventory);
        button.onClick.AddListener(SwitchInventory);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(SwitchInventory);
    }

    public void SwitchInventory()
    {
        if (inventoryUI == null)
        {
            Debug.LogError("InventoryViewButton could not find PlayerInventoryUI.");
            return;
        }

        if (PlayerManager.Instance == null)
        {
            Debug.LogError("InventoryViewButton could not find PlayerManager.");
            return;
        }

        Inventory playerInventory = PlayerManager.Instance.PlayerInventory;
        Inventory bankInventory = PlayerManager.Instance.BankInventory;

        switch (targetInventory)
        {
            case InventoryViewTarget.Player:
                inventoryUI.BindInventory(playerInventory, bankInventory);
                break;

            case InventoryViewTarget.Bank:
                inventoryUI.BindInventory(bankInventory, playerInventory);
                break;
        }
    }
}