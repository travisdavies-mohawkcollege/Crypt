using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemCatalog", menuName = "Items/Item Catalog")]
// Resolves stable definition IDs into immutable ItemDefinition assets.
public class ItemCatalog : ScriptableObject
{
    [SerializeField]
    private List<ItemDefinition> itemDefinitions = new();

    private Dictionary<string, ItemDefinition> lookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        // Rebuilding catches empty and duplicate IDs early instead of returning ambiguous definitions.
        lookup = new Dictionary<string, ItemDefinition>();

        foreach (ItemDefinition definition in itemDefinitions)
        {
            if (definition == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(definition.ItemID))
            {
                Debug.LogWarning($"Item definition '{definition.name}' has no item ID.");

                continue;
            }

            if (!lookup.TryAdd(definition.ItemID, definition))
            {
                Debug.LogError($"Duplicate item definition ID: {definition.ItemID}");
            }
        }
    }

    public bool TryGetItemDefinition(string definitionID, out ItemDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definitionID))
        {
            definition = null;
            return false;
        }

        if (lookup == null)
        {
            BuildLookup();
        }

        return lookup.TryGetValue(definitionID, out definition);
    }
}
