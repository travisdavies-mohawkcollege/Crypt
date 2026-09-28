using UnityEngine;
using System.Collections.Generic;

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
        Debug.Log($"Flamesnap hit {other.name}");
        IDamagable damagable = other.GetComponentInParent<IDamagable>();
        if(damagable != null)
        {
            damagable.TakeDamage(10f);
        }
    }
}
