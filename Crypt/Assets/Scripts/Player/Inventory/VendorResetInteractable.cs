using UnityEngine;

public class VendorResetInteractable : MonoBehaviour, IInteractable
{
    public string InteractText => "Reset vendor progress";

    public void Interact(PlayerController player)
    {
        if (VendorProgress.Instance == null)
        {
            Debug.LogError("VendorResetInteractable could not find VendorProgress.", this);
            return;
        }

        VendorProgress.Instance.ResetProgress();
        Debug.Log("Vendor progress reset to level 0.");
    }
}