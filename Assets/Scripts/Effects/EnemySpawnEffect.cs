using UnityEngine;
using System.Collections;

/// <summary>
/// Düşman spawn efekti - Düşmanlar belirirken duman/ışık efekti
/// </summary>
public class EnemySpawnEffect : MonoBehaviour
{
    public static EnemySpawnEffect Instance { get; private set; }
    
    [Header("Efekt Ayarları")]
    [SerializeField] private float spawnDuration = 0.5f;
    [SerializeField] private Color spawnColor = new Color(0.6f, 0f, 0.8f, 1f); // Mor
    [SerializeField] private Color smokeColor = new Color(0.2f, 0.1f, 0.3f, 0.8f);
    
    private ParticleSystem spawnParticles;
    private ParticleSystem smokeParticles;
    private ParticleSystem glowParticles;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        CreateSpawnParticles();
        CreateSmokeParticles();
        CreateGlowParticles();
    }
    
    /// <summary>
    /// Düşman spawn efektini oynat ve fade-in uygula
    /// </summary>
    public void PlaySpawnEffect(Vector3 position, Transform enemyTransform = null)
    {
        // Parçacık efektleri
        if (spawnParticles != null)
        {
            spawnParticles.transform.position = position;
            spawnParticles.Emit(15);
        }
        
        if (smokeParticles != null)
        {
            smokeParticles.transform.position = position;
            smokeParticles.Emit(8);
        }
        
        if (glowParticles != null)
        {
            glowParticles.transform.position = position;
            glowParticles.Emit(1);
        }
        
        // Düşman fade-in efekti
        if (enemyTransform != null)
        {
            StartCoroutine(FadeInEnemy(enemyTransform));
        }
    }
    
    /// <summary>
    /// Düşman spawn efektini oynat (boyut ile)
    /// </summary>
    public void PlaySpawnEffect(Vector3 position, Vector2 size)
    {
        float scaleFactor = Mathf.Max(size.x, size.y);
        
        if (spawnParticles != null)
        {
            spawnParticles.transform.position = position;
            var main = spawnParticles.main;
            var originalSize = main.startSize;
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f * scaleFactor, 0.35f * scaleFactor);
            spawnParticles.Emit(Mathf.RoundToInt(15 * scaleFactor));
            main.startSize = originalSize;
        }
        
        if (smokeParticles != null)
        {
            smokeParticles.transform.position = position;
            var main = smokeParticles.main;
            var originalSize = main.startSize;
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f * scaleFactor, 0.8f * scaleFactor);
            smokeParticles.Emit(Mathf.RoundToInt(8 * scaleFactor));
            main.startSize = originalSize;
        }
        
        if (glowParticles != null)
        {
            glowParticles.transform.position = position;
            var main = glowParticles.main;
            var originalSize = main.startSize;
            main.startSize = 2f * scaleFactor;
            glowParticles.Emit(1);
            main.startSize = originalSize;
        }
    }
    
    private IEnumerator FadeInEnemy(Transform enemyTransform)
    {
        if (enemyTransform == null) yield break;
        
        SpriteRenderer[] sprites = enemyTransform.GetComponentsInChildren<SpriteRenderer>();
        
        // Başlangıç renklerini kaydet
        Color[] originalColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            originalColors[i] = sprites[i].color;
            sprites[i].color = new Color(
                originalColors[i].r, 
                originalColors[i].g, 
                originalColors[i].b, 
                0f
            );
        }
        
        // Başlangıç scale
        Vector3 originalScale = enemyTransform.localScale;
        enemyTransform.localScale = originalScale * 0.5f;
        
        float elapsed = 0f;
        
        while (elapsed < spawnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / spawnDuration;
            
            // Ease out
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            
            // Fade in
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                {
                    sprites[i].color = new Color(
                        Mathf.Lerp(spawnColor.r, originalColors[i].r, easedT),
                        Mathf.Lerp(spawnColor.g, originalColors[i].g, easedT),
                        Mathf.Lerp(spawnColor.b, originalColors[i].b, easedT),
                        easedT * originalColors[i].a
                    );
                }
            }
            
            // Scale up
            if (enemyTransform != null)
            {
                enemyTransform.localScale = Vector3.Lerp(originalScale * 0.5f, originalScale, easedT);
            }
            
            yield return null;
        }
        
        // Final değerler
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
            {
                sprites[i].color = originalColors[i];
            }
        }
        
        if (enemyTransform != null)
        {
            enemyTransform.localScale = originalScale;
        }
    }
    
    private void CreateSpawnParticles()
    {
        GameObject particleObj = new GameObject("SpawnParticles");
        particleObj.transform.SetParent(transform);
        
        spawnParticles = particleObj.AddComponent<ParticleSystem>();
        
        var main = spawnParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.5f; // Yukarı yüzme
        main.startColor = spawnColor;
        main.playOnAwake = false;
        
        var emission = spawnParticles.emission;
        emission.rateOverTime = 0;
        
        // Şekil - yarım küre (yukarı doğru)
        var shape = spawnParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.3f;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        
        // Boyut
        var sizeOverLifetime = spawnParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.3f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Renk
        var colorOverLifetime = spawnParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(spawnColor, 0f),
                new GradientColorKey(new Color(0.8f, 0.2f, 1f), 0.5f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 20;
        
        // Texture
        CreateParticleTexture(renderer, 32);
    }
    
    private void CreateSmokeParticles()
    {
        GameObject smokeObj = new GameObject("SpawnSmoke");
        smokeObj.transform.SetParent(transform);
        
        smokeParticles = smokeObj.AddComponent<ParticleSystem>();
        
        var main = smokeParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.2f;
        main.startColor = smokeColor;
        main.playOnAwake = false;
        
        var emission = smokeParticles.emission;
        emission.rateOverTime = 0;
        
        // Şekil
        var shape = smokeParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;
        
        // Dönüş
        var rotationOverLifetime = smokeParticles.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-1f, 1f);
        
        // Boyut
        var sizeOverLifetime = smokeParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.3f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 1.5f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Renk
        var colorOverLifetime = smokeParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(smokeColor, 0f),
                new GradientColorKey(smokeColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0.4f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var renderer = smokeObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 19;
        
        CreateParticleTexture(renderer, 64);
    }
    
    private void CreateGlowParticles()
    {
        GameObject glowObj = new GameObject("SpawnGlow");
        glowObj.transform.SetParent(transform);
        
        glowParticles = glowObj.AddComponent<ParticleSystem>();
        
        var main = glowParticles.main;
        main.startLifetime = 0.4f;
        main.startSpeed = 0f;
        main.startSize = 2f;
        main.maxParticles = 5;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(spawnColor.r, spawnColor.g, spawnColor.b, 0.6f);
        main.playOnAwake = false;
        
        var emission = glowParticles.emission;
        emission.rateOverTime = 0;
        
        // Boyut animasyonu
        var sizeOverLifetime = glowParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.3f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Renk
        var colorOverLifetime = glowParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(spawnColor, 0.5f),
                new GradientColorKey(spawnColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.5f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var renderer = glowObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 18;
        
        CreateParticleTexture(renderer, 64);
    }
    
    private void CreateParticleTexture(ParticleSystemRenderer renderer, int size, bool additive = true)
    {
        // ParticleHelper kullan
        if (additive)
        {
            ParticleHelper.ApplyMaterial(renderer, true, ParticleHelper.GetGlowTexture());
        }
        else
        {
            ParticleHelper.ApplyMaterial(renderer, false, ParticleHelper.GetSoftCircleTexture());
        }
    }
}
