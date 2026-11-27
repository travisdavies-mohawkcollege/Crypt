using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LightningArc : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 endPosition;
    private Transform startTransform;

    [Header("Shape")]
    [SerializeField, Min(2)] private int segments = 16;
    [SerializeField] private float displacement = 0.25f;

    [Header("Animation")]
    [SerializeField] private float refreshRate = 30f;
    [SerializeField] private float lifetime = 0.25f;
    [SerializeField]
    private AnimationCurve widthOverLifetime =
        AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private LineRenderer line;
    private float age;
    private float refreshTimer;
    private float initialWidth;

    public void Initialize(Transform start, Vector3 end)
    {
        startTransform = start;
        startPosition = start.position;
        endPosition = end;
        age = 0f;
        enabled = true;
        gameObject.SetActive(true);
        GenerateBolt();
    }

    public void Initialize(Vector3 start, Vector3 end)
    {
        startTransform = null;
        startPosition = start;
        endPosition = end;
        age = 0f;
        enabled = true;
        gameObject.SetActive(true);
        GenerateBolt();
    }

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        initialWidth = line.widthMultiplier;
        line.positionCount = segments + 1;
    }

    private void Update()
    {
        age += Time.deltaTime;

        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        refreshTimer -= Time.deltaTime;

        if (refreshTimer <= 0f)
        {
            GenerateBolt();
            refreshTimer = 1f / refreshRate;
        }

        float normalizedAge = Mathf.Clamp01(age / lifetime);
        line.widthMultiplier = initialWidth * widthOverLifetime.Evaluate(normalizedAge);
    }

    private void GenerateBolt()
    {
        line.positionCount = segments + 1;

        Vector3 start = startTransform != null ? startTransform.position : startPosition;
        Vector3 end = endPosition;
        Vector3 direction = end - start;
        float distance = direction.magnitude;

        // Keep the visual noise proportional to arc length.
        float noiseAmount = displacement * Mathf.Min(distance, 3f);

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 position = Vector3.Lerp(start, end, t);

            // Keeps both endpoints attached to the targets.
            float endpointMask = Mathf.Sin(t * Mathf.PI);
            Vector3 offset = Random.insideUnitSphere
                * noiseAmount
                * endpointMask;

            line.SetPosition(i, position + offset);
        }
    }
}