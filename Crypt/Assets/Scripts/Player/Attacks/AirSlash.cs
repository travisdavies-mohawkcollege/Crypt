using System.Collections.Generic;
using UnityEngine;

public class AirSlash : MonoBehaviour
{
    [SerializeField] private float damage = 5f;
    [SerializeField] private float damageWindow = 0.2f;

    private readonly Dictionary<Component, float> nextDamageTimes = new();

    private void OnParticleCollision(GameObject other)
    {
        if (other == null) return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        Component target = damageable as Component;
        if (target == null || target.GetComponentInParent<PlayerController>() != null) return;

        if (nextDamageTimes.TryGetValue(target, out float nextDamageTime) && Time.time < nextDamageTime) return;

        nextDamageTimes[target] = Time.time + damageWindow;
        DamageUtil.Hit(target, damage, Element.Air);
    }

    private void OnDisable()
    {
        nextDamageTimes.Clear();
    }
}