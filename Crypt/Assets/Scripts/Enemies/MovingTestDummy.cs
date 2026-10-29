using UnityEngine;
using UnityEngine.AI;

// Damageable test target that can also interact with the NavMesh and damage the player on contact.
public class MovingTestDummy : MonoBehaviour, IDamageable
{
    [SerializeField] private NavMeshAgent agent;
    private float health = 100f;
    private float iFrameMax = 0.25f;
    private float iFrameTimer = 0.25f;
    private bool canTakeDamage = true;
    public void TakeDamage(float damage)
    {
        // Ignore repeated hits until the short invulnerability window expires.
        if(canTakeDamage)
        {
            Debug.Log($"Took {damage} damage");
            health -= damage;
            DamageNumberPool.Instance.Show(this.transform.position + Vector3.up * 1.5f, damage);
            canTakeDamage = false;
        }
    }

    public void Update()
    {
        // Count down hit immunity independently from incoming damage calls.
        if(!canTakeDamage)
        {
            iFrameTimer -= Time.deltaTime;
            if(iFrameTimer <= 0)
            {
                canTakeDamage = true;
                iFrameTimer = iFrameMax;
            }
        }

        if(health <= 0)
        {
            Destroy(this.gameObject);
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        // Collision damage is only intended for the player object.
        if(collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            player.TakeDamage(10f);
        }
    }
}
