using UnityEngine;

// Archer-specific behaviour: bowstring, drawing, shooting and dodging.
public class ArcherCombat : EnemyCombat
{
    [Header("Arrow")]
    public EnemyProjectile arrowPrefab;
    public Vector3 arrowModelRotationOffset = new Vector3(0f, 180f, 0f);
    public bool spawnAtString = true;

    [Header("Arrow Aim")]
    public float aimHeightOffset = -0.5f;

    [Header("Bowstring")]
    public string bowstringObjectName = "BowString";
    public Transform bowstringPullHand;
    public Vector3 pullHandOffset;
    [Min(0.01f)] public float maxPullDistance = 1f;
    [Header("Bowstring Deformation")]
    public BowstringAxis stringLengthAxis = BowstringAxis.Auto;
    public enum BowstringAxis { Auto, X, Y, Z }
    public bool debugBowstring = false;
    private bool warnedZeroPull;

    [Header("Archer")]
    [Min(0f)] public float dodgeCooldown = 3f;
    [SerializeField] private string drawStateName = "Attack";

    [Range(0f, 1f)]
    public float arrowAppearTime = 0.2f;
    [Range(0f, 1f)]
    [Tooltip("When the bowstring starts following the drawing hand. Independent of Arrow Appear Time.")]
    public float bowstringDrawStartTime = 0.2f;
    private const float ArcherAttackEnd = 0.98f;
    private const float BlendInTime = 0.1f;
    private const float BlendOutTime = 0.15f;

    protected override bool SupportsDodge => true;
    protected override float DodgeCooldown => dodgeCooldown;
    protected override string DrawStateName => drawStateName;
    protected override float DrawEnd => ArcherAttackEnd;

    private SkinnedMeshRenderer bowstringRenderer;
    private Mesh bowstringMesh;
    private Vector3[] originalVertices;
    private Vector3[] workingVertices;
    private float[] weights;
    private Vector3 localMidpoint;
    private Vector3 localAxis;
    private bool stringBent;
    private float drawWeight;

    [Header("Arrow Release")]
    [Range(0f, 1f)]
    public float arrowReleaseTime = 0.8f;
    public Vector3 heldArrowLocalPosition;
    public Vector3 heldArrowLocalRotation;

    public bool alignHeldArrowToBowstring = true;

    private EnemyProjectile heldArrow;
    private bool arrowReleasedThisAttack;
    private bool arrowCreatedThisAttack;

    public override void UpdateMovement()
    {
        RangedMovement();
    }

    public override void Tick()
    {
        bool wasAttacking = IsAttacking;
        bool finished = RangedTick();

        if (!wasAttacking && IsAttacking)
        {
            arrowReleasedThisAttack = false;
            arrowCreatedThisAttack = false;
        }

        // RangedTick can finish an attack before LateUpdate sees the release
        if (finished && !arrowReleasedThisAttack)
        {
            if (heldArrow == null)
                CreateHeldArrow();

            if (heldArrow != null)
            {
                OrientHeldArrow();
                EndBowstringPull();
                ReleaseArrow();
            }
        }

        // Only an interrupted attack discards its held projectile.
        if (!IsAttacking && heldArrow != null)
        {
            Destroy(heldArrow.gameObject);
            heldArrow = null;
        }
    }

    private void CreateHeldArrow()
    {
        if (heldArrow != null)
        {
            Destroy(heldArrow.gameObject);
            heldArrow = null;
        }

        if (arrowPrefab == null || bowstringPullHand == null)
        {
            Debug.LogWarning($"[{name}] Cannot create held arrow: assign Arrow Prefab and Bowstring Pull Hand.");
            return;
        }

        heldArrow = Instantiate(arrowPrefab, bowstringPullHand, false);
        heldArrow.Hold(transform);
        heldArrow.SetVisualRotationOffset(arrowModelRotationOffset);
        heldArrow.transform.localPosition = heldArrowLocalPosition;
        heldArrow.transform.localRotation = Quaternion.Euler(heldArrowLocalRotation);
        arrowCreatedThisAttack = true;
        OrientHeldArrow();
    }

    private void ReleaseArrow()
    {
        if (heldArrow == null || arrowReleasedThisAttack)
            return;

        arrowReleasedThisAttack = true;

        if (Player == null || ai == null || ai.Data == null)
        {
            Destroy(heldArrow.gameObject);
            heldArrow = null;
            return;
        }

        EnemyProjectile arrow = heldArrow;
        heldArrow = null;

        arrow.transform.SetParent(null, true);

        Vector3 aim = AimPoint + Vector3.up * aimHeightOffset;
        arrow.Launch(Player, aim, EnemyDamage, ai.Data.element, transform);
    }

    public override void OnInitialized()
    {
        base.OnInitialized();

        if (heldArrow != null)
        {
            Destroy(heldArrow.gameObject);
            heldArrow = null;
        }

        arrowReleasedThisAttack = false;
        arrowCreatedThisAttack = false;
        stringBent = false;
        drawWeight = 0f;
        bowstringRenderer = null;

        if (bowstringMesh != null)
        {
            Destroy(bowstringMesh);
            bowstringMesh = null;
        }

        Animator animator = EnemyAnimator;

        if (bowstringPullHand == null && animator != null && animator.isHuman)
        {
            HumanBodyBones drawBone =
                weaponBone == HumanBodyBones.LeftHand
                    ? HumanBodyBones.RightHand
                    : HumanBodyBones.LeftHand;

            bowstringPullHand = animator.GetBoneTransform(drawBone);
        }

        if (weaponHolder == null)
            return;

        foreach (Transform child in weaponHolder.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != bowstringObjectName)
                continue;

            bowstringRenderer = child.GetComponent<SkinnedMeshRenderer>();
            break;
        }

        if (bowstringRenderer == null)
        {
            Debug.LogError($"[{name}] BowString SkinnedMeshRenderer not found.");
            return;
        }

        if (bowstringRenderer.sharedMesh == null)
        {
            Debug.LogError($"[{name}] BowString has no mesh assigned.");
            return;
        }

        if (bowstringPullHand == null)
        {
            Debug.LogError($"[{name}] Drawing hand not found.");
            return;
        }

        bowstringMesh = Instantiate(bowstringRenderer.sharedMesh);
        bowstringMesh.name = "BowString_Runtime";
        bowstringMesh.MarkDynamic();
        bowstringRenderer.sharedMesh = bowstringMesh;

        originalVertices = bowstringMesh.vertices;
        workingVertices = new Vector3[originalVertices.Length];
        weights = new float[originalVertices.Length];

        Vector3 size = bowstringMesh.bounds.size;

        if (stringLengthAxis == BowstringAxis.X)
            localAxis = Vector3.right;
        else if (stringLengthAxis == BowstringAxis.Y)
            localAxis = Vector3.up;
        else if (stringLengthAxis == BowstringAxis.Z)
            localAxis = Vector3.forward;
        else if (size.x >= size.y && size.x >= size.z)
            localAxis = Vector3.right;
        else if (size.y >= size.z)
            localAxis = Vector3.up;
        else
            localAxis = Vector3.forward;

        warnedZeroPull = false;

        float min = float.MaxValue;
        float max = float.MinValue;

        foreach (Vector3 vertex in originalVertices)
        {
            float value = Vector3.Dot(vertex, localAxis);
            min = Mathf.Min(min, value);
            max = Mathf.Max(max, value);
        }

        for (int i = 0; i < originalVertices.Length; i++)
        {
            float t = Mathf.InverseLerp(min, max, Vector3.Dot(originalVertices[i], localAxis));
            weights[i] = Mathf.Sin(t * Mathf.PI);
        }

        Vector3 sum = Vector3.zero;
        int count = 0;

        for (int i = 0; i < originalVertices.Length; i++)
        {
            if (weights[i] < 0.95f)
                continue;

            sum += originalVertices[i];
            count++;
        }

        localMidpoint = count > 0
            ? sum / count
            : localAxis * ((min + max) * 0.5f);

        if (max - min < 0.0001f)
            Debug.LogWarning($"[{name}] BowString has almost no length on the selected axis ({stringLengthAxis}); choose the correct String Length Axis in the Inspector.", this);
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();

        float normalizedTime = 0f;
        bool animatingDraw = IsAttacking && TryGetDrawTime(out normalizedTime);

        if (animatingDraw &&
            !arrowCreatedThisAttack &&
            !arrowReleasedThisAttack &&
            normalizedTime >= Mathf.Min(arrowAppearTime, arrowReleaseTime - 0.01f) &&
            normalizedTime < arrowReleaseTime)
        {
            CreateHeldArrow();
        }

        UpdateBowstringPull();
        OrientHeldArrow();

        if (animatingDraw &&
            !arrowReleasedThisAttack &&
            normalizedTime >= arrowReleaseTime)
        {
            if (heldArrow == null)
                CreateHeldArrow();

            if (heldArrow != null)
            {
                OrientHeldArrow();
                ReleaseArrow();
            }
        }
    }

    private void OrientHeldArrow()
    {
        if (heldArrow == null || bowstringPullHand == null)
            return;

        // This arrow offset is from the hand, not Projectile Origin (Unused for archer class).
        heldArrow.transform.position =
            bowstringPullHand.TransformPoint(heldArrowLocalPosition);

        Vector3 aimDirection = Vector3.zero;

        if (alignHeldArrowToBowstring && bowstringRenderer != null)
        {
            Vector3 bowNockPoint =
                bowstringRenderer.transform.TransformPoint(localMidpoint);

            aimDirection = bowNockPoint - heldArrow.transform.position;
        }

        if (aimDirection.sqrMagnitude < 0.0001f && Player != null)
            aimDirection = AimPoint - heldArrow.transform.position;

        if (aimDirection.sqrMagnitude > 0.0001f)
        {
            heldArrow.FaceDirection(aimDirection);
            heldArrow.transform.rotation *=
                Quaternion.Euler(heldArrowLocalRotation);
        }
    }

    private void UpdateBowstringPull()
    {
        if (bowstringMesh == null ||
            bowstringPullHand == null ||
            bowstringRenderer == null)
            return;

        bool inState = TryGetDrawTime(out float time);
        Transform stringTransform = bowstringRenderer.transform;

        Vector3 worldOffset =
            bowstringPullHand.TransformPoint(pullHandOffset) -
            stringTransform.TransformPoint(localMidpoint);

        bool wantDraw =
            IsAttacking &&
            inState &&
            time >= Mathf.Min(bowstringDrawStartTime, arrowReleaseTime - 0.01f) &&
            time < arrowReleaseTime;

        if (inState && time >= arrowReleaseTime)
        {
            EndBowstringPull();
            return;
        }

        float target = wantDraw ? 1f : 0f;
        float duration = wantDraw ? BlendInTime : BlendOutTime;

        drawWeight = Mathf.MoveTowards(
            drawWeight,
            target,
            duration <= 0.0001f ? 1f : Time.deltaTime / duration
        );

        if (drawWeight <= 0.0001f)
        {
            if (stringBent)
                EndBowstringPull();

            return;
        }

        float eased = drawWeight * drawWeight * (3f - 2f * drawWeight);

        Vector3 localOffset = stringTransform.InverseTransformVector(worldOffset);

        localOffset = Vector3.ProjectOnPlane(localOffset, localAxis);

        Vector3 worldBend = stringTransform.TransformVector(localOffset);
        worldBend = Vector3.ClampMagnitude(worldBend, maxPullDistance);
        localOffset = stringTransform.InverseTransformVector(worldBend) * eased;

        if (debugBowstring && !warnedZeroPull && wantDraw &&
            localOffset.sqrMagnitude < 0.000001f)
        {
            warnedZeroPull = true;
            Debug.LogWarning(
                $"[{name}] Bowstring pull is zero. Check Bowstring Pull Hand, Pull Hand Offset, and String Length Axis (currently {stringLengthAxis}). " +
                "The hand must move to the bowstring length for mesh bending.",
                this
            );
        }

        for (int i = 0; i < originalVertices.Length; i++)
            workingVertices[i] = originalVertices[i] + localOffset * weights[i];

        bowstringMesh.vertices = workingVertices;
        bowstringMesh.RecalculateBounds();
        bowstringMesh.RecalculateNormals();

        stringBent = true;
    }

    private void EndBowstringPull()
    {
        stringBent = false;
        drawWeight = 0f;

        if (bowstringMesh == null || originalVertices == null)
            return;

        bowstringMesh.vertices = originalVertices;
        bowstringMesh.RecalculateBounds();
        bowstringMesh.RecalculateNormals();
    }

    public override void OnDeath()
    {
        base.OnDeath();

        if (heldArrow != null)
        {
            Destroy(heldArrow.gameObject);
            heldArrow = null;
        }

        arrowReleasedThisAttack = false;
        arrowCreatedThisAttack = false;
        EndBowstringPull();
    }

    private void OnDrawGizmosSelected()
    {
        if (bowstringPullHand == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            bowstringPullHand.TransformPoint(pullHandOffset),
            0.03f
        );
    }

    private void OnDisable()
    {
        if (heldArrow != null)
        {
            Destroy(heldArrow.gameObject);
            heldArrow = null;
        }

        arrowReleasedThisAttack = false;
        arrowCreatedThisAttack = false;
        EndBowstringPull();
    }

    private void OnDestroy()
    {
        if (bowstringMesh != null)
            Destroy(bowstringMesh);
    }
}