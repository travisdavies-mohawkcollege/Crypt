using UnityEngine;

// Goes on every projectile prefab with a trigger collider.
[RequireComponent(typeof(Rigidbody))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Straight shots: travel speed. Arcing shots: average speed toward the target (sets flight time = distance / speed).")]
    public float speed = 10f;
    public float lifetime = 6f;
    public float armTime = 0f;
    public float turnRate = 0f;

    [Header("Arc")]
    [Tooltip("0 = straight line. Otherwise the arc's peak height above the straight line as a fraction of the shot distance (0.15 = gentle, 0.3 = high lob). Always lands on the aim point at any range. Non-homing projectiles only.")]
    [Min(0f)] public float arcHeightRatio = 0.15f;

    [Header("Visual Orientation")]
    [Tooltip("Corrects the imported model's local forward axis. Set Y to 180 if the arrow mesh points backwards.")]
    public Vector3 visualRotationOffset;

    [Header("Impact")]
    public GameObject impactVfx;

    private Transform target;
    private Transform owner;
    private Vector3 dir = Vector3.forward;
    private Vector3 targetOffset = Vector3.up;
    private float damage;
    private Element element;
    private float born;
    private Quaternion visualRotationCorrection;
    private bool isHeld;
    private bool launched;
    private Collider[] projectileColliders;
    private bool ballistic;
    private Vector3 launchPos;
    private Vector3 launchVelocity;
    private float arcGravity;
    private float flightTime;

    private void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        projectileColliders = GetComponentsInChildren<Collider>(true);
        visualRotationCorrection = Quaternion.Euler(visualRotationOffset);
    }

    // Stops a newly instantiated arrow from moving while it is parented to the archer's hand.
    public void Hold(Transform projectileOwner)
    {
        isHeld = true;
        launched = false;
        ballistic = false;
        target = null;
        owner = projectileOwner;
        SetProjectileCollidersEnabled(false);
    }

    public void Launch(Transform target, Vector3 aimPoint, float damage, Element element, Transform owner)
    {
        if (launched)
            return;

        launched = true;
        isHeld = false;
        SetProjectileCollidersEnabled(true);
        this.target = target;
        this.damage = damage;
        this.element = element;
        this.owner = owner;
        born = Time.time;
        targetOffset = target != null ? aimPoint - target.position : Vector3.zero;

        Vector3 to = aimPoint - transform.position;
        if (to.sqrMagnitude > 0.001f)
            dir = to.normalized;

        // Arc only for arrow projectiles
        ballistic = arcHeightRatio > 0f && turnRate <= 0f && armTime <= 0f && to.sqrMagnitude > 0.001f;
        if (ballistic)
            SetupBallistic(to);

        UpdateFacing();
        Destroy(gameObject, lifetime + armTime);
    }

    // Gravity is chosen per shot so the arc is proportional at any range
    private void SetupBallistic(Vector3 to)
    {
        float dist = to.magnitude;
        flightTime = Mathf.Max(dist / Mathf.Max(speed, 0.01f), 0.05f);

        arcGravity = 8f * arcHeightRatio * dist / (flightTime * flightTime);

        launchPos = transform.position;
        launchVelocity = to / flightTime + Vector3.up * (0.5f * arcGravity * flightTime);
        dir = launchVelocity.normalized;
    }

    private void SetProjectileCollidersEnabled(bool enabled)
    {
        if (projectileColliders == null)
            return;

        for (int i = 0; i < projectileColliders.Length; i++)
        {
            if (projectileColliders[i] != null)
                projectileColliders[i].enabled = enabled;
        }
    }

    private void Update()
    {
        if (!launched || isHeld)
            return;

        if (Time.time - born < armTime)
        {
            if (target != null)
            {
                Vector3 to = (target.position + targetOffset) - transform.position;
                if (to.sqrMagnitude > 0.001f)
                    dir = to.normalized;

                UpdateFacing();
            }

            return;
        }

        if (ballistic)
        {
            float t = Time.time - born;
            transform.position = launchPos + launchVelocity * t + Vector3.down * (0.5f * arcGravity * t * t);
            dir = (launchVelocity + Vector3.down * (arcGravity * t)).normalized;
            UpdateFacing();
            return;
        }

        if (turnRate > 0f && target != null)
        {
            Vector3 desired = (target.position + targetOffset) - transform.position;

            if (desired.sqrMagnitude > 0.001f)
            {
                dir = Vector3.RotateTowards(
                    dir,
                    desired.normalized,
                    turnRate * Mathf.Deg2Rad * Time.deltaTime,
                    0f
                ).normalized;

                UpdateFacing();
            }
        }

        transform.position += dir * speed * Time.deltaTime;
    }

    public void SetVisualRotationOffset(Vector3 eulerDegrees)
    {
        visualRotationOffset = eulerDegrees;
        visualRotationCorrection = Quaternion.Euler(eulerDegrees);
    }

    public void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        transform.rotation =
            Quaternion.LookRotation(direction.normalized) * visualRotationCorrection;
    }

    private void UpdateFacing()
    {
        if (dir.sqrMagnitude <= 0.0001f)
            return;

        FaceDirection(dir);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!launched || isHeld || other.isTrigger)
            return;

        if (owner != null && other.transform.root == owner.root)
            return;

        if (other.GetComponentInParent<EnemyAI>() != null)
            return;

        DamageUtil.Hit(other, damage, element);

        if (impactVfx != null)
            Instantiate(impactVfx, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}