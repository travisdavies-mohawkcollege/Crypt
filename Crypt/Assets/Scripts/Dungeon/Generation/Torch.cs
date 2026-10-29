using UnityEngine;

public class Torch : MonoBehaviour
{
    [SerializeField] private Light light;
    [SerializeField] private bool lightOn;

    private void Start()
    {
        light.enabled = lightOn;
    }

    public void TurnOnTorch()
    {
        light.enabled = true;
    }

}
