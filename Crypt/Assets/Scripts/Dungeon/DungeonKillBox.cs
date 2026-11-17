using UnityEngine;

public class DungeonKillBox : MonoBehaviour
{
    private bool playerKilled;

    private void OnTriggerEnter(Collider other)
    {
        if (playerKilled) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        if (PlayerManager.Instance == null)
        {
            Debug.LogError("DungeonKillBox could not find PlayerManager.", this);
            return;
        }

        playerKilled = true;
        PlayerManager.Instance.KillPlayer();
    }
}