using TMPro;
using UnityEngine;

public class PlayerStatsPanel : MonoBehaviour
{
    [SerializeField] private PlayerCombatStats playerCombatStats;
    [SerializeField] private TMP_Text statsText;

    private void Awake()
    {
        if (playerCombatStats == null) playerCombatStats = GetComponentInParent<PlayerCombatStats>();
    }

    private void OnEnable()
    {
        if (playerCombatStats == null) playerCombatStats = FindFirstObjectByType<PlayerCombatStats>();

        if (playerCombatStats != null)
        {
            playerCombatStats.Changed -= Refresh;
            playerCombatStats.Changed += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (playerCombatStats != null) playerCombatStats.Changed -= Refresh;
    }

    public void Refresh()
    {
        if (statsText == null) return;
        statsText.text = playerCombatStats != null ? playerCombatStats.CurrentStatsText : "Player stats unavailable.";
    }
}