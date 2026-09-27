using UnityEngine;

public class BillBoard : MonoBehaviour
{
    private Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        transform.forward = cam.transform.forward;
    }
}
