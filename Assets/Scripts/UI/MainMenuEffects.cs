using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Ana menü için görsel efektler - Logo animasyonu, paralaks, fade-in
/// </summary>
public class MainMenuEffects : MonoBehaviour
{
    [Header("Logo Animation")]
    [SerializeField] private RectTransform logoTransform;
    [SerializeField] private float floatAmount = 10f;
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float pulseAmount = 0.05f;
    [SerializeField] private float pulseSpeed = 2f;

    [Header("Parallax Background")]
    [SerializeField] private RectTransform[] parallaxLayers;
    [SerializeField] private float[] parallaxSpeeds;
    [SerializeField] private float parallaxAmount = 30f;
    [SerializeField] private bool useMouseParallax = true;

    [Header("Screen Fade")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Ambient Particles")]
    [SerializeField] private ParticleSystem ambientParticles;

    [Header("Camera Shake (Subtle)")]
    [SerializeField] private float ambientShakeAmount = 0.5f;
    [SerializeField] private float ambientShakeSpeed = 0.5f;

    private Vector3 logoOriginalPosition;
    private Vector3 logoOriginalScale;
    private Vector3[] layerOriginalPositions;
    private Camera mainCamera;
    private Vector3 cameraOriginalPosition;

    private void Start()
    {
        mainCamera = Camera.main;
        
        if (mainCamera != null)
        {
            cameraOriginalPosition = mainCamera.transform.position;
        }

        // Logo setup
        if (logoTransform != null)
        {
            logoOriginalPosition = logoTransform.anchoredPosition;
            logoOriginalScale = logoTransform.localScale;
        }

        // Parallax layers setup
        SetupParallaxLayers();

        // Fade in başlat
        if (fadeOverlay != null)
        {
            StartCoroutine(FadeIn());
        }

        // Parçacıkları başlat
        if (ambientParticles != null)
        {
            ambientParticles.Play();
        }
    }

    private void SetupParallaxLayers()
    {
        if (parallaxLayers != null && parallaxLayers.Length > 0)
        {
            layerOriginalPositions = new Vector3[parallaxLayers.Length];
            for (int i = 0; i < parallaxLayers.Length; i++)
            {
                if (parallaxLayers[i] != null)
                {
                    layerOriginalPositions[i] = parallaxLayers[i].anchoredPosition;
                }
            }

            // Varsayılan hızlar
            if (parallaxSpeeds == null || parallaxSpeeds.Length != parallaxLayers.Length)
            {
                parallaxSpeeds = new float[parallaxLayers.Length];
                for (int i = 0; i < parallaxLayers.Length; i++)
                {
                    parallaxSpeeds[i] = (i + 1) * 0.5f;
                }
            }
        }
    }

    private void Update()
    {
        AnimateLogo();
        UpdateParallax();
        UpdateAmbientCameraShake();
    }

    private void AnimateLogo()
    {
        if (logoTransform == null) return;

        // Floating animation
        float floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmount;
        Vector3 newPosition = logoOriginalPosition + new Vector3(0, floatOffset, 0);
        logoTransform.anchoredPosition = newPosition;

        // Pulse animation (scale)
        float scaleOffset = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        logoTransform.localScale = logoOriginalScale * scaleOffset;
    }

    private void UpdateParallax()
    {
        if (!useMouseParallax || parallaxLayers == null) return;

        // Mouse pozisyonunu normalize et (-1 ile 1 arası)
        Vector2 mousePos = Input.mousePosition;
        float normalizedX = (mousePos.x / Screen.width - 0.5f) * 2f;
        float normalizedY = (mousePos.y / Screen.height - 0.5f) * 2f;

        for (int i = 0; i < parallaxLayers.Length; i++)
        {
            if (parallaxLayers[i] == null) continue;

            float speed = parallaxSpeeds[i];
            Vector3 offset = new Vector3(
                -normalizedX * parallaxAmount * speed,
                -normalizedY * parallaxAmount * speed * 0.5f,
                0
            );

            Vector3 targetPos = layerOriginalPositions[i] + offset;
            parallaxLayers[i].anchoredPosition = Vector3.Lerp(
                parallaxLayers[i].anchoredPosition,
                targetPos,
                Time.deltaTime * 3f
            );
        }
    }

    private void UpdateAmbientCameraShake()
    {
        if (mainCamera == null || ambientShakeAmount <= 0) return;

        float offsetX = Mathf.PerlinNoise(Time.time * ambientShakeSpeed, 0) * 2 - 1;
        float offsetY = Mathf.PerlinNoise(0, Time.time * ambientShakeSpeed) * 2 - 1;

        Vector3 shakeOffset = new Vector3(offsetX, offsetY, 0) * ambientShakeAmount;
        mainCamera.transform.position = cameraOriginalPosition + shakeOffset;
    }

    private IEnumerator FadeIn()
    {
        fadeOverlay.gameObject.SetActive(true);
        fadeOverlay.color = Color.black;

        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeCurve.Evaluate(elapsed / fadeInDuration);
            fadeOverlay.color = new Color(0, 0, 0, 1 - t);
            yield return null;
        }

        fadeOverlay.color = new Color(0, 0, 0, 0);
        fadeOverlay.gameObject.SetActive(false);
    }

    /// <summary>
    /// Sahne geçişi için fade out
    /// </summary>
    public IEnumerator FadeOut(float duration = 1f)
    {
        if (fadeOverlay == null) yield break;

        fadeOverlay.gameObject.SetActive(true);
        fadeOverlay.color = new Color(0, 0, 0, 0);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = fadeCurve.Evaluate(elapsed / duration);
            fadeOverlay.color = new Color(0, 0, 0, t);
            yield return null;
        }

        fadeOverlay.color = Color.black;
    }
}
