using UnityEngine;

public class Torch : MonoBehaviour
{
    [SerializeField] private Light light;

    private void Start()
    {
        light.enabled = false;
    }

    public void TurnOnTorch()
    {
        light.enabled = true;
    }

}
