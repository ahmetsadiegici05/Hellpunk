using UnityEngine;

public class SlashEffect : MonoBehaviour
{
    [SerializeField] private float lifeTime = 0.15f;
    [SerializeField] private float startScaleX = 0.2f;
    [SerializeField] private float endScaleX = 1.2f;

    private SpriteRenderer sr;
    private float timer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        timer = lifeTime;
        transform.localScale = new Vector3(startScaleX, 1f, 1f);
    }

    void Update()
    {
        timer -= Time.deltaTime;
        float t = 1f - (timer / lifeTime);

        // Uzama efekti
        transform.localScale = new Vector3(
            Mathf.Lerp(startScaleX, endScaleX, t),
            transform.localScale.y,
            1f
        );

        // Fade out
        Color c = sr.color;
        c.a = Mathf.Lerp(1f, 0f, t);
        sr.color = c;

        if (timer <= 0f)
            Destroy(gameObject);
    }
}
