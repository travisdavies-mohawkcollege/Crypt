using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ElementalManaPool
{
    public EElements element;
    public float currentMana = 100f;
    public float maxMana = 100f;
}

public class PlayerElementalMana : MonoBehaviour
{
    [SerializeField] private List<ElementalManaPool> pools = new();
    public event Action Changed;

    private void Awake()
    {
        foreach (EElements element in Enum.GetValues(typeof(EElements)))
        {
            if (element == EElements.None || pools.Exists(pool => pool.element == element)) continue;
            pools.Add(new ElementalManaPool { element = element });
        }
    }

    public bool TrySpend(EElements element, float amount)
    {
        ElementalManaPool pool = pools.Find(entry => entry.element == element);
        if (pool == null || amount <= 0f || pool.currentMana < amount) return false;
        pool.currentMana -= amount;
        Changed?.Invoke();
        return true;
    }

    public float GetCurrent(EElements element)
    {
        ElementalManaPool pool = pools.Find(entry => entry.element == element);
        return pool != null ? pool.currentMana : 0f;
    }

    public void Restore(EElements element, float amount)
    {
        ElementalManaPool pool = pools.Find(entry => entry.element == element);
        if (pool == null || amount <= 0f) return;
        pool.currentMana = Mathf.Min(pool.currentMana + amount, pool.maxMana);
        Changed?.Invoke();
    }

    public void RestoreAll(float amount)
    {
        if (amount <= 0f) return;

        bool manaChanged = false;

        foreach (ElementalManaPool pool in pools)
        {
            if (pool.currentMana >= pool.maxMana) continue;
            pool.currentMana = Mathf.Min(pool.currentMana + amount, pool.maxMana);
            manaChanged = true;
        }

        if (manaChanged) Changed?.Invoke();
    }

    public float GetMaximum(EElements element)
    {
        ElementalManaPool pool = pools.Find(entry => entry.element == element);
        return pool != null ? pool.maxMana : 0f;
    }
}