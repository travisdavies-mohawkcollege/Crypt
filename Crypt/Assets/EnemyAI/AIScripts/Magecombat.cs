using System.Collections.Generic;
using UnityEngine;

//  Mage-specific behaviour: charges an orb at the staff's empty transform "SummonPoint", then fires it straight at the player.
public class MageCombat : EnemyCombat
{
    [Header("Summon")]
    public EnemyProjectile orbPrefab;
    [Tooltip("Optional. Leave empty to auto-find a child named \"SummonPoint\" on the spawned weapon.")]
    public Transform summonPoint;
    [Tooltip("Only used if no SummonPoint is found anywhere. Offset from the enemy's root.")]
    public Vector3 summonOffset = new Vector3(0f, 2.2f, 0f);

    [Header("Range & Aim")]
    [Tooltip("Distance the mage starts casting from. 0 = use the enemy data's combat range.")]
    public float castRange = 12f;
    [Tooltip("Nudge from the center of the player's body. Negative = lower.")]
    public float aimHeightOffset = 0f;

    [Header("Charge")]
    [Min(0.05f)] public float chargeDuration = 1.2f;
    [Range(0f, 1f)] public float startScale = 0.2f;

    private float CastRange => castRange > 0f ? castRange : AttackRange;

    private EnemyProjectile orb;
    private Vector3 orbBaseScale = Vector3.one;
    private float chargeStartTime;
    private bool charging;
    private Transform weaponSummon;
    private Transform[] mFingers;
    private Quaternion[] mRest;
    private int[] mId;
    private int[] mJoint;

    public override void UpdateMovement()
    {
        if (InAggro)
            StopNavigation();
    }

    public override void Tick()
    {
        if (!InAggro)
        {
            CancelCharge();
            return;
        }

        if (charging)
        {
            UpdateCharge();
            return;
        }

        if (!CanSeePlayer || EnemyAnimator == null)
            return;

        Vector3 flat = FlatToPlayer();

        if (flat.magnitude > CastRange)
            return;

        FacePlayer(flat);

        if (TryStartAttack())
            BeginCharge();
    }

    private void BeginCharge()
    {
        pendingTime = -1f;

        if (orbPrefab == null)
        {
            Debug.LogWarning($"[{name}] MageCombat has no orbPrefab assigned!");
            return;
        }

        Transform point = summonPoint != null ? summonPoint : weaponSummon;
        Transform parent = point != null ? point : transform;

        orb = Instantiate(orbPrefab, parent);
        orbBaseScale = orbPrefab.transform.localScale;

        orb.transform.localPosition = point != null ? Vector3.zero : summonOffset;
        orb.transform.localRotation = Quaternion.identity;
        orb.transform.localScale = orbBaseScale * startScale;

        orb.Hold(transform);

        chargeStartTime = Time.time;
        charging = true;
    }

    private void UpdateCharge()
    {
        if (orb == null)
        {
            charging = false;
            return;
        }

        FacePlayer(FlatToPlayer(), 12f);

        float t = Mathf.Clamp01((Time.time - chargeStartTime) / chargeDuration);
        orb.transform.localScale = orbBaseScale * Mathf.Lerp(startScale, 1f, t);

        if (t >= 1f)
            Release();
    }

    private void Release()
    {
        charging = false;

        if (orb == null || Player == null || ai.Data == null)
        {
            CancelCharge();
            return;
        }

        Transform orbTransform = orb.transform;
        orbTransform.SetParent(null, true);
        orbTransform.localScale = orbBaseScale;

        orb.arcHeightRatio = 0f; // Forces shooting straight for the projectile

        orb.Launch(Player, GetBodyAimPoint(), EnemyDamage, ai.Data.element, transform);
        orb = null;

        // Cooldown starts after the projectile shoots
        nextAttackTime = Time.time + Random.Range(
            Mathf.Max(0f, attackCooldownMin),
            Mathf.Max(attackCooldownMin, attackCooldownMax));
    }

    // Aims at the center of the player's collider
    private Vector3 GetBodyAimPoint()
    {
        if (Player != null && CanSeePlayer)
        {
            foreach (Collider c in Player.GetComponentsInChildren<Collider>())
            {
                if (c.enabled && !c.isTrigger)
                    return c.bounds.center + Vector3.up * aimHeightOffset;
            }
        }
        return AimPoint + Vector3.up * aimHeightOffset;
    }

    private void CancelCharge()
    {
        charging = false;

        if (orb != null)
            Destroy(orb.gameObject);

        orb = null;
    }

    public override void OnDeath()
    {
        CancelCharge();
        base.OnDeath();
    }

    public override void OnInitialized()
    {
        base.OnInitialized();

        mFingers = null;
        weaponSummon = null;

        if (ai == null || ai.Data == null || ai.Data.weaponPrefab == null || weaponHolder == null)
            return;

        // The weapon is spawned at runtime, so find its SummonPoint by name
        foreach (Transform t in weaponHolder.GetComponentsInChildren<Transform>(true))
        {
            if (t != weaponHolder && t.name == "SummonPoint")
            {
                weaponSummon = t;
                break;
            }
        }

        CacheMageFingers();
    }

    // Mage weapon grip, since it has 4 fingers instead of 5

    private void CacheMageFingers()
    {
        Animator animator = ai.Animator;

        if (animator == null || !animator.isHuman)
            return;

        if (weaponBone != HumanBodyBones.LeftHand && weaponBone != HumanBodyBones.RightHand)
            return;

        Transform hand = animator.GetBoneTransform(weaponBone);

        if (hand == null)
            return;

        List<Transform> bones = new List<Transform>();
        List<int> ids = new List<int>();
        List<int> joints = new List<int>();
        List<string> unmatched = new List<string>();

        foreach (Transform chainRoot in hand)
        {
            if (weaponHolder != null && (chainRoot == weaponHolder || chainRoot.IsChildOf(weaponHolder)))
                continue;

            int id = FingerId(chainRoot.name);

            if (id < 0)
            {
                unmatched.Add(chainRoot.name);
                continue;
            }

            foreach (Transform t in chainRoot.GetComponentsInChildren<Transform>(true))
            {
                if (weaponHolder != null && t.IsChildOf(weaponHolder))
                    continue;

                int depth = 0;
                for (Transform p = t; p != null && p != hand; p = p.parent)
                    depth++;

                bones.Add(t);
                ids.Add(id);
                joints.Add(Mathf.Clamp(depth - 1, 0, 2));
            }
        }

        if (unmatched.Count > 0)
        {
            Debug.LogWarning(
                $"[{name}] MageCombat: hand children not recognised as fingers (left uncurled): {string.Join(", ", unmatched)}");
        }

        if (bones.Count == 0)
            return;

        mFingers = bones.ToArray();
        mId = ids.ToArray();
        mJoint = joints.ToArray();
        mRest = new Quaternion[mFingers.Length];

        for (int i = 0; i < mFingers.Length; i++)
            mRest[i] = mFingers[i].localRotation;
    }

    private static int FingerId(string boneName)
    {
        string n = boneName.ToLowerInvariant();

        if (n.Contains("thumb")) return 0;
        if (n.Contains("index") || n.Contains("pointer")) return 1;
        if (n.Contains("mid")) return 2;
        if (n.Contains("ring")) return 3;
        if (n.Contains("pinky") || n.Contains("pinkie") || n.Contains("little")) return 4;

        return -1;
    }

    private float MageCurlFor(int id)
    {
        switch (id)
        {
            case 0: return thumbCurl;
            case 1: return indexCurl;
            case 2: return middleCurl;
            case 3: return ringCurl;
            case 4: return pinkyCurl;
            default: return middleCurl;
        }
    }

    protected override void LateUpdate()
    {
        // Lets the base handle holder/arm sway, but skips finger curling
        bool curl = curlFingers;
        curlFingers = false;
        base.LateUpdate();
        curlFingers = curl;

        if (!curl || mFingers == null)
            return;

        for (int i = 0; i < mFingers.Length; i++)
        {
            if (mFingers[i] == null)
                continue;

            float angle = MageCurlFor(mId[i]) * jointScale[mJoint[i]];
            Vector3 axis = mId[i] == 0 ? thumbAxis : curlAxis;

            mFingers[i].localRotation = mRest[i] * Quaternion.AngleAxis(angle, axis);
        }
    }
}