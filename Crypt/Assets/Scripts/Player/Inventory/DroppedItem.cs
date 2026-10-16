using UnityEngine;

public class DroppedItem : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemCatalog itemCatalog;

    private string interactText = "Pickup Item";
    public string InteractText => interactText;

    private GeneratedItem associatedItem;
    private int quantity;
    private ItemDefinition itemDefinition;
    [SerializeField] private ParticleSystem lootBeam;

    [Header("Rarity Beam Gradients")]
    [SerializeField] private Gradient commonGradient = new();
    [SerializeField] private Gradient uncommonGradient = new();
    [SerializeField] private Gradient rareGradient = new();
    [SerializeField] private Gradient epicGradient = new();
    [SerializeField] private Gradient legendaryGradient = new();
    [SerializeField] private Gradient mythicGradient = new();
    [SerializeField] private Gradient secretGradient = new();

    [SerializeField] private Vector3 lootBeamWorldRotation = new Vector3(-90f, 0f, 0f);

    private void LateUpdate()
    {
        if (lootBeam == null) return;
        lootBeam.transform.rotation = Quaternion.Euler(lootBeamWorldRotation);
    }
    public bool AssignItem(GeneratedItem item, int quantity)
    {
        if (item == null)
        {
            Debug.LogWarning("Cannot assign a null item to DroppedItem.");
            return false;
        }

        if (quantity <= 0)
        {
            Debug.LogWarning("Dropped item quantity must be greater than 0.");
            return false;
        }

        if (itemCatalog == null)
        {
            Debug.LogError("DroppedItem does not have an ItemCatalog assigned.");
            return false;
        }

        if (!itemCatalog.TryGetItemDefinition(item.definitionID, out ItemDefinition definition))
        {
            Debug.LogWarning($"Could not find item definition '{item.definitionID}'.");
            return false;
        }

        associatedItem = item;
        itemDefinition = definition;
        this.quantity = quantity;

        interactText = $"Pick up {itemDefinition.ItemName}";

        SetLootBeamColour(itemDefinition.ItemRarity);



        return true;
    }

    public void Interact(PlayerController player)
    {
        if (player == null)
        {
            Debug.LogWarning("Cannot pick up an item without a player.");
            return;
        }

        if (associatedItem == null)
        {
            Debug.LogWarning("DroppedItem has not been assigned an item.");
            return;
        }

        if (quantity <= 0)
        {
            Debug.LogWarning("DroppedItem has an invalid quantity.");
            return;
        }

        if (!player.TryGetComponent(out InventoryComponent inventoryComponent))
        {
            Debug.LogWarning("Player has no InventoryComponent.");
            return;
        }

        if (inventoryComponent.TryAdd(associatedItem, quantity))
        {
            Debug.Log($"Picked up {quantity} {itemDefinition.ItemName}.");
            lootBeam.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            lootBeam.Clear();
            Destroy(gameObject);
        }
    }

    private void SetLootBeamColour(Rarity rarity)
    {
        if (lootBeam == null) return;

        Gradient selectedGradient = rarity switch
        {
            Rarity.Common => commonGradient,
            Rarity.Uncommon => uncommonGradient,
            Rarity.Rare => rareGradient,
            Rarity.Epic => epicGradient,
            Rarity.Legendary => legendaryGradient,
            Rarity.Mythic => mythicGradient,
            Rarity.Secret => secretGradient,
            _ => commonGradient
        };

        ParticleSystem.MainModule main = lootBeam.main;
        main.startColor = Color.white;

        ParticleSystem.ColorOverLifetimeModule colourOverLifetime = lootBeam.colorOverLifetime;
        colourOverLifetime.enabled = true;
        colourOverLifetime.color = new ParticleSystem.MinMaxGradient(selectedGradient);
    }
}