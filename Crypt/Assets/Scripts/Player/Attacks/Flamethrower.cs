using UnityEngine;
using System.Collections.Generic;

// Applies small repeated damage rolls when flamethrower particles strike a damageable target.
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
        // Search parents because damageable logic may live above the struck collider.
        //Debug.Log($"Flamethrower hit {other.name}");
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if(damageable != null)
        {
            damageable.TakeDamage(Random.Range(1, 4));
        }
    }
}
