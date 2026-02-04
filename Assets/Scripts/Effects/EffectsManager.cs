using UnityEngine;

/// <summary>
/// Tüm efekt sistemlerini otomatik olarak başlatır.
/// GameManager'a eklenebilir veya sahneye tek başına konulabilir.
/// </summary>
public class EffectsManager : MonoBehaviour
{
    public static EffectsManager Instance { get; private set; }
    
    [Header("Efekt Ayarları")]
    [SerializeField] private bool enableHitEffects = true;
    [SerializeField] private bool enableHitStop = true;
    [SerializeField] private bool enableDamageVignette = true;
    [SerializeField] private bool enableCoinEffect = true;
    [SerializeField] private bool enableHealEffect = true;
    [SerializeField] private bool enableComboSystem = true;
    [SerializeField] private bool enableCriticalHit = true;
    [SerializeField] private bool enableEnemySpawnEffect = true;
    [SerializeField] private bool enableAttackTrail = true;
    [SerializeField] private bool enableDashAfterimage = true;
    [SerializeField] private bool enableAtmosphericFog = true;
    
    [Header("Yeni Efektler")]
    [SerializeField] private bool enableFootprints = true;
    [SerializeField] private bool enableWallSparks = true;
    [SerializeField] private bool enableSoulParticles = true;
    [SerializeField] private bool enableHeatDistortion = true;
    [SerializeField] private bool enableWeatherEffects = true;
    [SerializeField] private bool enableSkillColorBurst = true;
    [SerializeField] private bool enableAbilityPickup = true;
    [SerializeField] private WeatherEffects.WeatherType defaultWeather = WeatherEffects.WeatherType.Embers;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeEffects();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeEffects()
    {
        // Hit Effect
        if (enableHitEffects && HitEffect.Instance == null)
        {
            GameObject hitEffectObj = new GameObject("HitEffect");
            hitEffectObj.transform.SetParent(transform);
            hitEffectObj.AddComponent<HitEffect>();
        }
        
        // Hit Stop
        if (enableHitStop && HitStop.Instance == null)
        {
            GameObject hitStopObj = new GameObject("HitStop");
            hitStopObj.transform.SetParent(transform);
            hitStopObj.AddComponent<HitStop>();
        }
        
        // Damage Vignette
        if (enableDamageVignette && DamageVignette.Instance == null)
        {
            GameObject damageVignetteObj = new GameObject("DamageVignette");
            damageVignetteObj.transform.SetParent(transform);
            damageVignetteObj.AddComponent<DamageVignette>();
        }
        
        // Coin Effect
        if (enableCoinEffect && CoinCollectEffect.Instance == null)
        {
            GameObject coinEffectObj = new GameObject("CoinCollectEffect");
            coinEffectObj.transform.SetParent(transform);
            coinEffectObj.AddComponent<CoinCollectEffect>();
        }
        
        // Heal Effect
        if (enableHealEffect && HealVFX.Instance == null)
        {
            GameObject healEffectObj = new GameObject("HealVFX");
            healEffectObj.transform.SetParent(transform);
            healEffectObj.AddComponent<HealVFX>();
        }
        
        // Combo System
        if (enableComboSystem && ComboSystem.Instance == null)
        {
            GameObject comboObj = new GameObject("ComboSystem");
            comboObj.transform.SetParent(transform);
            comboObj.AddComponent<ComboSystem>();
        }
        
        // Critical Hit System
        if (enableCriticalHit && CriticalHitSystem.Instance == null)
        {
            GameObject critObj = new GameObject("CriticalHitSystem");
            critObj.transform.SetParent(transform);
            critObj.AddComponent<CriticalHitSystem>();
        }
        
        // Enemy Spawn Effect
        if (enableEnemySpawnEffect && EnemySpawnEffect.Instance == null)
        {
            GameObject spawnEffectObj = new GameObject("EnemySpawnEffect");
            spawnEffectObj.transform.SetParent(transform);
            spawnEffectObj.AddComponent<EnemySpawnEffect>();
        }
        
        // Attack Trail
        if (enableAttackTrail && AttackTrail.Instance == null)
        {
            GameObject trailObj = new GameObject("AttackTrail");
            trailObj.transform.SetParent(transform);
            trailObj.AddComponent<AttackTrail>();
        }
        
        // Dash Afterimage
        if (enableDashAfterimage && DashAfterimage.Instance == null)
        {
            GameObject afterimageObj = new GameObject("DashAfterimage");
            afterimageObj.transform.SetParent(transform);
            afterimageObj.AddComponent<DashAfterimage>();
        }
        
        // Atmospheric Fog
        if (enableAtmosphericFog && AtmosphericFog.Instance == null)
        {
            GameObject fogObj = new GameObject("AtmosphericFog");
            fogObj.transform.SetParent(transform);
            fogObj.AddComponent<AtmosphericFog>();
        }
        
        // Footprint Effect - Ayak izleri
        if (enableFootprints && FootprintEffect.Instance == null)
        {
            GameObject footprintObj = new GameObject("FootprintEffect");
            footprintObj.transform.SetParent(transform);
            footprintObj.AddComponent<FootprintEffect>();
        }
        
        // Wall Spark Effect - Duvara çarpınca kıvılcım
        if (enableWallSparks && WallSparkEffect.Instance == null)
        {
            GameObject sparkObj = new GameObject("WallSparkEffect");
            sparkObj.transform.SetParent(transform);
            sparkObj.AddComponent<WallSparkEffect>();
        }
        
        // Soul Particle Effect - Düşman ölümünde ruh
        if (enableSoulParticles && SoulParticleEffect.Instance == null)
        {
            GameObject soulObj = new GameObject("SoulParticleEffect");
            soulObj.transform.SetParent(transform);
            soulObj.AddComponent<SoulParticleEffect>();
        }
        
        // Heat Distortion Effect - Lav yakınında sıcaklık
        if (enableHeatDistortion && HeatDistortionEffect.Instance == null)
        {
            GameObject heatObj = new GameObject("HeatDistortionEffect");
            heatObj.transform.SetParent(transform);
            heatObj.AddComponent<HeatDistortionEffect>();
        }
        
        // Weather Effects - Yağmur/Kar/Kül
        if (enableWeatherEffects && WeatherEffects.Instance == null)
        {
            GameObject weatherObj = new GameObject("WeatherEffects");
            weatherObj.transform.SetParent(transform);
            WeatherEffects weather = weatherObj.AddComponent<WeatherEffects>();
            // Varsayılan hava durumu ayarla (cehennem teması için Embers)
            weather.SetWeather(defaultWeather);
        }
        
        // Skill Color Burst - Skill kullanımında renk patlaması
        if (enableSkillColorBurst && SkillColorBurst.Instance == null)
        {
            GameObject burstObj = new GameObject("SkillColorBurst");
            burstObj.transform.SetParent(transform);
            burstObj.AddComponent<SkillColorBurst>();
        }
        
        // Ability Pickup Effect - Sandıktan ability çıkınca efekt
        if (enableAbilityPickup && AbilityPickupEffect.Instance == null)
        {
            GameObject abilityObj = new GameObject("AbilityPickupEffect");
            abilityObj.transform.SetParent(transform);
            abilityObj.AddComponent<AbilityPickupEffect>();
        }
        
        Debug.Log("[EffectsManager] Tüm efekt sistemleri başlatıldı!");
    }
}
