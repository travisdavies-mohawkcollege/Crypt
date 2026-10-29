using UnityEngine;
using UnityEngine.AI;
using Unity.Behavior;

public enum AwarenessState { Passive, Alerted, Aggro }

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(BehaviorGraphAgent))]
public class EnemyAI : MonoBehaviour, IDamageable
{
    [Header("Setup")]
    public EnemyData enemyData;
    [Min(1)] public int level = 1;
    public bool autoInitialize = true;

    [Header("Runtime State")]
    public AwarenessState currentState = AwarenessState.Passive;
    public float currentHealth;
    public bool isInitialized = false;
    public bool isDead = false;

    [Header("Vision & Detection")]
    public Transform player;
    public Vector3 eyeOffset = new Vector3(0f, 1.6f, 0f);
    public Vector3 targetOffset = new Vector3(0f, 1f, 0f);
    public float visionYaw = 0f;
    public LayerMask obstacleMask = ~0;

    [Header("Awareness Tuning")]
    [Range(0f, 100f)] public float awarenessMeter = 0f;
    public float closeFillBoost = 2.5f;
    public float alertedThreshold = 25f;
    public float aggroDropThreshold = 70f;

    [Header("Roaming & Searching")]
    public float passiveRoamRadius = 8f;
    public Vector2 passiveIdleTime = new Vector2(2f, 4f);
    public float searchRadius = 4f;
    public int searchSpots = 3;
    public float lookTime = 3f;
    public float lookAngle = 60f;

    [Header("Wall Avoidance")]
    public float wallClearance = 1.5f;     // Wander spots in open rooms stay this further away from walls
    public float lookAheadDistance = 2f;   // A wall closer in front makes the AI turn away
    public float bodyTurnSpeed = 90f;      // Degrees per second when turning away from a wall
    public float hallwayWidth = 4.5f;      // Spaces narrower than this count as hallways

    [Header("Head Turning")]
    public Transform headBone;
    public float headTurnSpeed = 8f;

    [Header("Combat")]
    public float iFrameTime = 0.25f;
    public float deathDelay = 3f;

    [Header("Melee")]
    public float attackCooldown = 1.5f;
    public float hitRangeBuffer = 1f;
    public float hitDelay = 0.5f;          // Seconds after the swing starts before damage lands
    public float chaseStopDistance = 1.6f; // How close to the player center is chases before attacking

    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public BehaviorGraphAgent behaviorAgent;
    [HideInInspector] public Vector3 lastSeenPosition;
    [HideInInspector] public bool canSeePlayer;

    private Animator enemyAnimator;
    private EnemyStats stats;
    private float nextDamageTime;
    private float nextAttackTime;
    private float pendingHitTime = -1f;
    private float nextChaseRefresh;

    private enum RoamPhase { Idle, Moving, Looking }
    private RoamPhase roamPhase = RoamPhase.Idle;
    private AwarenessState lastRoamState = AwarenessState.Passive;
    private NavMeshPath roamPath;
    private Vector3 homePosition;
    private float phaseTimer;
    private float idleDuration;
    private int searchLeft;

    private bool bodyTurning;
    private float bodyTargetYaw;
    private float nextWallCheck;

    private float headYaw;
    private float lookTargetYaw;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DieHash = Animator.StringToHash("Die");

    // Vision cone follows the AI head
    private Vector3 ViewDirection => Quaternion.Euler(0f, visionYaw + headYaw, 0f) * transform.forward;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        behaviorAgent = GetComponent<BehaviorGraphAgent>();
        enemyAnimator = GetComponentInChildren<Animator>();
        roamPath = new NavMeshPath();

        // Finds the head bone at runtime
        if (headBone == null && enemyAnimator != null && enemyAnimator.isHuman)
        {
            headBone = enemyAnimator.GetBoneTransform(HumanBodyBones.Head);
        }
    }

    private void Start()
    {
        if (isInitialized || !autoInitialize) return;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) InitializeEnemy(playerObj, level);
    }

    private void Update()
    {
        if (!isInitialized || isDead) return;

        canSeePlayer = CheckVision();
        UpdateDetection(canSeePlayer);
        UpdateRoaming();
        KeepChasing();
        UpdateHead();
        UpdateBlackboard();
        UpdateAnimation();
        TryMeleeAttack();
        ResolvePendingHit();
    }

    // Turns head bone after animation played
    private void LateUpdate()
    {
        if (headBone == null || isDead || Mathf.Abs(headYaw) < 0.01f) return;
        headBone.rotation = Quaternion.AngleAxis(headYaw, Vector3.up) * headBone.rotation;
    }

    public void InitializeEnemy(GameObject playerObj, int enemyLevel, EnemyData dataOverride = null)
    {
        if (dataOverride != null) enemyData = dataOverride;

        if (enemyData == null)
        {
            Debug.LogError($"[{name}] Missing EnemyData assignment!");
            return;
        }

        if (playerObj != null) player = playerObj.transform;

        level = Mathf.Clamp(enemyLevel, 1, enemyData.maxLevel);
        stats = enemyData.GetStats(level);

        currentHealth = stats.maxHealth;
        agent.speed = enemyData.walkSpeed;
        isDead = false;

        homePosition = transform.position;
        lastRoamState = currentState;
        StartIdle();

        isInitialized = true;
        UpdateBlackboard();
        behaviorAgent.enabled = true;
    }

    public void TakeDamage(float damage)
    {
        if (!isInitialized || isDead || Time.time < nextDamageTime) return;

        nextDamageTime = Time.time + iFrameTime;
        currentHealth -= damage;

        if (DamageNumberPool.Instance != null)
        {
            DamageNumberPool.Instance.Show(transform.position + Vector3.up * 1.5f, damage);
        }

        if (player != null) ForceAlert(player.position);
        if (currentHealth <= 0f) Die();
    }

    private void Die()
    {
        isDead = true;
        headYaw = 0f;
        lookTargetYaw = 0f;
        pendingHitTime = -1f;
        behaviorAgent.SetVariableValue("Dead", true);

        if (agent.isActiveAndEnabled)
        {
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
        }

        if (enemyAnimator != null) enemyAnimator.SetTrigger(DieHash);

        // Stops dead body collision from blocking the player
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, deathDelay);
    }

    public void ForceAlert(Vector3 position)
    {
        lastSeenPosition = position;
        awarenessMeter = 100f;
        UpdateAwarenessState();
    }

    private void UpdateRoaming()
    {
        if (currentState != lastRoamState)
        {
            lastRoamState = currentState;
            bodyTurning = false;

            if (currentState == AwarenessState.Alerted) StartSearch();
            else if (currentState == AwarenessState.Passive) StartIdle();
            else if (currentState == AwarenessState.Aggro && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = false; // Hands movement to the behaviour graph
            }
        }

        if (currentState == AwarenessState.Aggro)
        {
            lookTargetYaw = 0f; // Faces forward while chasing player
            return;
        }
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        if (bodyTurning) RotateBody();

        phaseTimer += Time.deltaTime;

        switch (roamPhase)
        {
            case RoamPhase.Idle:
                {
                    // If AI ends up staring at a wall, turn toward open space
                    if (!bodyTurning && Time.time >= nextWallCheck)
                    {
                        nextWallCheck = Time.time + 0.5f;
                        CheckFacingWall();
                    }

                    if (currentState == AwarenessState.Passive && !bodyTurning && phaseTimer >= idleDuration)
                    {
                        if (!PickAndMove(homePosition, passiveRoamRadius, enemyData.walkSpeed)) StartIdle();
                    }
                    break;
                }

            case RoamPhase.Moving:
                {
                    bool arrived = phaseTimer > 0.2f && !agent.pathPending &&
                                   agent.remainingDistance <= agent.stoppingDistance + 0.2f;
                    if (arrived || phaseTimer > 10f) StartLook();
                    break;
                }

            case RoamPhase.Looking:
                {
                    // Turns away from a wall before moving its headbone
                    if (bodyTurning)
                    {
                        phaseTimer = 0f;
                        break;
                    }

                    float t = Mathf.Clamp01(phaseTimer / Mathf.Max(0.1f, lookTime));
                    lookTargetYaw = Mathf.Sin(t * Mathf.PI * 2f) * lookAngle;
                    if (t >= 1f) FinishLook();
                    break;
                }
        }
    }

    // Chases player if they back out of reach
    private void KeepChasing()
    {
        if (currentState != AwarenessState.Aggro || !canSeePlayer || player == null) return;
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
        if (Time.time < nextChaseRefresh) return;
        nextChaseRefresh = Time.time + 0.2f;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        // If the AI is close enough to attack, don't walk into the player (Prevents collider hitbox constantly bumping player)
        if (toPlayer.magnitude <= enemyData.combatRange + hitRangeBuffer) return;

        bool stalled = agent.isStopped ||
                       (!agent.pathPending && !agent.hasPath) ||
                       (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f);
        if (!stalled) return;

        // Stops close to the player
        Vector3 stopPoint = player.position - toPlayer.normalized * chaseStopDistance;
        if (NavMesh.SamplePosition(stopPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.speed = enemyData.runSpeed;
            agent.SetDestination(hit.position);
        }
    }

    // How far the skeleton can see in a direction before hitting something
    private float ClearanceAhead(Vector3 dir, float maxDist = -1f)
    {
        if (maxDist < 0f) maxDist = lookAheadDistance * 2f;
        Vector3 origin = transform.position + Vector3.up;

        if (Physics.SphereCast(origin, 0.25f, dir, out RaycastHit hit, maxDist, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (player != null && hit.transform.root == player.root) return maxDist;
            return hit.distance;
        }
        return maxDist;
    }

    // If a wall is right in front of the AI, picks the most open direction to turn toward it
    private void CheckFacingWall()
    {
        if (ClearanceAhead(transform.forward) >= lookAheadDistance) return;

        float currentYaw = transform.eulerAngles.y;
        float bestScore = float.MinValue;
        float bestYaw = currentYaw;

        for (int i = 1; i < 12; i++)
        {
            float yaw = currentYaw + i * 30f;
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;

            float clear = ClearanceAhead(dir);
            float turnCost = Mathf.Abs(Mathf.DeltaAngle(currentYaw, yaw)) / 180f;
            float score = clear - turnCost;

            if (score > bestScore)
            {
                bestScore = score;
                bestYaw = yaw;
            }
        }

        bodyTargetYaw = bestYaw;
        bodyTurning = true;
    }

    private void RotateBody()
    {
        Quaternion target = Quaternion.Euler(0f, bodyTargetYaw, 0f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, bodyTurnSpeed * Time.deltaTime);
        if (Quaternion.Angle(transform.rotation, target) < 1f) bodyTurning = false;
    }

    private void UpdateHead()
    {
        float blend = 1f - Mathf.Exp(-headTurnSpeed * Time.deltaTime);
        headYaw = Mathf.Lerp(headYaw, lookTargetYaw, blend);
    }

    private void StartSearch()
    {
        searchLeft = searchSpots;
        if (MoveTo(lastSeenPosition, enemyData.alertedSpeed)) return;
        StartLook();
    }

    private void StartIdle()
    {
        if (agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
        roamPhase = RoamPhase.Idle;
        phaseTimer = 0f;
        lookTargetYaw = 0f;
        idleDuration = Random.Range(passiveIdleTime.x, passiveIdleTime.y);
    }

    private void StartLook()
    {
        if (agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
        roamPhase = RoamPhase.Looking;
        phaseTimer = 0f;
        CheckFacingWall();
    }

    private void FinishLook()
    {
        if (currentState == AwarenessState.Alerted && searchLeft > 0)
        {
            searchLeft--;
            if (PickAndMove(lastSeenPosition, searchRadius, enemyData.alertedSpeed)) return;
        }
        StartIdle();
    }

    // Walks along the dungeon hallway, otherwise picks a random spot away from walls
    private bool PickAndMove(Vector3 center, float radius, float speed)
    {
        if (TryHallwayMove(center, radius, speed)) return true;

        for (int i = 0; i < 8; i++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            float dist = Random.Range(0.4f, 1f) * radius;
            Vector3 target = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dist;

            if (!NavMesh.SamplePosition(target, out NavMeshHit pos, 2f, NavMesh.AllAreas)) continue;

            if (NavMesh.FindClosestEdge(pos.position, out NavMeshHit edge, NavMesh.AllAreas) &&
                edge.distance < wallClearance) continue;

            if (MoveTo(pos.position, speed)) return true;
        }
        return false;
    }

    // In narrow spaces AI finds the direction of the hallway and walks up or down it instead of side to side (Softlocking AI movement)
    private bool TryHallwayMove(Vector3 center, float radius, float speed)
    {
        float maxDist = Mathf.Max(radius, lookAheadDistance * 2f);

        float bestLength = -1f;
        Vector3 axis = Vector3.forward;
        for (int i = 0; i < 6; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, i * 30f, 0f) * Vector3.forward;
            float length = ClearanceAhead(dir, maxDist) + ClearanceAhead(-dir, maxDist);
            if (length > bestLength)
            {
                bestLength = length;
                axis = dir;
            }
        }

        // Counts space across dungeon axis that is wide as a room
        Vector3 across = Vector3.Cross(Vector3.up, axis);
        float width = ClearanceAhead(across, maxDist) + ClearanceAhead(-across, maxDist);
        if (width > hallwayWidth) return false;

        Vector3 first = Random.value < 0.5f ? axis : -axis;
        Vector3[] options = { first, -first };

        foreach (Vector3 dir in options)
        {
            float clear = ClearanceAhead(dir, maxDist) - wallClearance;
            if (clear < 1.5f) continue;

            float dist = Random.Range(0.5f, 1f) * Mathf.Min(clear, radius);
            Vector3 target = transform.position + dir * dist;

            if (Vector3.Distance(target, center) > radius) continue;
            if (!NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas)) continue;
            if (MoveTo(hit.position, speed)) return true;
        }
        return false;
    }

    private bool MoveTo(Vector3 target, float speed)
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return false;
        if (!NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas)) return false;
        if (!agent.CalculatePath(hit.position, roamPath) || roamPath.status != NavMeshPathStatus.PathComplete) return false;

        agent.isStopped = false;
        agent.speed = speed;
        agent.SetPath(roamPath);
        roamPhase = RoamPhase.Moving;
        phaseTimer = 0f;
        lookTargetYaw = 0f; // Faces forward while walking
        bodyTurning = false;
        return true;
    }


    private void UpdateAnimation()
    {
        if (enemyAnimator == null) return;
        enemyAnimator.SetFloat(SpeedHash, agent.velocity.magnitude, 0.1f, Time.deltaTime);
    }

    private void TryMeleeAttack()
    {
        if (currentState != AwarenessState.Aggro || player == null || enemyAnimator == null) return;

        Vector3 flat = player.position - transform.position;
        flat.y = 0f;

        float attackRange = enemyData.combatRange + hitRangeBuffer;
        if (flat.magnitude > attackRange) return;

        // Faces the player while in range
        if (flat.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(flat),
                10f * Time.deltaTime);
        }

        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackCooldown;
        pendingHitTime = Time.time + hitDelay;
        enemyAnimator.SetTrigger(AttackHash);
    }

    // If the player is in melee range, hits shortly after attack animation
    private void ResolvePendingHit()
    {
        if (pendingHitTime < 0f || Time.time < pendingHitTime) return;
        pendingHitTime = -1f;

        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) > enemyData.combatRange + hitRangeBuffer) return;

        IDamageable target = player.GetComponentInParent<IDamageable>();
        if (target != null) target.TakeDamage(stats.damage);
    }


    private bool CheckVision()
    {
        if (player == null) return false;

        Vector3 eye = transform.TransformPoint(eyeOffset);
        Vector3 aim = player.position + targetOffset;
        Vector3 toPlayer = aim - eye;

        if (toPlayer.magnitude > enemyData.sightRange) return false;

        Vector3 flatDir = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
        if (flatDir.sqrMagnitude > 0.0001f && Vector3.Angle(ViewDirection, flatDir) > enemyData.sightAngle * 0.5f)
        {
            return false;
        }

        // AI LoS check
        if (Physics.Linecast(eye, aim, out RaycastHit hit, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return hit.transform.root == player.root;
        }

        return true;
    }

    private void UpdateDetection(bool isVisible)
    {
        if (isVisible)
        {
            lastSeenPosition = player.position;

            float distance = Vector3.Distance(transform.position, player.position);
            float closeness = 1f - Mathf.Clamp01(distance / enemyData.sightRange);
            float fillRate = (100f / Mathf.Max(0.05f, enemyData.noticeTime)) * Mathf.Lerp(1f, closeFillBoost, closeness);

            awarenessMeter = Mathf.MoveTowards(awarenessMeter, 100f, fillRate * Time.deltaTime);
        }
        else
        {
            float decayRate = 100f / Mathf.Max(0.05f, enemyData.forgetTime);
            awarenessMeter = Mathf.MoveTowards(awarenessMeter, 0f, decayRate * Time.deltaTime);
        }

        UpdateAwarenessState();
    }

    private void UpdateAwarenessState()
    {
        switch (currentState)
        {
            case AwarenessState.Passive:
                if (awarenessMeter >= 100f) currentState = AwarenessState.Aggro;
                else if (awarenessMeter >= alertedThreshold) currentState = AwarenessState.Alerted;
                break;

            case AwarenessState.Alerted:
                if (awarenessMeter >= 100f) currentState = AwarenessState.Aggro;
                else if (awarenessMeter < alertedThreshold * 0.5f) currentState = AwarenessState.Passive;
                break;

            case AwarenessState.Aggro:
                if (awarenessMeter < aggroDropThreshold) currentState = AwarenessState.Alerted;
                break;
        }
    }

    private void UpdateBlackboard()
    {
        if (behaviorAgent == null || enemyData == null) return;

        behaviorAgent.SetVariableValue("Player", player != null ? player.gameObject : null);
        behaviorAgent.SetVariableValue("CanSeePlayer", canSeePlayer);
        behaviorAgent.SetVariableValue("Alerted", currentState == AwarenessState.Alerted);
        behaviorAgent.SetVariableValue("Aggro", currentState == AwarenessState.Aggro);
        behaviorAgent.SetVariableValue("Dead", isDead);
        behaviorAgent.SetVariableValue("LastSeenPosition", lastSeenPosition);
        behaviorAgent.SetVariableValue("WalkSpeed", enemyData.walkSpeed);
        behaviorAgent.SetVariableValue("AlertedSpeed", enemyData.alertedSpeed);
        behaviorAgent.SetVariableValue("RunSpeed", enemyData.runSpeed);
        behaviorAgent.SetVariableValue("CombatRange", enemyData.combatRange);
    }

    private void OnDrawGizmosSelected()
    {
        if (enemyData == null) return;

        float range = enemyData.sightRange;
        float halfAngle = enemyData.sightAngle * 0.5f;
        Vector3 eye = transform.TransformPoint(eyeOffset);

        Gizmos.color = currentState switch
        {
            AwarenessState.Aggro => Color.red,
            AwarenessState.Alerted => Color.yellow,
            _ => Color.green
        };

        Vector3 prevPoint = eye + Quaternion.AngleAxis(-halfAngle, Vector3.up) * ViewDirection * range;
        Gizmos.DrawLine(eye, prevPoint);

        int segments = 20;
        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)segments);
            Vector3 nextPoint = eye + Quaternion.AngleAxis(angle, Vector3.up) * ViewDirection * range;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
        Gizmos.DrawLine(eye, prevPoint);

        if (Application.isPlaying && player != null)
        {
            Gizmos.color = canSeePlayer ? Color.cyan : Color.magenta;
            Gizmos.DrawLine(eye, player.position + targetOffset);
        }
    }
}