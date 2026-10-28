using UnityEngine;

public class MagicalWaterColor : MonoBehaviour
{
    [Header("Shader Properties")]
    public string primaryColorProperty = "_Color1";
    public string secondaryColorProperty = "_Color2";

    [Header("Color Settings")]
    public Color[] colors =
    {
        Color.cyan,
        Color.blue,
        Color.magenta,
        Color.cyan
    };

    public float cycleSpeed = 0.2f;
    public float secondaryColorBrightness = 0.8f;

    private Material material;

    void Start()
    {
        material = GetComponent<Renderer>().material;
    }

    void Update()
    {
        if (material == null || colors.Length < 2)
            return;

        float time = Time.time * cycleSpeed;

        int index = Mathf.FloorToInt(time) % colors.Length;
        int next = (index + 1) % colors.Length;

        float blend = Mathf.SmoothStep(0, 1, time % 1f);

        Color currentColor = Color.Lerp(colors[index], colors[next], blend);

        if (material.HasProperty(primaryColorProperty))
            material.SetColor(primaryColorProperty, currentColor);

        if (material.HasProperty(secondaryColorProperty))
            material.SetColor(
                secondaryColorProperty,
                currentColor * secondaryColorBrightness
            );
    }
}