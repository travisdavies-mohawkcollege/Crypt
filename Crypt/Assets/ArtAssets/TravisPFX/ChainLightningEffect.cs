using System.Collections.Generic;
using UnityEngine;

public class ChainLightningEffect : MonoBehaviour
{
    [SerializeField] private LightningArc arcPrefab;
    [SerializeField] private ParticleSystem impactPrefab;

    [SerializeField] private float targetHeight = 1f;
    [SerializeField] private float targetRandomRadius = 0.2f;

    public void Play(Transform shootingPoint, IReadOnlyList<Transform> targets)
    {
        if (shootingPoint == null || targets == null || targets.Count == 0) return;

        Vector3 previousPoint = shootingPoint.position;
        bool isFirstArc = true;

        foreach (Transform target in targets)
        {
            Vector3 targetPoint = GetTargetPoint(target);
            LightningArc arc = Instantiate(arcPrefab);

            if (isFirstArc) arc.Initialize(shootingPoint, targetPoint);
            else arc.Initialize(previousPoint, targetPoint);

            ParticleSystem impact = Instantiate(impactPrefab, targetPoint, Quaternion.identity);
            impact.Play();
            Destroy(impact.gameObject, 2f);

            previousPoint = targetPoint;
            isFirstArc = false;
        }
    }

    private Vector3 GetTargetPoint(Transform target)
    {
        Collider targetCollider = target.GetComponentInChildren<Collider>();
        Vector3 basePoint = targetCollider != null ? targetCollider.bounds.center : target.position + Vector3.up * targetHeight;
        return basePoint + Random.insideUnitSphere * targetRandomRadius;
    }
}