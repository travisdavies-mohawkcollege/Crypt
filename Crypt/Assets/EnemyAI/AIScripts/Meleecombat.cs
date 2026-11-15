using UnityEngine;

// Shared by all melee variants.
public class MeleeCombat : EnemyCombat
{
    private const float MaxSwingTime = 3f;

    private float swingStartTime = -999f;

    public override bool IsBusy => base.IsBusy || IsSwinging;

    private bool IsSwinging
    {
        get
        {
            if (Time.time - swingStartTime > MaxSwingTime) return false;

            if (pendingTime >= 0f) return true;

            Animator animator = EnemyAnimator;
            if (animator == null) return false;

            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);

            return current.IsName(DrawStateName) ||
                   (animator.IsInTransition(0) && next.IsName(DrawStateName));
        }
    }

    public override void Tick()
    {
        if (InAggro && ai.Animator != null)
        {
            Vector3 flat = FlatToPlayer();
            if (flat.magnitude <= AttackRange)
            {
                FacePlayer(flat);

                if (TryStartAttack())
                    swingStartTime = Time.time;
            }
        }

        if (!PendingReady() || ai.player == null) return;
        if (Vector3.Distance(transform.position, ai.player.position) > AttackRange) return;

        DamagePlayer(ai.Stats.damage);
    }

    public override void OnDeath()
    {
        base.OnDeath();
        swingStartTime = -999f;
    }
}