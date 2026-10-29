using UnityEngine;

// Small room-light controller used by RoomTriggers.
public class Torch : MonoBehaviour
{
    [SerializeField] private Light light;
    [SerializeField] private bool lightOn;

    private void Start()
    {
        // Apply the authored initial state before a room trigger can turn the light on.
        light.enabled = lightOn;
    }

    public void TurnOnTorch()
    {
        light.enabled = true;
    }

}
