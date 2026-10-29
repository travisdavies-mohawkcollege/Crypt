using UnityEngine;

public class CameraLookAround : MonoBehaviour
{
    public float angle = 45f;
    public float speed = 1f;

    private Quaternion startingRotation;

    void Start()
    {
        startingRotation = transform.rotation;
    }

    void Update()
    {
        float rotation = Mathf.Sin(Time.time * speed) * angle;
        transform.rotation = startingRotation * Quaternion.Euler(0f, rotation, 0f);
    }
}