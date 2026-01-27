using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip healSound;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private AudioClip bossDeathSound;
    [SerializeField] private AudioClip enemyHitSound;
    [SerializeField] private AudioClip playerHitSound;

    [Header("Data")]
    public int coin;

    [Header("World Renderers")]
    public List<SpriteRenderer> sprites;
    public List<Tilemap> tilemaps;

    [Header("Chest System")]
    public GameObject chestPrefab;
    public List<Transform> randomPoints;

    [Header("Ulti")]
    public GameObject ultiObject;
    public Animator ultiAnimator;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

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

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        EnsureSoulSystem();
        EnsureTimeSlowSystem();
        EnsureGuitarSkillSystem();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSoulSystem();
        EnsureTimeSlowSystem();
        EnsureGuitarSkillSystem();

        SpawnRandomChests(); // 🔥 HER SAHNE YÜKLENDİĞİNDE
    }

    // ================= CHEST SPAWN =================

    private void SpawnRandomChests()
    {
        randomPoints = new List<Transform>();

        GameObject[] points = GameObject.FindGameObjectsWithTag("ChestPoint");
        foreach (var p in points)
            randomPoints.Add(p.transform);

        if (chestPrefab == null || randomPoints.Count == 0)
        {
            Debug.LogWarning("Chest spawn noktası bulunamadı!");
            return;
        }

        // Eski chestleri temizle
        GameObject[] oldChests = GameObject.FindGameObjectsWithTag("Chest");
        foreach (var chest in oldChests)
            Destroy(chest);

        // Random noktaları kopyala
        List<Transform> shuffledPoints = new List<Transform>(randomPoints);

        // Fisher–Yates shuffle
        for (int i = 0; i < shuffledPoints.Count; i++)
        {
            int rnd = Random.Range(i, shuffledPoints.Count);
            Transform temp = shuffledPoints[i];
            shuffledPoints[i] = shuffledPoints[rnd];
            shuffledPoints[rnd] = temp;
        }

        int spawnCount = Mathf.Min(10, shuffledPoints.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            Instantiate(
                chestPrefab,
                shuffledPoints[i].position,
                Quaternion.identity
            );
        }
    }

    // ================= SYSTEMS =================

    private void EnsureSoulSystem()
    {
        if (SoulSystem.Instance == null)
        {
            var existing = FindFirstObjectByType<SoulSystem>();
            if (existing != null)
                DontDestroyOnLoad(existing.gameObject);
            else
            {
                GameObject obj = new GameObject("SoulSystem");
                obj.AddComponent<SoulSystem>();
                DontDestroyOnLoad(obj);
            }
        }

        var soulUI = FindFirstObjectByType<SoulUI>();
        if (soulUI != null)
            DontDestroyOnLoad(soulUI.gameObject);
        else
        {
            GameObject uiObj = new GameObject("SoulUI");
            uiObj.AddComponent<SoulUI>();
            DontDestroyOnLoad(uiObj);
        }
    }

    private void EnsureTimeSlowSystem()
    {
        var audioController = FindFirstObjectByType<TimeSlowAudioController>();
        if (audioController != null)
            DontDestroyOnLoad(audioController.gameObject);
        else
        {
            GameObject obj = new GameObject("TimeSlowAudioController");
            obj.AddComponent<TimeSlowAudioController>();
            DontDestroyOnLoad(obj);
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.GetComponent<TimeSlowAbility>() == null)
            player.AddComponent<TimeSlowAbility>();
    }

    private void EnsureGuitarSkillSystem()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (player.GetComponent<GuitarSkillSystem>() == null)
                player.AddComponent<GuitarSkillSystem>();

            if (player.GetComponent<SkillInputUI>() == null)
                player.AddComponent<SkillInputUI>();
        }

        var skillUI = FindFirstObjectByType<SkillCooldownUI>();
        if (skillUI != null)
            DontDestroyOnLoad(skillUI.gameObject);
        else
        {
            GameObject uiObj = new GameObject("SkillCooldownUI_Manager");
            uiObj.AddComponent<SkillCooldownUI>();
            DontDestroyOnLoad(uiObj);
        }
    }

    // ================= AUDIO =================

    public void PlayHealSound() => PlayOneShot(healSound);
    public void PlayButtonSound() => PlayOneShot(buttonClickSound);
    public void PlayAttackSound() => PlayOneShot(attackSound);

    public void PlayFootstepSound()
    {
        if (footstepSound == null) return;
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        PlayOneShot(footstepSound);
        audioSource.pitch = 1f;
    }

    public void PlayBossDeathSound() => PlayOneShot(bossDeathSound);

    public void PlayEnemyHitSound()
    {
        if (enemyHitSound == null) return;
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        PlayOneShot(enemyHitSound);
        audioSource.pitch = 1f;
    }

    public void PlayPlayerHitSound()
    {
        if (playerHitSound == null) return;
        audioSource.pitch = Random.Range(0.95f, 1.05f);
        PlayOneShot(playerHitSound);
        audioSource.pitch = 1f;
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
}
