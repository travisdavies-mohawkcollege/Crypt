
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// Shared combat functionality. Attach a derived class, never this base class directly.
[RequireComponent(typeof(EnemyAI))]
public abstract class EnemyCombat : MonoBehaviour
{
    [Header("Attack")]
    [Min(0f)] public float attackCooldownMin = 1.5f;
    [Min(0f)] public float attackCooldownMax = 2.5f;

    [Header("Anchors")]
    public Transform weaponHolder;
    public HumanBodyBones weaponBone = HumanBodyBones.RightHand;
    public Vector3 position;
    public Vector3 rotation;
    public Transform projectileOrigin;
    public Transform vfxPoint;

    [Header("Weapon Grip")]
    public bool curlFingers = true;
    public Vector3 curlAxis = Vector3.forward;
    public Vector3 thumbAxis = Vector3.forward;
    public float thumbCurl = 30f;
    public float indexCurl = 60f;
    public float middleCurl = 60f;
    public float ringCurl = 60f;
    public float pinkyCurl = 60f;
    public Vector3 jointScale = Vector3.one;

    [Header("Arm Sway")]
    public bool dampArm = true;
    [Range(0f, 1f)] public float swayDamping = 0.6f;
    [Min(0.1f)] public float swayAverageTime = 1.5f;

    [Header("Notice Reaction")]
    [SerializeField] private bool noticeReaction = true;

    private const float NoticeDuration = 0.5f;
    private const float NoticeCooldown = 10f;
    private const float HitDelay = 0.5f;
    private const float RangeBuffer = 1f;
    private const float MaxAttackTime = 3f;

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AlertHash = Animator.StringToHash("Alert");
    private static readonly int DodgeHash = Animator.StringToHash("Dodge");

    protected EnemyAI ai;
    protected float nextAttackTime;
    protected float pendingTime = -1f;
    protected Transform muzzle;

    private GameObject weaponInstance;
    private bool combatInitialized;

    private bool noticing;
    private float noticeStartTime;
    private float lastAggroTime = -999f;
    private AwarenessState lastState;

    private bool attacking;
    private bool sawAttackState;
    private bool warnedState;
    private float attackStartTime;
    private float lastAttackAnimTime;

    private bool dodging;
    private bool sawDodgeState;
    private float dodgeStartTime;
    private float nextDodgeTime;

    private float nextRepath;

    private Transform[] fingerBones;
    private Quaternion[] fingerRest;
    private int[] fingerId;
    private int[] fingerJoint;

    private Transform[] armBones;
    private Quaternion[] armAverage;

    private bool holderIsAuto;

    public virtual bool IsBusy => noticing || dodging;
    public virtual bool DrivesMovement => false;

    public float AttackRange => ai != null && ai.Data != null
        ? ai.Data.combatRange + RangeBuffer
        : RangeBuffer;

    protected Animator EnemyAnimator => ai != null ? ai.Animator : null;
    protected NavMeshAgent Agent => ai != null ? ai.agent : null;
    protected Transform Player => ai != null ? ai.player : null;
    protected bool CanSeePlayer => ai != null && ai.canSeePlayer;
    protected Vector3 TargetOffset => ai != null ? ai.targetOffset : Vector3.zero;
    protected float EnemyDamage => ai != null ? ai.Stats.damage : 0f;
    protected bool CombatInitialized => combatInitialized;
    protected bool IsAttacking => attacking;

    protected Vector3 LastSeenPosition =>
        ai != null ? ai.lastSeenPosition : Vector3.zero;

    protected Vector3 AimPoint =>
        (CanSeePlayer ? Player.position : LastSeenPosition) + TargetOffset;

    protected bool InAggro =>
        ai != null &&
        ai.currentState == AwarenessState.Aggro &&
        ai.player != null;

    protected virtual bool SupportsDodge => false;
    protected virtual float DodgeCooldown => 3f;
    protected virtual string DrawStateName => "Attack";
    protected virtual float DrawEnd => 0.8f;

    protected virtual void Awake()
    {
        ai = GetComponent<EnemyAI>();
    }

    protected void StopNavigation()
    {
        if (ai != null)
            ai.StopNavigation();
    }

    protected void ChasePlayer()
    {
        if (ai != null)
            ai.ChasePlayer();
    }

    public virtual void UpdateMovement()
    {
        if (InAggro)
            ChasePlayer();
    }

    // Ranged movement: move into shooting range, then stand still.
    protected void RangedMovement()
    {
        if (!InAggro || noticing || dodging)
            return;

        NavMeshAgent agent = Agent;

        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return;

        bool hasShot = CanSeePlayer &&
                       FlatToPlayer().magnitude <= AttackRange;

        if (attacking || hasShot)
        {
            StopNavigation();
            return;
        }

        if (Time.time < nextRepath)
            return;

        nextRepath = Time.time + 0.2f;

        Vector3 goal = CanSeePlayer ? Player.position : LastSeenPosition;

        if (NavMesh.SamplePosition(goal, out NavMeshHit hit, 4f, agent.areaMask))
        {
            agent.isStopped = false;
            agent.speed = ai.Data.runSpeed;
            agent.SetDestination(hit.position);
        }
    }

    // Returns true on the frame the projectile should be released.
    protected bool RangedTick()
    {
        if (!InAggro)
        {
            attacking = false;
            pendingTime = -1f;
            EndDodge();
            return false;
        }

        if (noticing)
            return false;

        Vector3 flat = FlatToPlayer();

        if (dodging)
        {
            FacePlayer(flat, 12f);
            UpdateDodge();
            return false;
        }

        if (attacking)
        {
            FacePlayer(flat, 12f);
            return UpdateAttackRelease();
        }

        if (SupportsDodge &&
            CanSeePlayer &&
            flat.magnitude < AttackRange * 0.6f &&
            Time.time >= nextDodgeTime)
        {
            StartDodge();
            return false;
        }

        if (!CanSeePlayer ||
            flat.magnitude > AttackRange ||
            EnemyAnimator == null)
            return false;

        FacePlayer(flat);

        if (TryStartAttack())
        {
            attacking = true;
            attackStartTime = Time.time;
            lastAttackAnimTime = -1f;
            sawAttackState = false;
            warnedState = false;
        }

        return false;
    }

    private void StartDodge()
    {
        Animator animator = EnemyAnimator;

        if (animator == null || !HasDodgeParameter(animator))
            return;

        dodging = true;
        sawDodgeState = false;
        dodgeStartTime = Time.time;

        NavMeshAgent agent = Agent;

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        animator.SetBool(DodgeHash, true);
    }

    private void UpdateDodge()
    {
        Animator animator = EnemyAnimator;

        if (animator == null)
        {
            EndDodge();
            return;
        }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);

        bool inDodge = current.IsName("Dodge") ||
                       (animator.IsInTransition(0) && next.IsName("Dodge"));

        if (inDodge)
            sawDodgeState = true;

        if ((sawDodgeState && !inDodge) ||
            Time.time - dodgeStartTime >= 3f)
        {
            EndDodge();
        }
    }

    private bool HasDodgeParameter(Animator animator)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == DodgeHash &&
                parameter.type == AnimatorControllerParameterType.Bool)
                return true;
        }

        return false;
    }

    private void EndDodge()
    {
        if (!dodging)
            return;

        dodging = false;
        nextDodgeTime = Time.time + DodgeCooldown;

        if (EnemyAnimator != null)
            EnemyAnimator.SetBool(DodgeHash, false);

        NavMeshAgent agent = Agent;

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            agent.isStopped = false;
    }

    protected bool TryGetDrawTime(out float t)
    {
        t = -1f;

        if (EnemyAnimator == null)
            return false;

        AnimatorStateInfo info =
            EnemyAnimator.GetCurrentAnimatorStateInfo(0);

        if (!info.IsName(DrawStateName))
            return false;

        t = info.normalizedTime;

        if (t > 1f)
            t -= Mathf.Floor(t);

        return true;
    }

    private bool UpdateAttackRelease()
    {
        float elapsed = Time.time - attackStartTime;

        if (elapsed > MaxAttackTime)
        {
            attacking = false;
            pendingTime = -1f;
            return false;
        }

        if (TryGetDrawTime(out float t))
        {
            sawAttackState = true;
            lastAttackAnimTime = Mathf.Max(lastAttackAnimTime, t);

            if (t >= DrawEnd)
                return FinishAttack(true);

            return false;
        }

        if (sawAttackState)
            return FinishAttack(lastAttackAnimTime >= DrawEnd * 0.5f);

        if (!warnedState && elapsed > 0.6f)
        {
            warnedState = true;

            Debug.LogWarning(
                $"[{name}] Animator did not enter \"{DrawStateName}\" after the Attack trigger."
            );
        }

        return elapsed >= HitDelay + 0.3f
            ? FinishAttack(true)
            : false;
    }

    private bool FinishAttack(bool release)
    {
        attacking = false;
        pendingTime = -1f;
        return release;
    }

    public abstract void Tick();

    public virtual void OnInitialized()
    {
        if (ai == null)
            ai = GetComponent<EnemyAI>();

        if (ai == null || ai.Data == null)
            return;

        combatInitialized = true;
        lastState = ai.currentState;

        if (weaponInstance != null)
            Destroy(weaponInstance);

        muzzle = projectileOrigin;

        fingerBones = null;
        armBones = null;
        armAverage = null;
        holderIsAuto = false;

        GameObject prefab = ai.Data.weaponPrefab;

        if (prefab == null)
            return;

        if (weaponHolder == null)
            weaponHolder = FindOrCreateHolder();

        if (weaponHolder == null)
            return;

        weaponInstance = Instantiate(prefab, weaponHolder);

        AlignWeapon(weaponInstance.transform);
        CacheFingers();

        foreach (Collider c in weaponInstance.GetComponentsInChildren<Collider>())
            Destroy(c);

        Transform m = FindDeep(weaponInstance.transform, "Muzzle");

        if (m != null)
            muzzle = m;
    }

    private void AlignWeapon(Transform weapon)
    {
        Quaternion offsetRot = Quaternion.Euler(ai.Data.weaponRotation);
        Transform grip = FindDeep(weapon, "Grip");

        if (grip == null)
        {
            weapon.localPosition = ai.Data.weaponPosition;
            weapon.localRotation = offsetRot;
            return;
        }

        Vector3 gripPos = weapon.InverseTransformPoint(grip.position);
        Quaternion gripRot = Quaternion.Inverse(weapon.rotation) * grip.rotation;

        Quaternion rot = offsetRot * Quaternion.Inverse(gripRot);

        weapon.localRotation = rot;
        weapon.localPosition =
            ai.Data.weaponPosition -
            rot * Vector3.Scale(gripPos, weapon.localScale);
    }

    private static Transform FindDeep(Transform root, string childName)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t != root && t.name == childName)
                return t;
        }

        return null;
    }

    private void CacheFingers()
    {
        fingerBones = null;
        armBones = null;

        Animator animator = ai.Animator;

        if (animator == null)
            return;

        if (weaponBone != HumanBodyBones.LeftHand &&
            weaponBone != HumanBodyBones.RightHand)
            return;

        Transform hand = animator.GetBoneTransform(weaponBone);

        if (hand == null)
            return;

        if (animator.isHuman)
        {
            bool left = weaponBone == HumanBodyBones.LeftHand;

            Transform upper = animator.GetBoneTransform(
                left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm
            );

            Transform lower = animator.GetBoneTransform(
                left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm
            );

            armBones = new[] { upper, lower, hand };
            armAverage = new Quaternion[armBones.Length];

            for (int i = 0; i < armBones.Length; i++)
            {
                armAverage[i] = armBones[i] != null
                    ? armBones[i].localRotation
                    : Quaternion.identity;
            }
        }

        List<Transform> bones = new List<Transform>();

        foreach (Transform t in hand.GetComponentsInChildren<Transform>(true))
        {
            if (t == hand)
                continue;

            if (weaponHolder != null && t.IsChildOf(weaponHolder))
                continue;

            bones.Add(t);
        }

        if (bones.Count == 0)
            return;

        fingerBones = bones.ToArray();
        fingerRest = new Quaternion[fingerBones.Length];
        fingerId = new int[fingerBones.Length];
        fingerJoint = new int[fingerBones.Length];

        for (int i = 0; i < fingerBones.Length; i++)
        {
            Transform b = fingerBones[i];

            fingerRest[i] = b.localRotation;
            fingerId[i] = FingerIdFromName(b.name);

            int depth = 0;

            for (Transform p = b; p != null && p != hand; p = p.parent)
                depth++;

            fingerJoint[i] = Mathf.Clamp(depth - 1, 0, 2);
        }
    }

    private static int FingerIdFromName(string boneName)
    {
        string n = boneName.ToLowerInvariant();

        if (n.Contains("thumb")) return 0;
        if (n.Contains("index")) return 1;
        if (n.Contains("mid")) return 2;
        if (n.Contains("ring")) return 3;
        if (n.Contains("pinky") || n.Contains("little")) return 4;

        return -1;
    }

    private float CurlFor(int id)
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

    protected virtual void LateUpdate()
    {
        if (weaponInstance == null)
            return;

        if (holderIsAuto && weaponHolder != null)
        {
            weaponHolder.localPosition = position;
            weaponHolder.localEulerAngles = rotation;
        }

        if (armBones != null)
        {
            float follow = 1f - Mathf.Exp(
                -Time.deltaTime / Mathf.Max(0.1f, swayAverageTime)
            );

            for (int i = 0; i < armBones.Length; i++)
            {
                if (armBones[i] == null)
                    continue;

                Quaternion animated = armBones[i].localRotation;

                armAverage[i] = Quaternion.Slerp(
                    armAverage[i],
                    animated,
                    follow
                );

                if (dampArm)
                {
                    armBones[i].localRotation = Quaternion.Slerp(
                        animated,
                        armAverage[i],
                        swayDamping
                    );
                }
            }
        }

        if (!curlFingers || fingerBones == null)
            return;

        for (int i = 0; i < fingerBones.Length; i++)
        {
            if (fingerBones[i] == null)
                continue;

            int id = fingerId[i];

            float angle = CurlFor(id) * jointScale[fingerJoint[i]];
            Vector3 axis = id == 0 ? thumbAxis : curlAxis;

            fingerBones[i].localRotation =
                fingerRest[i] * Quaternion.AngleAxis(angle, axis);
        }
    }

    private Transform FindOrCreateHolder()
    {
        Animator animator = ai.Animator;

        if (animator == null || !animator.isHuman)
        {
            Debug.LogWarning(
                $"[{name}] Assign Weapon Holder manually. Automatic detection requires a Humanoid avatar."
            );

            return null;
        }

        Transform hand = animator.GetBoneTransform(weaponBone);

        if (hand == null)
        {
            Debug.LogWarning(
                $"[{name}] Humanoid avatar has no {weaponBone} bone mapped."
            );

            return null;
        }

        GameObject holder = new GameObject("WeaponHolder");

        holder.transform.SetParent(hand, false);
        holder.transform.localPosition = position;
        holder.transform.localEulerAngles = rotation;

        holderIsAuto = true;

        return holder.transform;
    }

    public void NoticeTick()
    {
        if (ai == null)
            ai = GetComponent<EnemyAI>();

        if (ai == null)
            return;

        EnsureInitialized();

        if (!combatInitialized)
            return;

        AwarenessState state = ai.currentState;

        bool enteredAggro =
            lastState != AwarenessState.Aggro &&
            state == AwarenessState.Aggro;

        if (enteredAggro &&
            noticeReaction &&
            !noticing &&
            Time.time - lastAggroTime >= NoticeCooldown)
        {
            StartNotice();
        }

        if (state == AwarenessState.Aggro)
            lastAggroTime = Time.time;

        lastState = state;

        if (!noticing)
            return;

        if (!InAggro || Time.time - noticeStartTime >= NoticeDuration)
            EndNotice();
        else
            FacePlayer(FlatToPlayer());
    }

    private void EnsureInitialized()
    {
        if (combatInitialized)
            return;

        if (ai == null)
            ai = GetComponent<EnemyAI>();

        if (ai == null || ai.Data == null)
            return;

        OnInitialized();
    }

    private void StartNotice()
    {
        noticing = true;
        noticeStartTime = Time.time;
        pendingTime = -1f;

        if (ai.agent != null &&
            ai.agent.isActiveAndEnabled &&
            ai.agent.isOnNavMesh)
        {
            ai.agent.ResetPath();
            ai.agent.isStopped = true;
            ai.agent.velocity = Vector3.zero;
        }

        if (ai.Animator != null)
        {
            ai.Animator.ResetTrigger(AttackHash);
            ai.Animator.SetTrigger(AlertHash);
        }
    }

    private void EndNotice()
    {
        noticing = false;

        Animator animator = ai.Animator;

        if (animator != null)
        {
            animator.ResetTrigger(AlertHash);
            animator.CrossFadeInFixedTime("Idle", 0.1f);
        }

        if (ai.agent != null &&
            ai.agent.isActiveAndEnabled &&
            ai.agent.isOnNavMesh)
        {
            ai.agent.isStopped = false;
        }
    }

    public virtual void OnDeath()
    {
        pendingTime = -1f;
        noticing = false;
        attacking = false;
        EndDodge();
    }

    protected Vector3 FlatToPlayer()
    {
        if (ai == null || ai.player == null)
            return Vector3.zero;

        Vector3 flat = ai.player.position - transform.position;
        flat.y = 0f;

        return flat;
    }

    protected void FacePlayer(Vector3 flat, float speed = 10f)
    {
        if (flat.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(flat),
            speed * Time.deltaTime
        );
    }

    protected bool TryStartAttack()
    {
        if (ai == null || noticing || dodging || Time.time < nextAttackTime)
            return false;

        float min = Mathf.Max(0f, attackCooldownMin);
        float max = Mathf.Max(min, attackCooldownMax);

        nextAttackTime = Time.time + Random.Range(min, max);
        pendingTime = Time.time + HitDelay;

        if (ai.Animator != null)
            ai.Animator.SetTrigger(AttackHash);

        return true;
    }

    protected bool PendingReady()
    {
        if (pendingTime < 0f || Time.time < pendingTime)
            return false;

        pendingTime = -1f;
        return true;
    }

    protected void DamagePlayer(float amount)
    {
        if (ai != null && ai.player != null && ai.Data != null)
            DamageUtil.Hit(ai.player, amount, ai.Data.element);
    }

    protected Vector3 MuzzlePosition =>
        muzzle != null
            ? muzzle.position
            : transform.position + Vector3.up * 1.3f;

    protected EnemyProjectile SpawnProjectile(EnemyProjectile prefab)
    {
        return SpawnProjectile(prefab, MuzzlePosition);
    }

    protected EnemyProjectile SpawnProjectile(
        EnemyProjectile prefab,
        Vector3 position)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[{name}] No projectile prefab assigned!");
            return null;
        }

        if (ai == null || ai.player == null || ai.Data == null)
            return null;

        EnemyProjectile projectile =
            Instantiate(prefab, position, Quaternion.identity);

        projectile.Launch(
            ai.player,
            ai.player.position + ai.targetOffset,
            ai.Stats.damage,
            ai.Data.element,
            transform
        );

        return projectile;
    }

    protected EnemyProjectile SpawnProjectile(
        EnemyProjectile prefab,
        Vector3 position,
        Vector3 aimPoint)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[{name}] No projectile prefab assigned!");
            return null;
        }

        if (ai == null || ai.player == null || ai.Data == null)
            return null;

        EnemyProjectile projectile =
            Instantiate(prefab, position, Quaternion.identity);

        projectile.Launch(
            ai.player,
            aimPoint,
            ai.Stats.damage,
            ai.Data.element,
            transform
        );

        return projectile;
    }
}
