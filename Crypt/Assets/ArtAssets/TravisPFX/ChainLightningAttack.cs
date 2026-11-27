using System.Collections.Generic;
using UnityEngine;

public class ChainLightningAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform shootingPoint;
    [SerializeField] private ChainLightningEffect lightningEffect;

    [Header("Targeting")]
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private float initialRange = 12f;
    [SerializeField, Range(0f, 180f)] private float coneAngle = 45f;

    [Header("Chaining")]
    [SerializeField] private int maximumTargets = 5;
    [SerializeField] private float chainRange = 6f;

    [SerializeField] private float fireInterval = 0.25f;
    private float nextFireTime;

    public void Fire()
    {
        Transform firstTarget = FindInitialTarget();

        if (firstTarget == null)
            return;

        List<Transform> targets = BuildChain(firstTarget);

        for (int i = 0; i < targets.Count; i++)
        {
            float damage = Mathf.Max(1f, 10f - i * 2f);
            DamageUtil.Hit(targets[i], damage, Element.Lightning);
        }

        lightningEffect.Play(shootingPoint, targets);
    }

    public void TryFire()
    {
        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + fireInterval;
        Fire();
    }

    private Transform FindInitialTarget()
    {
        Collider[] candidates = Physics.OverlapSphere(
            shootingPoint.position,
            initialRange,
            targetMask,
            QueryTriggerInteraction.Ignore
        );

        Transform bestTarget = null;
        float bestScore = float.MaxValue;

        foreach (Collider candidate in candidates)
        {
            Transform target = ResolveTarget(candidate);
            if (target == null) continue;
            Vector3 toTarget = GetTargetPosition(target) - shootingPoint.position;
            float angle = Vector3.Angle(shootingPoint.forward, toTarget);

            if (angle > coneAngle * 0.5f)
                continue;

            if (!HasLineOfSight(shootingPoint.position, target))
                continue;

            // Prefer targets near the center of the cone, then nearby ones.
            float score = angle * 2f + toTarget.magnitude;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }

        return bestTarget;
    }

    private Transform ResolveTarget(Collider candidate)
    {
        IDamageable damageable = candidate.GetComponentInParent<IDamageable>();
        Component damageComponent = damageable as Component;
        return damageComponent != null ? damageComponent.transform : null;
    }

    private List<Transform> BuildChain(Transform firstTarget)
    {
        List<Transform> chain = new() { firstTarget };
        Transform currentTarget = firstTarget;

        while (chain.Count < maximumTargets)
        {
            Transform nextTarget = FindNextTarget(currentTarget, chain);

            if (nextTarget == null)
                break;

            chain.Add(nextTarget);
            currentTarget = nextTarget;
        }

        return chain;
    }

    private Transform FindNextTarget(Transform currentTarget, List<Transform> alreadyHit)
    {
        Vector3 currentPosition = GetTargetPosition(currentTarget);

        Collider[] candidates = Physics.OverlapSphere(currentPosition, chainRange, targetMask, QueryTriggerInteraction.Ignore);

        Transform closestTarget = null;
        float closestDistanceSqr = float.MaxValue;

        foreach (Collider candidate in candidates)
        {
            Transform target = ResolveTarget(candidate);
            if (target == null) continue;

            if (alreadyHit.Contains(target)) continue;

            if (!HasLineOfSight(currentPosition, target)) continue;

            float distanceSqr = (GetTargetPosition(target) - currentPosition).sqrMagnitude;

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestTarget = target;
            }
        }

        return closestTarget;
    }

    private bool HasLineOfSight(Vector3 start, Transform target)
    {
        Vector3 targetPosition = GetTargetPosition(target);
        Vector3 direction = targetPosition - start;
        float distance = direction.magnitude;

        return !Physics.Raycast(start, direction.normalized, distance, obstructionMask, QueryTriggerInteraction.Ignore);
    }

    private Vector3 GetTargetPosition(Transform target)
    {
        Collider targetCollider = target.GetComponent<Collider>();

        return targetCollider != null ? targetCollider.bounds.center : target.position;
    }
}