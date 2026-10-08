using UnityEngine;

public class TestDummy : MonoBehaviour, IDamageable
{
    private float health = 100f;
    private float iFrameMax = 0.25f;
    private float iFrameTimer = 0.25f;
    private bool canTakeDamage = true;
    public void TakeDamage(float damage)
    {
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
}
