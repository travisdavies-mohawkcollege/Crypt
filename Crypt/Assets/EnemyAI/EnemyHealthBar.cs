using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private EnemyAI enemy;
    [SerializeField] private Image healthFill;
    [SerializeField] private TextMeshProUGUI enemyName;

    private Camera gameplayCamera;

    private void Awake()
    {
        if (enemy == null)
        {
            enemy = GetComponentInParent<EnemyAI>();
            enemyName.text = $"{enemy.Data.element} {enemy.Data.EnemyName}";
        }
    }

    private void LateUpdate()
    {
        if (enemy == null || healthFill == null)
            return;

        float maxHealth = enemy.Stats.maxHealth;

        healthFill.fillAmount = maxHealth > 0f
            ? Mathf.Clamp01(enemy.currentHealth / maxHealth)
            : 0f;

        FaceCamera();
    }

    private void FaceCamera()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        if (gameplayCamera == null)
            return;

        transform.rotation = Quaternion.LookRotation(transform.position - gameplayCamera.transform.position);
    }
}