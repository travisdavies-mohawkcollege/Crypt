using UnityEngine;

public class MagicalWaterColor : MonoBehaviour
{
    public Color[] colors =
    {
        Color.cyan,
        Color.blue,
        Color.magenta,
        Color.cyan
    };

    public float cycleSpeed = 0.2f;

    private Material waterMaterial;

    void Start()
    {
        waterMaterial = GetComponent<Renderer>().material;
    }

    void Update()
    {
        if (colors.Length < 2) return;

        float time = Time.time * cycleSpeed;
        int index = Mathf.FloorToInt(time) % colors.Length;
        int next = (index + 1) % colors.Length;

        float blend = Mathf.SmoothStep(0, 1, time % 1f);

        Color currentColor = Color.Lerp(colors[index], colors[next], blend);

        waterMaterial.SetColor("_Color1", currentColor);
        waterMaterial.SetColor("_Color2", currentColor * 0.5f);
    }
}
