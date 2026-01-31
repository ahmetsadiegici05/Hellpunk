using UnityEngine;

/// <summary>
/// Hellpunk temasına uygun ateş/kıvılcım parçacıkları oluşturur
/// Bu script'i CANVAS DIŞINDA boş bir GameObject'e ekleyin (Main Camera'nın yanına)
/// </summary>
public class MenuParticleSpawner : MonoBehaviour
{
    [Header("Particle Settings")]
    [SerializeField] private bool autoCreateParticles = true;
    [SerializeField] private ParticleSystemType particleType = ParticleSystemType.FireEmbers;
    [SerializeField] private float zPosition = 5f; // Kameradan uzaklık

    public enum ParticleSystemType
    {
        FireEmbers,      // Ateş kıvılcımları
        FloatingDust,    // Yüzen toz
        MagicSparkles,   // Sihirli parıltılar
        Smoke            // Duman
    }

    private ParticleSystem particles;
    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;
        
        if (autoCreateParticles)
        {
            CreateParticleSystem();
        }
    }

    public void CreateParticleSystem()
    {
        // Particle System oluştur
        GameObject particleObj = new GameObject("Menu_Particles");
        particleObj.transform.SetParent(transform);
        
        // Kameranın önüne konumlandır
        if (mainCam != null)
        {
            particleObj.transform.position = mainCam.transform.position + mainCam.transform.forward * zPosition;
        }
        else
        {
            particleObj.transform.localPosition = new Vector3(0, 0, zPosition);
        }

        particles = particleObj.AddComponent<ParticleSystem>();

        // Renderer ayarları
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 100;
        
        // Default material kullan (yoksa görünmez!)
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetColor("_Color", Color.white);

        switch (particleType)
        {
            case ParticleSystemType.FireEmbers:
                SetupFireEmbers();
                break;
            case ParticleSystemType.FloatingDust:
                SetupFloatingDust();
                break;
            case ParticleSystemType.MagicSparkles:
                SetupMagicSparkles();
                break;
            case ParticleSystemType.Smoke:
                SetupSmoke();
                break;
        }

        particles.Play();
    }

    private void SetupFireEmbers()
    {
        var main = particles.main;
        main.startLifetime = 4f;
        main.startSpeed = 2f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.5f, 0.1f, 1f),  // Turuncu
            new Color(1f, 0.8f, 0.2f, 1f)   // Sarı
        );
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.3f; // Yukarı yüzme

        var emission = particles.emission;
        emission.rateOverTime = 15f;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(20f, 1f, 1f);
        shape.position = new Vector3(0, -6f, 0);

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.6f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.3f, 0.1f), 0.5f),
                new GradientColorKey(new Color(0.5f, 0.1f, 0.1f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.1f),
                new GradientAlphaKey(1f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 0.3f));

        // Hafif yatay hareket
        var velocityOverLifetime = particles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
    }

    private void SetupFloatingDust()
    {
        var main = particles.main;
        main.startLifetime = 8f;
        main.startSpeed = 0.3f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
        main.startColor = new Color(1f, 1f, 1f, 0.3f);
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = particles.emission;
        emission.rateOverTime = 5f;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(20f, 12f, 1f);

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.3f, 0.3f),
                new GradientAlphaKey(0.3f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        var noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 0.5f;
    }

    private void SetupMagicSparkles()
    {
        var main = particles.main;
        main.startLifetime = 3f;
        main.startSpeed = 0.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.1f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.8f, 0.4f, 1f, 1f),  // Mor
            new Color(1f, 0.5f, 0.8f, 1f)   // Pembe
        );
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = particles.emission;
        emission.rateOverTime = 20f;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(18f, 10f, 1f);

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.8f, 0.4f, 1f), 0f),
                new GradientColorKey(new Color(1f, 0.6f, 0.9f), 0.5f),
                new GradientColorKey(new Color(0.6f, 0.3f, 0.8f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(1f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0f);
        curve.AddKey(0.2f, 1f);
        curve.AddKey(0.8f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var noise = particles.noise;
        noise.enabled = true;
        noise.strength = 1f;
        noise.frequency = 1f;
    }

    private void SetupSmoke()
    {
        var main = particles.main;
        main.startLifetime = 6f;
        main.startSpeed = 0.8f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startColor = new Color(0.2f, 0.15f, 0.25f, 0.3f); // Koyu mor duman
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.1f;

        var emission = particles.emission;
        emission.rateOverTime = 3f;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(15f, 1f, 1f);
        shape.position = new Vector3(0, -5f, 0);

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.3f, 0.2f, 0.35f), 0f),
                new GradientColorKey(new Color(0.2f, 0.15f, 0.25f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.2f, 0.2f),
                new GradientAlphaKey(0.15f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 2f));

        var noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 0.3f;
    }
}
