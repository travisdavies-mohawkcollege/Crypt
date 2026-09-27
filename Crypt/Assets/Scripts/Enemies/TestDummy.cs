using UnityEngine;

public class TestDummy : MonoBehaviour, IDamagable
{
    private float health = 100f;

    public void TakeDamage(float damage)
    {
        health -= damage;
    }
}
