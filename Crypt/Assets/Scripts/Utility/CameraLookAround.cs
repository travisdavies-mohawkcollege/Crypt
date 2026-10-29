using UnityEngine;

// Gives menu or showcase cameras a repeating horizontal sweep around their initial rotation.
public class CameraLookAround : MonoBehaviour
{
    public float angle = 45f;
    public float speed = 1f;

    private Quaternion startingRotation;

    void Start()
    {
        // Preserve the authored rotation so the sine wave acts as a local offset.
        startingRotation = transform.rotation;
    }

    void Update()
    {
        // Sin keeps the motion smooth at both ends instead of snapping direction.
        float rotation = Mathf.Sin(Time.time * speed) * angle;
        transform.rotation = startingRotation * Quaternion.Euler(0f, rotation, 0f);
    }
}
