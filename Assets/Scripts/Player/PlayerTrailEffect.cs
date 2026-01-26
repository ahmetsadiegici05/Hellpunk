using UnityEngine;

/// <summary>
/// Oyuncu hareket ederken arkasında iz bırakan trail efekti.
/// Koşarken hafif, dash/zıplamada daha belirgin.
/// </summary>
public class PlayerTrailEffect : MonoBehaviour
{
    [Header("Trail Settings")]
    [SerializeField] private int maxTrailCount = 8;
    [SerializeField] private float trailSpawnInterval = 0.05f;
    [SerializeField] private float trailDuration = 0.3f;
    [SerializeField] private float minSpeedForTrail = 3f;
    
    [Header("Appearance")]
    [SerializeField] private Color trailColor = new Color(0.5f, 0.7f, 1f, 0.4f);
    [SerializeField] private float trailAlphaMultiplier = 0.5f;
    [SerializeField] private bool usePlayerSprite = true;
    
    [Header("Jump Trail")]
    [SerializeField] private bool showJumpTrail = true;
    [SerializeField] private Color jumpTrailColor = new Color(1f, 1f, 1f, 0.5f);
    
    private class TrailGhost
    {
        public GameObject obj;
        public SpriteRenderer renderer;
        public float lifetime;
        public float maxLifetime;
        public Color startColor;
    }
    
    private TrailGhost[] ghosts;
    private int currentIndex = 0;
    private float spawnTimer;
    
    private SpriteRenderer playerRenderer;
    private Rigidbody2D playerRb;
    private Animator playerAnimator;
    
    private bool wasGrounded = true;
    private float lastYVelocity;
    
    private void Awake()
    {
        playerRenderer = GetComponent<SpriteRenderer>();
        playerRb = GetComponent<Rigidbody2D>();
        playerAnimator = GetComponent<Animator>();
    }
    
    private void Start()
    {
        if (playerRenderer == null)
        {
            enabled = false;
            return;
        }
        
        ghosts = new TrailGhost[maxTrailCount];
        
        for (int i = 0; i < maxTrailCount; i++)
        {
            ghosts[i] = CreateGhost(i);
        }
    }
    
    private TrailGhost CreateGhost(int index)
    {
        TrailGhost ghost = new TrailGhost();
        
        GameObject obj = new GameObject($"TrailGhost_{index}");
        obj.transform.SetParent(null); // Parent'sız (dünya koordinatlarında)
        
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = playerRenderer.sortingLayerName;
        sr.sortingOrder = playerRenderer.sortingOrder - 1;
        
        ghost.obj = obj;
        ghost.renderer = sr;
        ghost.lifetime = 0f;
        
        obj.SetActive(false);
        
        return ghost;
    }
    
    private void Update()
    {
        UpdateGhosts();
        
        if (playerRb == null) return;
        
        float speed = playerRb.linearVelocity.magnitude;
        bool isGrounded = IsGrounded();
        
        // Zıplama anında trail oluştur
        if (showJumpTrail && wasGrounded && !isGrounded && playerRb.linearVelocity.y > 0)
        {
            SpawnGhost(jumpTrailColor, 0.4f);
        }
        
        // Yere iniş anında trail
        if (showJumpTrail && !wasGrounded && isGrounded && lastYVelocity < -5f)
        {
            SpawnGhost(new Color(0.8f, 0.8f, 0.8f, 0.4f), 0.3f);
        }
        
        wasGrounded = isGrounded;
        lastYVelocity = playerRb.linearVelocity.y;
        
        // Normal koşma trail'i
        if (speed >= minSpeedForTrail && isGrounded)
        {
            spawnTimer += Time.deltaTime;
            
            if (spawnTimer >= trailSpawnInterval)
            {
                spawnTimer = 0f;
                
                // Hıza göre alpha ayarla
                float speedFactor = Mathf.Clamp01((speed - minSpeedForTrail) / 10f);
                Color color = trailColor;
                color.a *= (0.3f + speedFactor * 0.7f);
                
                SpawnGhost(color, trailDuration);
            }
        }
    }
    
    private void SpawnGhost(Color color, float duration)
    {
        TrailGhost ghost = ghosts[currentIndex];
        currentIndex = (currentIndex + 1) % maxTrailCount;
        
        if (usePlayerSprite && playerRenderer.sprite != null)
        {
            ghost.renderer.sprite = playerRenderer.sprite;
        }
        
        ghost.obj.transform.position = transform.position;
        ghost.obj.transform.localScale = transform.lossyScale;
        ghost.obj.transform.rotation = transform.rotation;
        
        ghost.startColor = color;
        ghost.maxLifetime = duration;
        ghost.lifetime = duration;
        
        ghost.renderer.color = color;
        ghost.obj.SetActive(true);
    }
    
    private void UpdateGhosts()
    {
        for (int i = 0; i < ghosts.Length; i++)
        {
            TrailGhost ghost = ghosts[i];
            if (ghost == null || !ghost.obj.activeSelf) continue;
            
            ghost.lifetime -= Time.deltaTime;
            
            if (ghost.lifetime <= 0)
            {
                ghost.obj.SetActive(false);
                continue;
            }
            
            // Fade out
            float t = ghost.lifetime / ghost.maxLifetime;
            Color c = ghost.startColor;
            c.a = ghost.startColor.a * t * trailAlphaMultiplier;
            ghost.renderer.color = c;
            
            // Hafif shrink
            float scale = Mathf.Lerp(0.9f, 1f, t);
            ghost.obj.transform.localScale = transform.lossyScale * scale;
        }
    }
    
    private bool IsGrounded()
    {
        // Basit ground check
        if (playerAnimator != null)
        {
            return playerAnimator.GetBool("grounded");
        }
        return true;
    }
    
    private void OnDestroy()
    {
        if (ghosts != null)
        {
            foreach (var ghost in ghosts)
            {
                if (ghost?.obj != null)
                    Destroy(ghost.obj);
            }
        }
    }
}
