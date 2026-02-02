using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Dash yaparken arkada kalan gölge/iz efekti
/// PlayerMovement'a entegre edilmeli
/// </summary>
public class DashAfterimage : MonoBehaviour
{
    public static DashAfterimage Instance { get; private set; }
    
    [Header("Afterimage Ayarları")]
    [SerializeField] private Color afterimageColor = new Color(0.3f, 0.5f, 1f, 0.6f); // Mavi tint
    [SerializeField] private float afterimageDuration = 0.3f;
    [SerializeField] private float spawnInterval = 0.02f; // Ne sıklıkla spawn
    [SerializeField] private int poolSize = 15;
    
    [Header("Görsel")]
    [SerializeField] private bool useColorTint = true;
    [SerializeField] private bool scaleDown = true;
    
    private List<SpriteRenderer> afterimagePool = new List<SpriteRenderer>();
    private Queue<SpriteRenderer> availableAfterimages = new Queue<SpriteRenderer>();
    
    private Transform playerTransform;
    private SpriteRenderer playerSprite;
    private bool isSpawning = false;
    
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
        CreatePool();
        FindPlayer();
    }
    
    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerSprite = player.GetComponent<SpriteRenderer>();
        }
    }
    
    private void CreatePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject afterimageObj = new GameObject($"Afterimage_{i}");
            afterimageObj.transform.SetParent(transform);
            
            SpriteRenderer sr = afterimageObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5; // Oyuncunun arkasında
            afterimageObj.SetActive(false);
            
            afterimagePool.Add(sr);
            availableAfterimages.Enqueue(sr);
        }
    }
    
    /// <summary>
    /// Dash başladığında çağır
    /// </summary>
    public void StartDashEffect(float dashDuration)
    {
        if (playerTransform == null || playerSprite == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }
        
        if (!isSpawning)
        {
            StartCoroutine(SpawnAfterimages(dashDuration));
        }
    }
    
    /// <summary>
    /// Dash bittiğinde çağır (opsiyonel - otomatik durur)
    /// </summary>
    public void StopDashEffect()
    {
        isSpawning = false;
    }
    
    /// <summary>
    /// Tek bir afterimage spawn et (manuel kontrol için)
    /// </summary>
    public void SpawnSingleAfterimage()
    {
        if (playerTransform == null || playerSprite == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }
        
        SpawnAfterimage();
    }
    
    /// <summary>
    /// Belirtilen SpriteRenderer ile afterimage spawn et
    /// </summary>
    public void SpawnSingleAfterimage(SpriteRenderer sourceSprite)
    {
        if (sourceSprite == null || sourceSprite.sprite == null) return;
        if (availableAfterimages.Count == 0) return;
        
        SpriteRenderer afterimage = availableAfterimages.Dequeue();
        
        // Pozisyon ve sprite kopyala
        afterimage.transform.position = sourceSprite.transform.position;
        afterimage.transform.rotation = sourceSprite.transform.rotation;
        afterimage.transform.localScale = sourceSprite.transform.lossyScale;
        afterimage.sprite = sourceSprite.sprite;
        afterimage.flipX = sourceSprite.flipX;
        afterimage.flipY = sourceSprite.flipY;
        
        // Renk ayarla
        if (useColorTint)
        {
            afterimage.color = afterimageColor;
        }
        else
        {
            Color c = sourceSprite.color;
            afterimage.color = new Color(c.r, c.g, c.b, afterimageColor.a);
        }
        
        afterimage.gameObject.SetActive(true);
        
        StartCoroutine(FadeAfterimage(afterimage));
    }
    
    private IEnumerator SpawnAfterimages(float duration)
    {
        isSpawning = true;
        float elapsed = 0f;
        
        while (elapsed < duration && isSpawning)
        {
            SpawnAfterimage();
            
            elapsed += spawnInterval;
            yield return new WaitForSeconds(spawnInterval);
        }
        
        isSpawning = false;
    }
    
    private void SpawnAfterimage()
    {
        if (availableAfterimages.Count == 0) return;
        if (playerSprite == null || playerSprite.sprite == null) return;
        
        SpriteRenderer afterimage = availableAfterimages.Dequeue();
        
        // Pozisyon ve sprite kopyala
        afterimage.transform.position = playerTransform.position;
        afterimage.transform.rotation = playerTransform.rotation;
        afterimage.transform.localScale = playerTransform.localScale;
        afterimage.sprite = playerSprite.sprite;
        afterimage.flipX = playerSprite.flipX;
        afterimage.flipY = playerSprite.flipY;
        
        // Renk ayarla
        if (useColorTint)
        {
            afterimage.color = afterimageColor;
        }
        else
        {
            afterimage.color = new Color(
                playerSprite.color.r,
                playerSprite.color.g,
                playerSprite.color.b,
                afterimageColor.a
            );
        }
        
        afterimage.gameObject.SetActive(true);
        
        StartCoroutine(FadeAfterimage(afterimage));
    }
    
    private IEnumerator FadeAfterimage(SpriteRenderer afterimage)
    {
        float elapsed = 0f;
        Color startColor = afterimage.color;
        Vector3 startScale = afterimage.transform.localScale;
        
        while (elapsed < afterimageDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / afterimageDuration;
            
            // Solma
            float alpha = Mathf.Lerp(startColor.a, 0f, t);
            afterimage.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            
            // Küçülme (opsiyonel)
            if (scaleDown)
            {
                float scale = Mathf.Lerp(1f, 0.8f, t);
                afterimage.transform.localScale = startScale * scale;
            }
            
            yield return null;
        }
        
        afterimage.gameObject.SetActive(false);
        availableAfterimages.Enqueue(afterimage);
    }
    
    /// <summary>
    /// Afterimage rengini değiştir
    /// </summary>
    public void SetColor(Color color)
    {
        afterimageColor = color;
    }
    
    /// <summary>
    /// Ability için özel renk (örn: time slow sırasında farklı renk)
    /// </summary>
    public void SetAbilityColor()
    {
        afterimageColor = new Color(0.8f, 0.4f, 1f, 0.5f); // Mor
    }
    
    /// <summary>
    /// Normal renge dön
    /// </summary>
    public void ResetColor()
    {
        afterimageColor = new Color(0.3f, 0.5f, 1f, 0.6f); // Varsayılan mavi
    }
}
