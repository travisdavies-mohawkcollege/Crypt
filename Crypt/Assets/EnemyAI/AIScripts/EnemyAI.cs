using UnityEngine;
using UnityEngine.AI;

public enum AwarenessState { Passive, Alerted, Aggro }

// Shared enemy brain: perception, memory, chase, search, patrol, head tracking.
// Attack behaviour stays in EnemyCombat subclasses.
//
// Design: the enemy never knows where the player is without seeing them. It remembers
// the last place and velocity it observed 
// Obstacles should be part of the NavMesh (Navigation Static / NavMeshModifier / carving
// NavMeshObstacle); the agent then paths around them natively. As a safety net, if the agent
// is about to walk into a collider that ISN'T in the NavMesh, it stops and carves that
// collider into the NavMesh at runtime then repaths.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour, IDamageable
{
    [Header("Setup")]
    public EnemyData enemyData;
    [Min(1)] public int level = 1;
    public bool autoInitialize = true;
    [Tooltip("Layers that block sight and count as walls.")]
    public LayerMask obstacleMask = ~0;
    [Tooltip("Optional. Passive enemies walk these in order; if empty they wander around their spawn point.")]
    public Transform[] patrolPoints;

    [Header("Runtime State (debug)")]
    public AwarenessState currentState = AwarenessState.Passive;
    public float awarenessMeter;
    public float currentHealth;

    [HideInInspector] public Transform player;
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public Vector3 lastSeenPosition;
    [HideInInspector] public bool canSeePlayer;
    [HideInInspector] public bool isInitialized;
    [HideInInspector] public bool isDead;

    // Awareness
    private const float AlertedThreshold = 25f;
    private const float CloseFillBoost = 2.5f;
    private const float HuntMemoryTime = 12f;          // re-spot within this window of losing the player -> instant Aggro
    private const float AggroPeripheralHalfAngle = 100f;
    private const float AggroSenseRange = 6f;
    private const float ProximitySenseRange = 2f;

    // Hunting
    private const float LoseTrackDelay = 1f;           // grace after sight breaks before the chase can end
    private const float ChaseClueTimeout = 6f;
    private const float PredictionTime = 2.5f;
    private const float PredictionMinLead = 6f;
    private const float PredictionMaxLead = 14f;
    private const float SearchDuration = 20f;
    private const float SearchRadiusMin = 8f;
    private const float SearchMinLeg = 4f;             // never pick a search spot closer than this to ourselves
    private const float SearchRadiusMax = 16f;
    private const float UrgentTime = 8f;
    private const float UrgentSpeedBlend = 0.7f;
    private const float ArrivalDistance = 1.2f;
    private const float GroundSampleRadius = 4f;
    private const float MaxLegTime = 20f;

    // Collider safeguard
    private const float GuardInterval = 0.08f;
    private const float GuardHoldTime = 0.35f;
    private const float GuardStepHeight = 0.2f;

    // Passive
    private const float PassiveRoamRadius = 8f;
    private const float WallClearance = 1.5f;
    private const float LookAheadDistance = 2f;
    private const float BodyTurnSpeed = 90f;

    // Chase
    private const float ChaseStopDistance = 1.6f;
    private const float ChaseRefreshTime = 0.1f;

    // Head
    private const float AggroHeadMaxYaw = 100f;
    private const float HeadMaxPitch = 40f;
    private const float NeckShare = 0.4f;
    private const float AggroHeadRate = 30f;
    private const float CalmHeadRate = 8f;
    private const float UrgentHeadRate = 14f;
    private const float AggroBodyTurnSpeed = 240f;

    // Headstand: the enemy kicks player off if player stands on head.
    private const float HeadstandRadius = 0.8f;
    private const float HeadstandGraceTime = 0.2f;
    private const float EscapeDistance = 2.5f;
    private const float EscapeDuration = 0.4f;
    private const float EscapeArcHeight = 0.6f;
    private const float EscapeCooldown = 1.5f;
    private const string EscapeTriggerName = "Hop";

    // Misc
    private const float IFrameTime = 0.25f;
    private const float DeathDelay = 3f;

    // Public access for combat scripts

    public EnemyData Data => enemyData;
    public EnemyStats Stats => stats;
    public Animator Animator => enemyAnimator;
    public Vector3 targetOffset => Vector3.up;

    // Private states

    private EnemyCombat combat;
    private Animator enemyAnimator;
    private Collider rootCollider;
    private Collider playerCollider;
    private EnemyStats stats;
    private NavMeshPath scratchPath;

    private float nextDamageTime, nextChaseUpdate, nextGuardCheck, guardHoldUntil;
    private readonly RaycastHit[] guardHits = new RaycastHit[8];
    private float lastAggroTime = -999f, lastSeenTime = -999f;
    private float searchStartTime, searchEndTime;
    private Vector3 lastSeenVelocity, prevPlayerPos;
    private Vector3 searchCenter, chaseGoal;

    private enum RoamPhase { Idle, Moving, Looking }
    private RoamPhase roamPhase = RoamPhase.Idle;
    private Vector3 homePosition;
    private int patrolIndex;
    private float phaseTimer, idleDuration, lookDuration, lookAngle, stuckTimer;
    private bool bodyTurning;
    private float bodyTargetYaw;

    private readonly Vector3[] visited = new Vector3[6];
    private int visitedCount, visitedNext;

    private Transform headBone, neckBone;
    private float headYaw, headPitch, targetYaw, targetPitch;

    private bool escaping, escapeTriggerExists;
    private float headstandTimer, escapeStartTime, nextEscapeTime;
    private Vector3 escapeStartPosition, escapeDirection;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int EscapeHash = Animator.StringToHash(EscapeTriggerName);

    private Vector3 EyePosition => headBone != null ? headBone.position : transform.position + Vector3.up * 1.6f;
    private Vector3 ViewDirection => Quaternion.AngleAxis(headYaw, transform.up) * transform.forward;
    private bool AgentReady => agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;
    private bool Busy => escaping || (combat != null && combat.IsBusy);
    private bool Urgent => currentState == AwarenessState.Alerted && Time.time - lastAggroTime < UrgentTime;
    private float SearchSpeed => Urgent ? Mathf.Lerp(enemyData.alertedSpeed, enemyData.runSpeed, UrgentSpeedBlend) : enemyData.alertedSpeed;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyAnimator = GetComponentInChildren<Animator>();
        combat = GetComponent<EnemyCombat>();
        rootCollider = GetComponent<Collider>();
        scratchPath = new NavMeshPath();

        agent.acceleration = Mathf.Max(agent.acceleration, 16f);
        agent.angularSpeed = Mathf.Max(agent.angularSpeed, 360f);
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        FindHeadBones();
        FindEscapeTrigger();
    }

    private void Start()
    {
        if (isInitialized || !autoInitialize) return;

        // Normalize any preassigned child reference to the tagged player root.
        GameObject playerObj = player != null
            ? ResolvePlayerRoot(player.gameObject)
            : GameObject.FindWithTag("Player");

        if (playerObj != null)
            InitializeEnemy(playerObj, level);
        else
            Debug.LogError($"[{name}] Could not find the player root. Ensure the player ROOT is tagged 'Player'.");
    }

    private void Update()
    {
        if (!isInitialized || isDead) return;

        canSeePlayer = CheckVision();
        UpdateAwareness();
        UpdateRoaming();

        if (combat != null) combat.NoticeTick();
        UpdateHeadstandEscape();

        // Neither combat movement nor pursuit may overwrite the escape sidestep.
        if (!escaping)
        {
            if (combat != null) combat.UpdateMovement();
            else ChasePlayer();
        }

        GuardAgainstUnbakedColliders();
        HoldIfBusy();
        UpdateHead();
        UpdateAnimation();

        if (currentState == AwarenessState.Aggro) lastAggroTime = Time.time;
        if (combat != null && !escaping) combat.Tick();
    }

    // After animation, so the procedural head turn layers over the Animator pose.
    private void LateUpdate()
    {
        if (headBone == null || isDead) return;
        if (Mathf.Abs(headYaw) < 0.01f && Mathf.Abs(headPitch) < 0.01f) return;

        Quaternion look = Quaternion.AngleAxis(headYaw, transform.up) *
                          Quaternion.AngleAxis(-headPitch, transform.right);
        if (neckBone != null && neckBone != headBone)
        {
            neckBone.rotation = Quaternion.Slerp(Quaternion.identity, look, NeckShare) * neckBone.rotation;
            headBone.rotation = Quaternion.Slerp(Quaternion.identity, look, 1f - NeckShare) * headBone.rotation;
        }
        else headBone.rotation = look * headBone.rotation;
    }

    private void FindHeadBones()
    {
        if (enemyAnimator != null && enemyAnimator.isHuman)
        {
            headBone = enemyAnimator.GetBoneTransform(HumanBodyBones.Head);
            neckBone = enemyAnimator.GetBoneTransform(HumanBodyBones.Neck);
        }
        if (headBone != null) return;

        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name.ToLowerInvariant().Contains("head")) { headBone = t; break; }
        }
    }

    private void FindEscapeTrigger()
    {
        escapeTriggerExists = false;
        if (enemyAnimator == null || enemyAnimator.runtimeAnimatorController == null) return;
        foreach (AnimatorControllerParameter p in enemyAnimator.parameters)
        {
            if (p.name == EscapeTriggerName && p.type == AnimatorControllerParameterType.Trigger)
            {
                escapeTriggerExists = true;
                return;
            }
        }
    }

    public void InitializeEnemy(GameObject playerObj, int enemyLevel, EnemyData dataOverride = null)
    {
        if (dataOverride != null) enemyData = dataOverride;
        if (enemyData == null)
        {
            Debug.LogError($"[{name}] Missing EnemyData assignment!");
            return;
        }

        if (playerObj != null)
        {
            GameObject playerRoot = ResolvePlayerRoot(playerObj);

            if (playerRoot == null)
            {
                Debug.LogError(
                    $"[{name}] InitializeEnemy received '{playerObj.name}', but it is not the Player root " +
                    $"and has no parent tagged 'Player'. The player reference was not changed."
                );
                return;
            }

            player = playerRoot.transform;
        }
        CachePlayerCollider();
        if (player != null) prevPlayerPos = player.position;
        lastSeenVelocity = Vector3.zero;

        level = Mathf.Clamp(enemyLevel, 1, enemyData.maxLevel);
        stats = enemyData.GetStats(level);
        currentHealth = stats.maxHealth;
        agent.speed = enemyData.walkSpeed;
        isDead = false;

        homePosition = transform.position;
        currentState = AwarenessState.Passive;
        StartIdle();

        if (combat != null) combat.OnInitialized();
        isInitialized = true;
    }

    // Always store the actual player root, never a child such as the handcanvas.
    // This also handles callers that accidentally pass a child transform.
    private GameObject ResolvePlayerRoot(GameObject candidate)
    {
        if (candidate == null)
            return null;

        Transform current = candidate.transform;
        Transform taggedPlayerRoot = null;

        while (current != null)
        {
            if (current.CompareTag("Player"))
                taggedPlayerRoot = current;

            current = current.parent;
        }

        if (taggedPlayerRoot != null)
            return taggedPlayerRoot.gameObject;

        if (candidate.transform == candidate.transform.root)
            return candidate;

        return null;
    }

    private void CachePlayerCollider()
    {
        playerCollider = null;
        if (player == null) return;

        playerCollider = player.GetComponentInParent<CharacterController>();
        if (playerCollider == null) playerCollider = player.GetComponentInChildren<Collider>();
        if (playerCollider == null) playerCollider = player.GetComponentInParent<Collider>();
    }

    public void TakeDamage(float damage)
    {
        if (!isInitialized || isDead || Time.time < nextDamageTime) return;

        nextDamageTime = Time.time + IFrameTime;
        currentHealth -= damage;

        if (DamageNumberPool.Instance != null)
            DamageNumberPool.Instance.Show(transform.position + Vector3.up * 1.5f, damage);

        if (player != null) ForceAlert(player.position);
        if (currentHealth <= 0f) Die();
    }

    private void Die()
    {
        isDead = true;
        headYaw = headPitch = 0f;
        if (escaping) EndEscape();
        if (combat != null) combat.OnDeath();

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
        }

        if (enemyAnimator != null) enemyAnimator.SetTrigger(DieHash);
        if (rootCollider != null) rootCollider.enabled = false;
        Destroy(gameObject, DeathDelay);
    }

    public void ForceAlert(Vector3 position)
    {
        lastSeenPosition = position;
        lastSeenTime = Time.time;
        lastSeenVelocity = Vector3.zero;
        awarenessMeter = 100f;
        SetState(AwarenessState.Aggro);
    }

    private void HoldIfBusy()
    {
        if (!Busy || !AgentReady) return;
        agent.isStopped = true;
        if (escaping) return; // the escape drives the agent manually
        if (combat == null || !combat.DrivesMovement) agent.velocity = Vector3.zero;
    }

    // Awareness

    private bool CheckVision()
    {
        if (player == null) return false;

        Vector3 eye = EyePosition;
        GetPlayerSamplePoints(out Vector3 a, out Vector3 b);

        float dist = Vector3.Distance(eye, a);
        if (dist > enemyData.sightRange) return false;

        bool engaged = currentState == AwarenessState.Aggro;
        bool closeSense = dist <= (engaged ? AggroSenseRange : ProximitySenseRange);

        float halfAngle = enemyData.sightAngle * 0.5f;
        if (engaged) halfAngle = Mathf.Max(halfAngle, AggroPeripheralHalfAngle);
        else if (currentState == AwarenessState.Alerted) halfAngle = Mathf.Min(180f, halfAngle * 1.3f);

        Vector3 flat = Vector3.ProjectOnPlane(a - eye, Vector3.up);
        if (!closeSense && flat.sqrMagnitude > 0.0001f && Vector3.Angle(ViewDirection, flat) > halfAngle)
            return false;

        return LineClear(eye, a) || LineClear(eye, b);
    }

    private void GetPlayerSamplePoints(out Vector3 center, out Vector3 head)
    {
        if (playerCollider != null)
        {
            Bounds bounds = playerCollider.bounds;
            center = bounds.center;
            head = new Vector3(center.x, bounds.max.y - 0.15f, center.z);
        }
        else
        {
            center = player.position + Vector3.up;
            head = player.position + Vector3.up * 1.6f;
        }
    }

    private bool LineClear(Vector3 from, Vector3 to)
    {
        if (!Physics.Linecast(from, to, out RaycastHit hit, obstacleMask, QueryTriggerInteraction.Ignore)) return true;
        return hit.transform.root == player.root;
    }

    private void UpdateAwareness()
    {
        if (player == null) return;
        float dt = Time.deltaTime;

        if (canSeePlayer)
        {
            lastSeenPosition = player.position;
            lastSeenTime = Time.time;
            TrackPlayerVelocity(dt);

            float closeness = 1f - Mathf.Clamp01(Vector3.Distance(transform.position, player.position) / enemyData.sightRange);
            float fillRate = (100f / Mathf.Max(0.05f, enemyData.noticeTime)) * Mathf.Lerp(1f, CloseFillBoost, closeness);
            awarenessMeter = Mathf.MoveTowards(awarenessMeter, 100f, fillRate * dt);

            if (currentState == AwarenessState.Alerted)
            {
                searchEndTime = Mathf.Max(searchEndTime, Time.time + SearchDuration * 0.4f);
                if (Time.time - lastAggroTime < HuntMemoryTime) awarenessMeter = 100f;
            }
        }
        else
        {
            switch (currentState)
            {
                case AwarenessState.Passive:
                    awarenessMeter = Mathf.MoveTowards(awarenessMeter, 0f, 100f / Mathf.Max(0.05f, enemyData.forgetTime) * dt);
                    break;
                case AwarenessState.Alerted:
                    // If meter is zero the search ends
                    awarenessMeter = Mathf.MoveTowards(awarenessMeter, 0f,
                        awarenessMeter / Mathf.Max(0.1f, searchEndTime - Time.time) * dt);
                    break;
                case AwarenessState.Aggro:
                    UpdateLostTrack();
                    break;
            }
        }

        prevPlayerPos = player.position;

        switch (currentState)
        {
            case AwarenessState.Passive:
                if (awarenessMeter >= 100f) SetState(AwarenessState.Aggro);
                else if (awarenessMeter >= AlertedThreshold) SetState(AwarenessState.Alerted);
                break;
            case AwarenessState.Alerted:
                if (awarenessMeter >= 100f) SetState(AwarenessState.Aggro);
                else if (!canSeePlayer && Time.time >= searchEndTime) SetState(AwarenessState.Passive);
                break;
        }
    }

    private void TrackPlayerVelocity(float dt)
    {
        Vector3 v = (player.position - prevPlayerPos) / Mathf.Max(dt, 0.0001f);
        v.y = 0f;
        if (v.magnitude > 15f) v = Vector3.zero;
        lastSeenVelocity = Vector3.Lerp(lastSeenVelocity, v, 1f - Mathf.Exp(-8f * dt));
    }

    private void UpdateLostTrack()
    {
        float lost = Time.time - lastSeenTime;
        awarenessMeter = Mathf.Lerp(100f, 70f, lost / ChaseClueTimeout);
        if (lost < LoseTrackDelay) return;

        if (lost >= ChaseClueTimeout || (AgentReady && Arrived()))
            SetState(AwarenessState.Alerted);
    }

    private void SetState(AwarenessState next)
    {
        if (next == currentState) return;
        AwarenessState previous = currentState;
        currentState = next;
        bodyTurning = false;

        switch (next)
        {
            case AwarenessState.Aggro:
                lastAggroTime = Time.time;
                if (AgentReady) agent.isStopped = false;
                break;
            case AwarenessState.Alerted:
                BeginSearch(previous == AwarenessState.Aggro ? SearchDuration : SearchDuration * 0.4f);
                break;
            case AwarenessState.Passive:
                awarenessMeter = 0f;
                StartIdle();
                break;
        }
    }

    private Vector3 PredictedClue()
    {
        float speed = lastSeenVelocity.magnitude;
        if (speed < 1f) return lastSeenPosition;
        if (!NavMesh.SamplePosition(lastSeenPosition, out NavMeshHit start, GroundSampleRadius, agent.areaMask))
            return lastSeenPosition;

        float lead = Mathf.Clamp(speed * PredictionTime, PredictionMinLead, PredictionMaxLead);
        Vector3 end = start.position + lastSeenVelocity / speed * lead;
        end.y = start.position.y;

        if (NavMesh.Raycast(start.position, end, out NavMeshHit wall, agent.areaMask))
            return Vector3.MoveTowards(wall.position, start.position, 0.6f); // stop just short of the edge
        return end;
    }

    // Chasing

    public void StopNavigation()
    {
        if (!AgentReady) return;
        if (!agent.isStopped) agent.ResetPath();
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    // Aggro pursuit. With sight: close in on the player. Without: follow the predicted clue.
    // Unreachable targets (player on a box, mid-air) resolve to the nearest reachable point.
    public void ChasePlayer()
    {
        if (currentState != AwarenessState.Aggro || player == null || Busy || !AgentReady) return;

        float attackRange = combat != null ? combat.AttackRange : enemyData.combatRange;
        if (canSeePlayer && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            agent.isStopped = true;
            return;
        }

        if (Time.time < nextChaseUpdate) return;
        nextChaseUpdate = Time.time + ChaseRefreshTime;

        chaseGoal = canSeePlayer ? player.position : PredictedClue();
        SetGoal(chaseGoal, enemyData.runSpeed, canSeePlayer ? ChaseStopDistance : 0f);
    }

    // If the point is on an unreachable island the agent follows a partial path to the closest point.
    private bool SetGoal(Vector3 target, float speed, float stopShort = 0f)
    {
        if (!AgentReady) return false;

        if (stopShort > 0f)
        {
            Vector3 to = target - transform.position;
            to.y = 0f;
            float d = to.magnitude;
            target = d > stopShort ? target - to / d * stopShort : transform.position;
        }

        if (!NavMesh.SamplePosition(target, out NavMeshHit hit, GroundSampleRadius, agent.areaMask)) return false;

        agent.isStopped = false;
        agent.speed = speed;
        if (!agent.hasPath || (agent.destination - hit.position).sqrMagnitude > 0.25f)
            agent.SetDestination(hit.position);
        return true;
    }

    private bool Arrived()
    {
        return !agent.pathPending && (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance + ArrivalDistance);
    }

    // Roaming (patrol + search)

    private void UpdateRoaming()
    {
        if (escaping || currentState == AwarenessState.Aggro || !AgentReady) return;
        float dt = Time.deltaTime;
        phaseTimer += dt;

        if (currentState == AwarenessState.Alerted && canSeePlayer)
        {
            SetGoal(lastSeenPosition, SearchSpeed, ChaseStopDistance * 2f);
            roamPhase = RoamPhase.Moving;
            phaseTimer = 0f;
            return;
        }

        if (bodyTurning) RotateBody();

        switch (roamPhase)
        {
            case RoamPhase.Idle:
                if (!bodyTurning && phaseTimer >= idleDuration && !PickNextDestination()) StartIdle();
                break;

            case RoamPhase.Moving:
                targetYaw = Urgent ? Mathf.Sin(Time.time * 3.5f) * 55f : 0f;

                bool still = agent.velocity.sqrMagnitude < 0.01f && phaseTimer > 0.5f && !agent.pathPending && !Busy;
                stuckTimer = still ? stuckTimer + dt : 0f;

                if ((phaseTimer > 0.25f && Arrived()) || phaseTimer > MaxLegTime || stuckTimer > 1f)
                    StartLook();
                break;

            case RoamPhase.Looking:
                if (bodyTurning) { phaseTimer = 0f; break; }

                float t = Mathf.Clamp01(phaseTimer / lookDuration);
                targetYaw = Mathf.Sin(t * Mathf.PI * 2f) * lookAngle;
                if (t >= 1f)
                {
                    if (currentState == AwarenessState.Alerted && PickSearchSpot()) break;
                    StartIdle();
                }
                break;
        }
    }

    private bool PickNextDestination()
    {
        return currentState == AwarenessState.Passive ? PickPatrolOrWander() : PickSearchSpot();
    }

    private void StartIdle()
    {
        if (AgentReady) agent.isStopped = true;
        roamPhase = RoamPhase.Idle;
        phaseTimer = 0f;
        targetYaw = 0f;

        if (Urgent) idleDuration = 0.15f;
        else if (currentState == AwarenessState.Passive) idleDuration = Random.Range(2f, 4.5f);
        else idleDuration = Random.Range(0.35f, 0.75f);
    }

    private void StartMoving()
    {
        roamPhase = RoamPhase.Moving;
        phaseTimer = 0f;
        stuckTimer = 0f;
        targetYaw = 0f;
        bodyTurning = false;
    }

    private void StartLook()
    {
        if (AgentReady) agent.isStopped = true;
        roamPhase = RoamPhase.Looking;
        phaseTimer = 0f;

        bool urgent = Urgent;
        lookDuration = urgent ? Random.Range(0.4f, 0.8f) : Random.Range(0.7f, 1.2f);
        lookAngle = urgent ? Random.Range(70f, 100f) : Random.Range(45f, 70f);
        FaceOpenSpace();
    }

    // Searching

    private void BeginSearch(float duration)
    {
        searchStartTime = Time.time;
        searchEndTime = Time.time + duration;
        visitedCount = visitedNext = 0;

        searchCenter = PredictedClue();
        RememberSpot(searchCenter);

        if (AgentReady && SetGoal(searchCenter, SearchSpeed)) StartMoving();
        else StartLook();
    }

    private void RememberSpot(Vector3 p)
    {
        visited[visitedNext] = p;
        visitedNext = (visitedNext + 1) % visited.Length;
        visitedCount = Mathf.Min(visitedCount + 1, visited.Length);
    }

    private bool NearVisited(Vector3 p, float radius)
    {
        for (int i = 0; i < visitedCount; i++)
            if (FlatDistance(p, visited[i]) < radius) return true;
        return false;
    }

    // Pick spots in the direction the player was heading
    private bool PickSearchSpot()
    {
        if (!AgentReady) return false;

        float progress = Mathf.Clamp01((Time.time - searchStartTime) / SearchDuration);
        float radius = Mathf.Lerp(SearchRadiusMin, SearchRadiusMax, progress);
        bool hasHeading = lastSeenVelocity.sqrMagnitude > 0.25f;
        Vector3 heading = hasHeading ? lastSeenVelocity.normalized : Vector3.zero;
        Vector3 eye = EyePosition;

        float bestScore = float.MinValue;
        Vector3 bestPoint = Vector3.zero;

        for (int i = 0; i < 10; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, Random.value * 360f, 0f) * Vector3.forward;
            Vector3 raw = searchCenter + dir * (Random.Range(0.5f, 1f) * radius);

            if (!ReachablePoint(raw, out Vector3 p, out float travel)) continue;
            if (FlatDistance(p, transform.position) < SearchMinLeg || NearVisited(p, 4f)) continue;

            float score = Random.value * 0.5f - travel * 0.05f;
            if (hasHeading) score += Vector3.Dot(dir, heading) * 1.5f * (1f - progress * 0.5f);
            if (Physics.Linecast(eye, p + Vector3.up * 1.2f, obstacleMask, QueryTriggerInteraction.Ignore)) score += 1f;

            if (score > bestScore) { bestScore = score; bestPoint = p; }
        }

        if (bestScore == float.MinValue) return false;

        RememberSpot(bestPoint);
        if (!SetGoal(bestPoint, SearchSpeed)) return false;
        StartMoving();
        return true;
    }

    private bool ReachablePoint(Vector3 raw, out Vector3 point, out float length)
    {
        point = raw;
        length = 0f;
        if (!NavMesh.SamplePosition(raw, out NavMeshHit hit, 2f, agent.areaMask)) return false;
        if (!agent.CalculatePath(hit.position, scratchPath) || scratchPath.status != NavMeshPathStatus.PathComplete) return false;

        point = hit.position;
        Vector3[] c = scratchPath.corners;
        for (int i = 1; i < c.Length; i++) length += Vector3.Distance(c[i - 1], c[i]);
        return true;
    }

    // Passive patrol

    private bool PickPatrolOrWander()
    {
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            for (int tries = 0; tries < patrolPoints.Length; tries++)
            {
                Transform point = patrolPoints[patrolIndex];
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                if (point != null && SetGoal(point.position, enemyData.walkSpeed))
                {
                    StartMoving();
                    return true;
                }
            }
            return false;
        }

        for (int i = 0; i < 8; i++)
        {
            Vector2 r = Random.insideUnitCircle * PassiveRoamRadius;
            if (!ReachablePoint(homePosition + new Vector3(r.x, 0f, r.y), out Vector3 p, out _)) continue;
            if (FlatDistance(p, transform.position) < 2f) continue;
            if (NavMesh.FindClosestEdge(p, out NavMeshHit edge, agent.areaMask) && edge.distance < WallClearance) continue;

            SetGoal(p, enemyData.walkSpeed);
            StartMoving();
            return true;
        }
        return false;
    }

    // Collider safeguard

    // NavMeshAgents ignore ordinary colliders that aren't in the NavMesh.
    // Every few frames, sweep capsule along the agent's actual velocity.
    // If it's about to hit a solid, non-floor collider, stop, carve that collider
    // into the NavMesh and let the agent repath around it.
    private void GuardAgainstUnbakedColliders()
    {
        if (!AgentReady) return;

        if (guardHoldUntil > 0f)
        {
            if (Time.time < guardHoldUntil) { agent.isStopped = true; return; }
            guardHoldUntil = 0f;
            if (!Busy) agent.isStopped = false;
        }

        if (escaping || agent.isStopped || Time.time < nextGuardCheck) return;
        nextGuardCheck = Time.time + GuardInterval;

        Vector3 velocity = agent.velocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;
        if (speed < 0.2f) return;

        GetAgentCapsule(out Vector3 bottom, out Vector3 top, out float radius);
        int count = Physics.CapsuleCastNonAlloc(bottom, top, radius, velocity / speed, guardHits,
            Mathf.Max(0.5f, speed * 0.3f), obstacleMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = guardHits[i];
            Collider c = hit.collider;
            if (hit.distance <= 0f || hit.normal.y > 0.5f) continue;
            if (IsOwnOrPlayer(c) || !IsSolidObstacle(c)) continue;

            if (!CarveIntoNavMesh(c)) stuckTimer = 2f;
            guardHoldUntil = Time.time + GuardHoldTime;
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
            return;
        }
    }

    private void GetAgentCapsule(out Vector3 bottom, out Vector3 top, out float radius)
    {
        radius = Mathf.Max(0.12f, agent.radius * 0.85f);
        float bottomY = radius + GuardStepHeight;
        float topY = Mathf.Max(bottomY, agent.height - radius);
        bottom = transform.position + Vector3.up * bottomY;
        top = transform.position + Vector3.up * topY;
    }

    private static bool IsSolidObstacle(Collider c)
    {
        if (c.GetComponentInParent<NavMeshAgent>() != null || c.GetComponentInParent<CharacterController>() != null)
            return false;
        Vector3 size = c.bounds.size;
        return Mathf.Max(size.x, size.z) >= 0.3f;
    }

    private static bool CarveIntoNavMesh(Collider c)
    {
        NavMeshObstacle obstacle = c.GetComponent<NavMeshObstacle>();
        if (obstacle == null)
        {
            if (c is BoxCollider box)
            {
                obstacle = c.gameObject.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.center = box.center;
                obstacle.size = box.size;
            }
            else if (c is CapsuleCollider capsule && capsule.direction == 1)
            {
                obstacle = c.gameObject.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Capsule;
                obstacle.center = capsule.center;
                obstacle.radius = capsule.radius;
                obstacle.height = capsule.height;
            }
            else if (c is SphereCollider sphere)
            {
                obstacle = c.gameObject.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Capsule;
                obstacle.center = sphere.center;
                obstacle.radius = sphere.radius;
                obstacle.height = sphere.radius * 2f;
            }
            else if (c.bounds.size.magnitude < 6f)
            {
                Vector3 scale = c.transform.lossyScale;
                if (Mathf.Approximately(scale.x, 0f) || Mathf.Approximately(scale.y, 0f) || Mathf.Approximately(scale.z, 0f)) return false;
                obstacle = c.gameObject.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.center = c.transform.InverseTransformPoint(c.bounds.center);
                obstacle.size = new Vector3(c.bounds.size.x / scale.x, c.bounds.size.y / scale.y, c.bounds.size.z / scale.z);
            }
            else return false;
        }

        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;
        obstacle.carvingTimeToStationary = 0.1f;
        return true;
    }

    private float Clearance(Vector3 dir, float max = LookAheadDistance * 2f)
    {
        if (Physics.SphereCast(transform.position + Vector3.up, 0.25f, dir, out RaycastHit hit, max, obstacleMask, QueryTriggerInteraction.Ignore)
            && !IsOwnOrPlayer(hit.collider))
            return hit.distance;
        return max;
    }

    // Don't stand staring at a wall while "looking around"
    private void FaceOpenSpace()
    {
        if (Clearance(transform.forward) >= LookAheadDistance) return;

        float currentYaw = transform.eulerAngles.y;
        float bestScore = float.MinValue, bestYaw = currentYaw;
        for (int i = 1; i < 12; i++)
        {
            float yaw = currentYaw + i * 30f;
            float turnCost = Mathf.Abs(Mathf.DeltaAngle(currentYaw, yaw)) / 180f;
            float score = Clearance(Quaternion.Euler(0f, yaw, 0f) * Vector3.forward) - turnCost;
            if (score > bestScore) { bestScore = score; bestYaw = yaw; }
        }

        bodyTargetYaw = bestYaw;
        bodyTurning = true;
    }

    private void RotateBody()
    {
        Quaternion target = Quaternion.Euler(0f, bodyTargetYaw, 0f);
        float speed = Urgent ? BodyTurnSpeed * 2f : BodyTurnSpeed;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, speed * Time.deltaTime);
        if (Quaternion.Angle(transform.rotation, target) < 1f) bodyTurning = false;
    }

    private bool IsOwnOrPlayer(Collider c)
    {
        if (c == null || c.isTrigger) return true;
        Transform root = c.transform.root;
        return root == transform.root || (player != null && root == player.root);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // Head bones


    private void UpdateHead()
    {
        // Inside combat range, EnemyCombat owns body rotation through FacePlayer()
        // Do not rotate the root, two independent rotation calls the
        // same frame can make the archer overshoot or appear to turn inconsistently
        if (combat != null &&
            currentState == AwarenessState.Aggro &&
            canSeePlayer &&
            player != null &&
            FlatDistance(transform.position, player.position) <= combat.AttackRange)
        {
            headYaw = 0f;
            headPitch = 0f;
            targetYaw = 0f;
            targetPitch = 0f;
            return;
        }

        // Head tracking behaviour
        bool hasAim = false;
        Vector3 aimPoint = Vector3.zero;

        if (canSeePlayer && currentState != AwarenessState.Passive && player != null)
        {
            aimPoint = player.position + targetOffset;
            hasAim = true;
        }
        else if (currentState == AwarenessState.Aggro)
        {
            aimPoint = lastSeenPosition + targetOffset;
            hasAim = true;
        }

        float rate = CalmHeadRate;

        if (hasAim)
        {
            Vector3 aim = aimPoint - EyePosition;
            Vector3 flat = new Vector3(aim.x, 0f, aim.z);

            if (flat.sqrMagnitude > 0.0001f)
            {
                float yaw = Vector3.SignedAngle(transform.forward, flat, Vector3.up);

                targetYaw = Mathf.Clamp(
                    yaw,
                    -AggroHeadMaxYaw,
                    AggroHeadMaxYaw
                );

                targetPitch = Mathf.Clamp(
                    Mathf.Atan2(aim.y, flat.magnitude) * Mathf.Rad2Deg,
                    -HeadMaxPitch,
                    HeadMaxPitch
                );

                if (agent.velocity.sqrMagnitude < 0.04f &&
                    !Busy &&
                    Mathf.Abs(yaw) > 60f)
                {
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        Quaternion.LookRotation(flat),
                        AggroBodyTurnSpeed * Time.deltaTime
                    );
                }
            }

            rate = AggroHeadRate;
        }
        else
        {
            // Return both axes to neutral when there is no current aim
            // Leaving targetYaw unchanged locks the head toward previous target
            targetYaw = 0f;
            targetPitch = 0f;

            if (Urgent)
                rate = UrgentHeadRate;
        }

        float blend = 1f - Mathf.Exp(-rate * Time.deltaTime);

        headYaw = Mathf.Lerp(headYaw, targetYaw, blend);
        headPitch = Mathf.Lerp(headPitch, targetPitch, blend);
    }

    private void UpdateAnimation()
    {
        if (enemyAnimator == null) return;
        enemyAnimator.SetFloat(SpeedHash, agent.velocity.magnitude, 0.1f, Time.deltaTime);
    }

    // Headstand escape (Player stands on head this avoids surfing)

    private void UpdateHeadstandEscape()
    {
        if (escaping) { RunEscape(); return; }

        if (player == null || Time.time < nextEscapeTime || Busy || !IsPlayerOnHead())
        {
            headstandTimer = 0f;
            return;
        }

        headstandTimer += Time.deltaTime;
        if (headstandTimer >= HeadstandGraceTime) TryStartEscape();
    }

    private bool IsPlayerOnHead()
    {
        Vector3 playerCenter = playerCollider != null ? playerCollider.bounds.center : player.position;
        Vector3 flat = playerCenter - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude > HeadstandRadius * HeadstandRadius) return false;

        float headTop = rootCollider != null && rootCollider.enabled
            ? rootCollider.bounds.max.y
            : transform.position.y + 1.8f;
        float playerFeet = playerCollider != null ? playerCollider.bounds.min.y : player.position.y;
        return playerFeet >= headTop - 0.25f && playerFeet <= headTop + 1.5f;
    }

    private void TryStartEscape()
    {
        if (!AgentReady || Busy) return;

        Vector3 away = transform.position - (playerCollider != null ? playerCollider.bounds.center : player.position);
        away.y = 0f;
        away = away.sqrMagnitude < 0.01f ? -transform.forward : away.normalized;

        float baseYaw = Mathf.Atan2(away.x, away.z) * Mathf.Rad2Deg;
        float bestScore = float.MinValue;
        Vector3 best = Vector3.zero;
        Vector3 origin = transform.position + Vector3.up * 0.7f;

        for (int i = 0; i < 12; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, baseYaw + i * 30f, 0f) * Vector3.forward;
            float clear = EscapeDistance;

            if (NavMesh.Raycast(transform.position, transform.position + dir * EscapeDistance, out NavMeshHit nav, agent.areaMask))
                clear = nav.distance;

            foreach (RaycastHit hit in Physics.SphereCastAll(origin, agent.radius * 0.9f, dir, EscapeDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                if (!IsOwnOrPlayer(hit.collider)) clear = Mathf.Min(clear, hit.distance - 0.15f);
            }

            float score = clear + Vector3.Dot(dir, away) * 0.35f;
            if (score > bestScore) { bestScore = score; best = dir; }
        }

        if (bestScore < 0.65f) return;

        escaping = true;
        headstandTimer = 0f;
        escapeStartTime = Time.time;
        escapeStartPosition = transform.position;
        escapeDirection = best;

        if (agent.hasPath) agent.ResetPath();
        agent.velocity = Vector3.zero;
        agent.isStopped = true;
        if (escapeTriggerExists) enemyAnimator.SetTrigger(EscapeHash);
    }

    private void RunEscape()
    {
        if (!AgentReady) { EndEscape(); return; }

        float t = Mathf.Clamp01((Time.time - escapeStartTime) / Mathf.Max(0.05f, EscapeDuration));
        Vector3 desired = escapeStartPosition
                        + escapeDirection * EscapeDistance * t
                        + Vector3.up * (Mathf.Sin(t * Mathf.PI) * EscapeArcHeight);
        agent.Move(desired - transform.position);

        if (t >= 1f) EndEscape();
    }

    private void EndEscape()
    {
        escaping = false;
        headstandTimer = 0f;
        nextEscapeTime = Time.time + EscapeCooldown;

        if (AgentReady)
        {
            agent.velocity = Vector3.zero;
            agent.isStopped = false;
            agent.ResetPath();
        }
    }

    // Editor Gizmos

    private void OnDrawGizmosSelected()
    {
        if (enemyData == null) return;

        Gizmos.color = currentState switch
        {
            AwarenessState.Aggro => Color.red,
            AwarenessState.Alerted => Color.yellow,
            _ => Color.green
        };

        float range = enemyData.sightRange;
        float halfAngle = enemyData.sightAngle * 0.5f;
        Vector3 eye = EyePosition;
        Vector3 prev = eye + Quaternion.AngleAxis(-halfAngle, Vector3.up) * ViewDirection * range;
        Gizmos.DrawLine(eye, prev);
        for (int i = 1; i <= 20; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, i / 20f);
            Vector3 next = eye + Quaternion.AngleAxis(angle, Vector3.up) * ViewDirection * range;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
        Gizmos.DrawLine(eye, prev);

        if (!Application.isPlaying) return;

        Gizmos.color = Color.white;                       // where it last saw the player
        Gizmos.DrawWireSphere(lastSeenPosition, 0.3f);
        Gizmos.color = Color.cyan;                        // where AI is going and searching from
        Gizmos.DrawWireSphere(chaseGoal, 0.25f);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(searchCenter, 0.35f);
    }
}