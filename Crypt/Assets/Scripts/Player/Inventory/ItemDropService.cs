using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDropService", menuName = "Inventory/Item Drop Service")]
// Automically converts an inventory entry into a launched world pickup.
public class ItemDropService : ScriptableObject
{
    [SerializeField] private ItemCatalog itemCatalog;
    [SerializeField] private DroppedItem droppedItemPrefab;

    [Header("Spawn")]
    [SerializeField] private float forwardOffset = 0.75f;
    [SerializeField] private float upwardOffset = 1f;
    [SerializeField] private float positionSpread = 0.2f;

    [Header("Launch")]
    [SerializeField] private Vector2 forwardForce = new Vector2(3f, 6f);
    [SerializeField] private Vector2 upwardForce = new Vector2(3f, 5f);
    [SerializeField] private float horizontalSpread = 25f;
    [SerializeField] private float torque = 5f;

    public bool TryDrop(Inventory sourceInventory, GeneratedItem item, int quantity, Transform dropOrigin, EquipmentLoadout equipmentLoadout = null)
    {
        if (dropOrigin == null)
        {
            Debug.LogWarning("Cannot drop an item without a drop origin.");
            return false;
        }

        return TryDrop(sourceInventory, item, quantity, dropOrigin.position, dropOrigin.forward, equipmentLoadout);
    }

    public bool TryDrop(Inventory sourceInventory, GeneratedItem item, int quantity, Vector3 origin, Vector3 direction, EquipmentLoadout equipmentLoadout = null)
    {
        // Validate the complete operation before removing anything from the source inventory.
        if (sourceInventory == null || item == null || quantity <= 0) return false;

        if (itemCatalog == null || droppedItemPrefab == null)
        {
            Debug.LogError("ItemDropService is missing its ItemCatalog or DroppedItem prefab.");
            return false;
        }

        if (!itemCatalog.TryGetItemDefinition(item.definitionID, out ItemDefinition definition))
        {
            Debug.LogWarning($"Could not find item definition '{item.definitionID}'.");
            return false;
        }

        if (!sourceInventory.TryFindByInstanceID(item.instanceID, out InventoryEntry sourceEntry))
        {
            Debug.LogWarning($"Inventory does not contain instance ID {item.instanceID}.");
            return false;
        }

        if (!sourceInventory.CanRemove(item, quantity, definition)) return false;

        if (equipmentLoadout != null && equipmentLoadout.IsEquipped(item.instanceID))
        {
            Debug.LogWarning($"{definition.ItemName} cannot be dropped while equipped.");
            return false;
        }

        if (droppedItemPrefab.GetComponent<Rigidbody>() == null)
        {
            Debug.LogError("DroppedItem prefab does not have a Rigidbody.");
            return false;
        }

        GeneratedItem itemToDrop = item;

        if (definition.StackLimit > 1 && quantity < sourceEntry.quantity)
        {
            // A partial stack receives a new identity while the source stack keeps its existing ID.
            itemToDrop = CreateSplitStackItem(item);
        }

        Vector3 flatDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
        if (flatDirection.sqrMagnitude <= 0.001f) flatDirection = Vector3.forward;

        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * positionSpread;
        Vector3 spawnPosition = origin + flatDirection * forwardOffset + Vector3.up * upwardOffset + new Vector3(randomOffset.x, 0f, randomOffset.y);

        DroppedItem droppedItem = Instantiate(droppedItemPrefab, spawnPosition, UnityEngine.Random.rotation);

        if (!droppedItem.AssignItem(itemToDrop, quantity))
        {
            Destroy(droppedItem.gameObject);
            return false;
        }

        if (!droppedItem.TryGetComponent(out Rigidbody body))
        {
            Destroy(droppedItem.gameObject);
            return false;
        }

        if (!sourceInventory.TryRemove(item, quantity, definition))
        {
            // Leave the inventory unchanged if the spawned pickup cannot be committed.
            Destroy(droppedItem.gameObject);
            return false;
        }

        Vector3 spreadDirection = Quaternion.AngleAxis(UnityEngine.Random.Range(-horizontalSpread, horizontalSpread), Vector3.up) * flatDirection;
        Vector3 launchForce = spreadDirection * UnityEngine.Random.Range(forwardForce.x, forwardForce.y) + Vector3.up * UnityEngine.Random.Range(upwardForce.x, upwardForce.y);

        body.AddForce(launchForce, ForceMode.Impulse);
        body.AddTorque(UnityEngine.Random.insideUnitSphere * torque, ForceMode.Impulse);

        Debug.Log($"Dropped {quantity} {definition.ItemName}.");
        return true;
    }

    private GeneratedItem CreateSplitStackItem(GeneratedItem sourceItem)
    {
        // Copy bonus records so the split item does not share mutable list entries with its source.
        List<GeneratedBonus> copiedBonuses = new();

        if (sourceItem.bonusStats != null)
        {
            foreach (GeneratedBonus bonus in sourceItem.bonusStats)
            {
                if (bonus == null) continue;
                copiedBonuses.Add(new GeneratedBonus(bonus.statType, bonus.element));
            }
        }

        return new GeneratedItem(Guid.NewGuid().ToString(), sourceItem.definitionID, copiedBonuses);
    }
}
