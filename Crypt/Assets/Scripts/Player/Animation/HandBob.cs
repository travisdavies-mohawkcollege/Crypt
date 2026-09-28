using UnityEngine;

public class HandBob : MonoBehaviour
{
    [SerializeField] private float bobAmount = 10f;
    [SerializeField] private float bobSpeed = 6f;
    [SerializeField] private float sprintBobSpeed = 10f;

    private PlayerController player;
    private RectTransform rectTransform;
    private Vector2 restPosition;
    private float timer;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        restPosition = rectTransform.anchoredPosition;
        player = FindAnyObjectByType<PlayerController>();
    }

    private void Update()
    {
        //Debug.Log($"Moving: {player?.moving}");
        if (player != null && player.moving)
        {
            float speed = player.sprinting? sprintBobSpeed : bobSpeed;

            timer += Time.deltaTime * speed;

            float xOffset = Mathf.Cos(timer) * bobAmount;
            float yOffset = Mathf.Abs(Mathf.Sin(timer)) * bobAmount;

            rectTransform.anchoredPosition = restPosition + new Vector2(xOffset, yOffset);
        }
        else
        {
            timer = 0f;
            rectTransform.anchoredPosition = restPosition;
        }
    }
}