using UnityEngine;
using TMPro;

public class FloatingDamageText : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TextMeshProUGUI text;

    [Header("Motion")]
    [SerializeField] private float moveSpeed = 0.8f;
    [SerializeField] private float lifeTime = 0.8f;

    [Header("Fade")]
    [SerializeField] private float fadeStartPercent = 0.4f;

    private float elapsed;
    private Color startColor;

    public void Initialize(float damage)
    {
        text.text = damage.ToString();
        startColor = text.color;
    }

    void Update()
    {
        transform.localPosition += Vector3.up * moveSpeed * Time.deltaTime;

        elapsed += Time.deltaTime;
        float t = elapsed / lifeTime;

        if (t >= fadeStartPercent)
        {
            float fadeT = Mathf.InverseLerp(fadeStartPercent, 1f, t);
            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, fadeT);
            text.color = c;
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
