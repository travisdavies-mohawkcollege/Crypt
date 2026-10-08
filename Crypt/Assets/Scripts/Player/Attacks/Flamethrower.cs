using UnityEngine;
using System.Collections.Generic;

public class Flamethrower : MonoBehaviour
{
    private ParticleSystem particles;
    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    void Start()
    {
        particles = GetComponent<ParticleSystem>();
    }

    void OnParticleCollision(GameObject other)
    {
        Debug.Log($"Flamethrower hit {other.name}");
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if(damageable != null)
        {
            damageable.TakeDamage(Random.Range(1, 4));
        }
    }
}
