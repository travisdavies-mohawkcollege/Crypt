using UnityEngine;

// Keeps a world-space damage popup facing the active gameplay camera.
public class BillBoard : MonoBehaviour
{
    private Camera cam;

    private void OnEnable()
    {
        FindCamera();
    }

    private void LateUpdate()
    {
        // Reacquire Camera.main after scene changes or camera replacement.
        if (cam == null || !cam.isActiveAndEnabled) FindCamera();
        if (cam == null) return;

        transform.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
    }

    private void FindCamera()
    {
        cam = Camera.main;
    }
}
