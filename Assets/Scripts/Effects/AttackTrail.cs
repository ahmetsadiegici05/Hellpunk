using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Yumruk atarken iz efekti - Saldırı hareketinin arkasında parlak iz
/// PlayerAttack scriptine entegre edilmeli
/// </summary>
public class AttackTrail : MonoBehaviour
{
    public static AttackTrail Instance { get; private set; }
    
    [Header("Trail Ayarları")]
    [SerializeField] private Color trailColor = new Color(1f, 0.8f, 0.4f, 0.8f); // Altın sarısı
    [SerializeField] private float trailWidth = 0.3f;
    [SerializeField] private float trailDuration = 0.15f;
    [SerializeField] private int trailSegments = 10;
    
    [Header("Parçacık Ayarları")]
    [SerializeField] private bool useParticles = true;
    [SerializeField] private int particleCount = 8;
    
    private List<TrailRenderer> trailPool = new List<TrailRenderer>();
    private ParticleSystem trailParticles;
    
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
        CreateTrailPool();
        
        if (useParticles)
        {
            CreateTrailParticles();
        }
    }
    
    private void CreateTrailPool()
    {
        for (int i = 0; i < 5; i++)
        {
            TrailRenderer trail = CreateTrail();
            trail.gameObject.SetActive(false);
            trailPool.Add(trail);
        }
    }
    
    private TrailRenderer CreateTrail()
    {
        GameObject trailObj = new GameObject("AttackTrail");
        trailObj.transform.SetParent(transform);
        
        TrailRenderer trail = trailObj.AddComponent<TrailRenderer>();
        trail.time = trailDuration;
        trail.startWidth = trailWidth;
        trail.endWidth = 0f;
        trail.numCapVertices = 5;
        trail.numCornerVertices = 5;
        
        // Gradient
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(trailColor, 0.3f),
                new GradientColorKey(trailColor * 0.5f, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        trail.colorGradient = gradient;
        
        // Material
        trail.material = ParticleHelper.CreateAdditiveMaterial(ParticleHelper.GetSoftCircleTexture());
        trail.sortingOrder = 50;
        
        return trail;
    }
    
    private void CreateTrailParticles()
    {
        GameObject particleObj = new GameObject("TrailParticles");
        particleObj.transform.SetParent(transform);
        
        trailParticles = particleObj.AddComponent<ParticleSystem>();
        
        var main = trailParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = trailColor;
        main.playOnAwake = false;
        
        var emission = trailParticles.emission;
        emission.rateOverTime = 0;
        
        var shape = trailParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;
        
        var colorOverLifetime = trailParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(trailColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var sizeOverLifetime = trailParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 51;
        ParticleHelper.ApplyMaterial(renderer, true, ParticleHelper.GetSparkTexture());
    }
    
    /// <summary>
    /// Saldırı trail efektini başlat
    /// </summary>
    public void PlayAttackTrail(Transform attackPoint, Vector2 direction, float duration = 0.1f)
    {
        if (attackPoint == null) return;
        
        StartCoroutine(AttackTrailRoutine(attackPoint, direction, duration));
    }
    
    /// <summary>
    /// Ark şeklinde saldırı trail'i
    /// </summary>
    public void PlaySwingTrail(Vector3 startPos, Vector3 endPos, float arcHeight = 0.5f)
    {
        StartCoroutine(SwingTrailRoutine(startPos, endPos, arcHeight));
    }
    
    private IEnumerator AttackTrailRoutine(Transform attackPoint, Vector2 direction, float duration)
    {
        TrailRenderer trail = GetTrailFromPool();
        if (trail == null) yield break;
        
        trail.gameObject.SetActive(true);
        trail.Clear();
        
        float elapsed = 0f;
        Vector3 startPos = attackPoint.position;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Trail pozisyonunu güncelle
            trail.transform.position = startPos + (Vector3)(direction * t * 1.5f);
            
            // Parçacıklar
            if (useParticles && trailParticles != null && t < 0.5f)
            {
                trailParticles.transform.position = trail.transform.position;
                trailParticles.Emit(1);
            }
            
            yield return null;
        }
        
        // Trail'in solmasını bekle
        yield return new WaitForSeconds(trailDuration);
        
        trail.Clear();
        trail.gameObject.SetActive(false);
    }
    
    private IEnumerator SwingTrailRoutine(Vector3 startPos, Vector3 endPos, float arcHeight)
    {
        TrailRenderer trail = GetTrailFromPool();
        if (trail == null) yield break;
        
        trail.gameObject.SetActive(true);
        trail.Clear();
        
        float duration = 0.1f;
        float elapsed = 0f;
        
        Vector3 midPoint = (startPos + endPos) / 2f + Vector3.up * arcHeight;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Bezier curve
            Vector3 p1 = Vector3.Lerp(startPos, midPoint, t);
            Vector3 p2 = Vector3.Lerp(midPoint, endPos, t);
            trail.transform.position = Vector3.Lerp(p1, p2, t);
            
            // Parçacıklar
            if (useParticles && trailParticles != null)
            {
                trailParticles.transform.position = trail.transform.position;
                trailParticles.Emit(1);
            }
            
            yield return null;
        }
        
        yield return new WaitForSeconds(trailDuration);
        
        trail.Clear();
        trail.gameObject.SetActive(false);
    }
    
    private TrailRenderer GetTrailFromPool()
    {
        foreach (var trail in trailPool)
        {
            if (!trail.gameObject.activeInHierarchy)
            {
                return trail;
            }
        }
        
        // Pool dolu, yeni oluştur
        TrailRenderer newTrail = CreateTrail();
        trailPool.Add(newTrail);
        return newTrail;
    }
}
