using UnityEngine;
using UnityEngine.SceneManagement;

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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSoulSystem();
        EnsureTimeSlowSystem();
        EnsureGuitarSkillSystem();
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
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Sistemleri başlangıçta oluştur
        EnsureSoulSystem();
        EnsureTimeSlowSystem();
        EnsureGuitarSkillSystem();
    }
    
    private void EnsureSoulSystem()
    {
        // SoulSystem
        if (SoulSystem.Instance == null)
        {
            var existingSystem = FindFirstObjectByType<SoulSystem>();
            if (existingSystem != null)
            {
                DontDestroyOnLoad(existingSystem.gameObject);
            }
            else
            {
                GameObject soulSystemObj = new GameObject("SoulSystem");
                soulSystemObj.AddComponent<SoulSystem>();
                DontDestroyOnLoad(soulSystemObj);
            }
        }
        
        // SoulUI
        if (FindFirstObjectByType<SoulUI>() == null)
        {
            // Sahnede yoksa oluştur (eğer sahnede varsa zaten bu if'e girmez ve yenisini oluşturmaz)
            GameObject soulUIObj = new GameObject("SoulUI");
            soulUIObj.AddComponent<SoulUI>();
            DontDestroyOnLoad(soulUIObj);
        }
        else
        {
            // Sahnede varsa onu DontDestroyOnLoad yap ki sahne geçişlerinde gitmesin
            var ui = FindFirstObjectByType<SoulUI>();
            DontDestroyOnLoad(ui.gameObject);
        }
    }

    private void EnsureTimeSlowSystem()
    {
        // 1. Audio Controller
        var audioController = FindFirstObjectByType<TimeSlowAudioController>();
        if (audioController == null)
        {
            GameObject audioObj = new GameObject("TimeSlowAudioController");
            // Ses kliplerinin atanması prefab üzerinden olmalı, burası kodla oluşturuyorsa ses çıkmayabilir
            // ancak sistemin çalışması için referans gereklidir.
            audioObj.AddComponent<TimeSlowAudioController>();
            DontDestroyOnLoad(audioObj);
        }
        else
        {
            DontDestroyOnLoad(audioController.gameObject);
        }

        // 2. Ability on Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (player.GetComponent<TimeSlowAbility>() == null)
            {
                player.AddComponent<TimeSlowAbility>();
                Debug.Log("GameManager: TimeSlowAbility Player'a eklendi.");
            }
        }

        // 3. UI
        if (TimeSlowUI.Instance == null)
        {
            var existingUI = FindFirstObjectByType<TimeSlowUI>();
            if (existingUI == null)
            {
                GameObject uiObj = new GameObject("TimeSlowUI");
                uiObj.AddComponent<TimeSlowUI>();
                DontDestroyOnLoad(uiObj);
            }
            else
            {
                DontDestroyOnLoad(existingUI.gameObject);
            }
        }
    }

    private void EnsureGuitarSkillSystem()
    {
        // 1. Player Components (GuitarSkillSystem, SkillInputUI)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (player.GetComponent<GuitarSkillSystem>() == null)
            {
                player.AddComponent<GuitarSkillSystem>();
                Debug.Log("GameManager: GuitarSkillSystem Player'a eklendi.");
            }

            if (player.GetComponent<SkillInputUI>() == null)
            {
                player.AddComponent<SkillInputUI>();
                Debug.Log("GameManager: SkillInputUI Player'a eklendi.");
            }
        }

        // 2. HUD (SkillCooldownUI)
        var skillUI = FindFirstObjectByType<SkillCooldownUI>();
        if (skillUI == null)
        {
            GameObject uiObj = new GameObject("SkillCooldownUI_Manager");
            uiObj.AddComponent<SkillCooldownUI>();
            DontDestroyOnLoad(uiObj);
        }
        else
        {
            DontDestroyOnLoad(skillUI.gameObject);
        }
    }

    public void PlayHealSound()
    {
        if (healSound != null) audioSource.PlayOneShot(healSound);
    }

    public void PlayButtonSound()
    {
        if (buttonClickSound != null) audioSource.PlayOneShot(buttonClickSound);
    }

    public void PlayAttackSound()
    {
        if (attackSound != null) audioSource.PlayOneShot(attackSound);
    }

    public void PlayFootstepSound()
    {
        if (footstepSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(footstepSound);
            audioSource.pitch = 1f;
        }
    }

    public void PlayBossDeathSound()
    {
        if (bossDeathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(bossDeathSound);
        }
    }

    public void PlayEnemyHitSound()
    {
        if (enemyHitSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(enemyHitSound);
            audioSource.pitch = 1f;
        }
    }

    public void PlayPlayerHitSound()
    {
        if (playerHitSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(playerHitSound);
            audioSource.pitch = 1f;
        }
    }
}