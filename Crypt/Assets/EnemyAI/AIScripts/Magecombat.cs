using UnityEngine;

// Shared by all mage variants. Mage positioning and casting behaviour stay in this class.
public class MageCombat : EnemyCombat
{
    [Header("Summon")]
    public EnemyProjectile orbPrefab;
    public Transform summonPoint;
    public Vector3 summonOffset = new Vector3(0f, 2.2f, 0f);
    [Min(1)] public int orbCount = 1;
    [Min(0f)] public float orbSpread = 0.8f;

    public override void UpdateMovement()
    {
        if (InAggro) ai.StopNavigation();
    }

    public override void Tick()
    {
        if (InAggro && ai.canSeePlayer && ai.Animator != null)
        {
            Vector3 flat = FlatToPlayer();
            if (flat.magnitude <= AttackRange)
            {
                FacePlayer(flat);
                TryStartAttack();
            }
        }

        if (!PendingReady() || ai.player == null) return;
        Summon();
    }

    private void Summon()
    {
        Vector3 center = summonPoint != null ? summonPoint.position : transform.TransformPoint(summonOffset);

        for (int i = 0; i < orbCount; i++)
        {
            float side = (i - (orbCount - 1) * 0.5f) * orbSpread;
            SpawnProjectile(orbPrefab, center + transform.right * side);
        }
    }
}
