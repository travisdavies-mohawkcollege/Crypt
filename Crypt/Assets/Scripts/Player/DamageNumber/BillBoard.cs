using UnityEngine;

public class BillBoard : MonoBehaviour
{
    private Camera cam;

    private void OnEnable()
    {
        FindCamera();
    }

    private void LateUpdate()
    {
        if (cam == null || !cam.isActiveAndEnabled) FindCamera();
        if (cam == null) return;

        transform.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
    }

    private void FindCamera()
    {
        cam = Camera.main;
    }
}