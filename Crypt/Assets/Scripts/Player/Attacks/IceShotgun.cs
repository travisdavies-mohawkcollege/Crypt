using UnityEngine;
using System.Collections.Generic;

public class IceShotgun : MonoBehaviour
{
    private ParticleSystem particles;
    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    void Start()
    {
        particles = GetComponent<ParticleSystem>();
    }

    void OnParticleCollision(GameObject other)
    {
        IElementalDamageable damageable = other.GetComponentInParent<IElementalDamageable>();
        if (damageable != null)
        {
            int collisionCount = particles.GetCollisionEvents(other, collisionEvents);
            Debug.Log($"collsion events: {collisionCount}");
            float totalDamage = 0f;

            for (int i = 0; i < collisionCount; i++)
            {
                totalDamage += Random.Range(1,4);
            }

            damageable.TakeDamage(totalDamage, Element.Ice);
        }
    }
}
