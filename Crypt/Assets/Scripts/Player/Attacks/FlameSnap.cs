using UnityEngine;
using System.Collections.Generic;

// Converts FlameSnap particle collisions into direct damage calls.
public class FlameSnap : MonoBehaviour
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
        Debug.Log($"Flamesnap hit {other.name}");
        IDamageable damagable = other.GetComponentInParent<IDamageable>();
        if(damagable != null)
        {
            damagable.TakeDamage(10f);
        }
    }
}
