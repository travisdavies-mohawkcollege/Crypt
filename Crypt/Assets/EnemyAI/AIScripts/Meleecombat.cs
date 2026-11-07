using UnityEngine;

// Shared by all melee variants.
public class MeleeCombat : EnemyCombat
{
    public override void Tick()
    {
        if (InAggro && ai.Animator != null)
        {
            Vector3 flat = FlatToPlayer();
            if (flat.magnitude <= AttackRange)
            {
                FacePlayer(flat);
                TryStartAttack();
            }
        }

        if (!PendingReady() || ai.player == null) return;
        if (Vector3.Distance(transform.position, ai.player.position) > AttackRange) return;

        DamagePlayer(ai.Stats.damage);
    }
}
