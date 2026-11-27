using UnityEngine;
using System.Collections.Generic;

public class IceSniper : MonoBehaviour
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
            damageable.TakeDamage(25, Element.Ice);
        }
    }
}
