using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Module Catalog", menuName = "Module Catalog")]
public class ModuleCatalog : ScriptableObject
{
    [Header("Special Modules")]
    [SerializeField] private RoomModule entranceModule;
    [SerializeField] private RoomModule exitModule;

    [Header("Random Modules")]
    [SerializeField] private List<RoomModule> smallModules = new();
    [SerializeField] private List<RoomModule> mediumModules = new();
    [SerializeField] private List<RoomModule> largeModules = new();

    public RoomModule EntranceModule => entranceModule;
    public RoomModule ExitModule => exitModule;
    public IReadOnlyList<RoomModule> SmallModules => smallModules;
    public IReadOnlyList<RoomModule> MediumModules => mediumModules;
    public IReadOnlyList<RoomModule> LargeModules => largeModules;

    public RoomModule SelectSmallModule()
    {
        if (smallModules.Count == 0)
        {
            return null;
        }

        return smallModules[Random.Range(0, smallModules.Count)];
    }

    public RoomModule SelectMediumModule()
    {
        if (mediumModules.Count == 0)
        {
            return null;
        }

        return mediumModules[Random.Range(0, mediumModules.Count)];
    }

    public RoomModule SelectLargeModule()
    {
        if (largeModules.Count == 0)
        {
            return null;
        }

        return largeModules[Random.Range(0, largeModules.Count)];
    }
}
