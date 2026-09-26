using System.Data;
using System.Security.Cryptography;
using System.Collections;
using TMPro;
using UnityEngine;

public class DamageNumberAnimation : MonoBehaviour
{
   public AnimationCurve animHeight;
   public AnimationCurve animScale;
   public AnimationCurve animAlphaColour;
   private float time = 0;
   private Vector3 origin;

   private TextMeshProUGUI text;
   
   private float lifetime = 1f;
   private Coroutine disableRoutine;
   
   private void OnEnable()
    {
        disableRoutine = StartCoroutine(DisableAfterDelay());
    }
    
    private void OnDisable()
    {
        if(disableRoutine != null) StopCoroutine(DisableAfterDelay());
    }

   void Start()
    {
        origin = transform.position;
        text = GetComponentInChildren<TextMeshProUGUI>();
    }

    void Update()
    {
        transform.position = origin + new Vector3(0, 1 * animHeight.Evaluate(time), 0);
        transform.localScale = new Vector3(1 * animScale.Evaluate(time), 1 *animScale.Evaluate(time), 1);
        time += Time.deltaTime;

        Color currentColour = text.color;
        currentColour.a = animAlphaColour.Evaluate(time);
        text.color = currentColour;
    }

    private IEnumerator DisableAfterDelay()
    {
        yield return new WaitForSeconds(lifetime);
        gameObject.SetActive(false);
    }
}
