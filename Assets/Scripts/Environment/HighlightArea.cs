using UnityEngine;
using System.Collections;

public class HighlightArea : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";

    [Header("Highlight Object")]
    [SerializeField] private GameObject highlightObject;

    [Header("Animation")]
    [SerializeField] private float animDuration = 0.25f;
    [SerializeField] private AnimationCurve animCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isHidden = false;
    private bool isAnimating = false;

    private SpriteRenderer highlightSprite;
    private Vector3 startScale;

    private void Awake()
    {
        if (highlightObject == null)
        {
            Debug.LogError("Highlight Object atanmamış!", this);
            enabled = false;
            return;
        }

        highlightSprite = highlightObject.GetComponent<SpriteRenderer>();
        startScale = highlightObject.transform.localScale;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isHidden || isAnimating) return;

        if (other.CompareTag(playerTag))
        {
            StartCoroutine(HideHighlight());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!isHidden || isAnimating) return;

        if (other.CompareTag(playerTag))
        {
            StartCoroutine(ShowHighlight());
        }
    }

    // --------------------------------------------------
    // HIDE
    // --------------------------------------------------
    private IEnumerator HideHighlight()
    {
        isAnimating = true;

        float elapsed = 0f;
        Vector3 fromScale = startScale;
        Vector3 toScale = Vector3.zero;

        Color startColor = highlightSprite.color;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = animCurve.Evaluate(elapsed / animDuration);

            highlightObject.transform.localScale = Vector3.Lerp(fromScale, toScale, t);
            highlightSprite.color = new Color(
                startColor.r,
                startColor.g,
                startColor.b,
                Mathf.Lerp(1f, 0f, t)
            );

            yield return null;
        }

        highlightObject.transform.localScale = Vector3.zero;
        highlightSprite.color = new Color(startColor.r, startColor.g, startColor.b, 0f);

        isHidden = true;
        isAnimating = false;
    }

    // --------------------------------------------------
    // SHOW
    // --------------------------------------------------
    private IEnumerator ShowHighlight()
    {
        isAnimating = true;

        float elapsed = 0f;
        Vector3 fromScale = Vector3.zero;
        Vector3 toScale = startScale;

        Color startColor = highlightSprite.color;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = animCurve.Evaluate(elapsed / animDuration);

            highlightObject.transform.localScale = Vector3.Lerp(fromScale, toScale, t);
            highlightSprite.color = new Color(
                startColor.r,
                startColor.g,
                startColor.b,
                Mathf.Lerp(0f, 1f, t)
            );

            yield return null;
        }

        highlightObject.transform.localScale = startScale;
        highlightSprite.color = new Color(startColor.r, startColor.g, startColor.b, 1f);

        isHidden = false;
        isAnimating = false;
    }
}
