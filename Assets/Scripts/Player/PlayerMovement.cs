using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance;

    [Header("Movement Settings")]
    [SerializeField] public float speed = 10f;
    [SerializeField] public float jumpPower = 15f;
    [SerializeField] private float footstepInterval = 0.3f;
    [SerializeField] private float groundGravity = 7f;
    [SerializeField] private float wallSlideGravity = 1.5f;
    [SerializeField] private float movementSmoothTime = 0.05f;
    [SerializeField] private ParticleSystem groundHitParticle;
    private bool wasGrounded;
    private ParticleSystem landingDustEffect; // Kod ile oluşturulan landing efekti
    private ParticleSystem runningDustEffect; // Koşarken toz efekti
    private ParticleSystem jumpDustEffect; // Zıplama efekti
    private float runDustTimer = 0f;
    private float runDustInterval = 0.15f; // Ne sıklıkla toz çıksın

    [Header("Game Feel Settings")]
    [SerializeField] private float coyoteTime = 0.15f; 
    [SerializeField] private float jumpBufferTime = 0.1f; 
    [SerializeField] private float variableJumpMultiplier = 0.5f; 

    [Header("Double Jump Settings")]
    [SerializeField] private float doubleJumpPower = 18f; 

    [Header("Physics Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    private Rigidbody2D body;
    private Animator anim;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;

    private float wallJumpCooldown;
    private float horizontalInput;
    private float rawHorizontalInput;
    private float currentVelocityX; 
    private Vector3 defaultWorldScale;
    private float footstepTimer;
    
    // Game Feel Counters
    private float coyoteCounter;
    private float jumpBufferCounter;
    
    // Double Jump
    private bool canDoubleJump = false;
    private bool hasDoubleJumped = false;

    [HideInInspector] public bool lockMovement;

    #region Time Slow Compensation
    // ===========================================
    // TIME SLOW COMPENSATION
    // Slow-mo sırasında oyuncu normal hızda kalsın diye telafi sistemi.
    // Raw telafi = 1 / Time.timeScale (timeScale=0.3 iken ~3.33)
    // ===========================================
    
    /// <summary>
    /// Ham time compensation değeri (TimeSlowAbility'den alınır)
    /// </summary>
    private float RawTimeCompensation => TimeSlowAbility.Instance != null 
        ? TimeSlowAbility.Instance.PlayerTimeCompensation 
        : 1f;
    
    /// <summary>
    /// Hareket için yumuşatılmış telafi - sqrt ile daha dengeli his verir.
    /// Raw=3.33 iken bu ~1.82 olur.
    /// </summary>
    private float MovementTimeCompensation => Mathf.Sqrt(RawTimeCompensation);
    
    /// <summary>
    /// Zıplama için azaltılmış telafi - yerçekimi de yavaşladığı için tam telafi gerekmez.
    /// Formül: 1 + (raw - 1) * 0.1
    /// Raw=3.33 iken bu ~1.23 olur.
    /// </summary>
    private float JumpTimeCompensation => 1f + (RawTimeCompensation - 1f) * 0.1f;
    
    /// <summary>
    /// Time slow aktif mi kontrolü
    /// </summary>
    private bool IsTimeSlowActive => TimeSlowAbility.Instance != null && TimeSlowAbility.Instance.IsSlowMotionActive;
    #endregion

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        defaultWorldScale = transform.lossyScale;
        Instance = this;
        
        // Yere iniş efektini oluştur
        CreateLandingDustEffect();
        CreateRunningDustEffect();
        CreateJumpDustEffect();
    }

    private void Update()
    {
        // Puzzle aktifken tüm input'ları engelle
        if (GameManager.IsPuzzleActive || Time.timeScale == 0f)
        {
            horizontalInput = 0f;
            rawHorizontalInput = 0f;
            anim.SetBool("Run", false);
            // Velocity'yi de sıfırla ki havada kalmasın
            if (body != null && GameManager.IsPuzzleActive)
            {
                body.linearVelocity = Vector2.zero;
            }
            return;
        }
        
        if (lockMovement)
        {
            horizontalInput = 0f;
            rawHorizontalInput = 0f;
            anim.SetBool("Run", false);
            return;
        }

        // Skill input modundayken hareketi devre dışı bırak
        if (GuitarSkillSystem.Instance != null && GuitarSkillSystem.Instance.IsInSkillInput)
        {
            horizontalInput = 0f;
            rawHorizontalInput = 0f;
            anim.SetBool("Run", false);
            return;
        }

        // Input Okuma
        float aKey = Input.GetKey(KeyCode.A) ? -1f : 0f;
        float dKey = Input.GetKey(KeyCode.D) ? 1f : 0f;
        rawHorizontalInput = aKey + dKey;
        
        // Smooth Hareket
        horizontalInput = Mathf.SmoothDamp(horizontalInput, rawHorizontalInput, ref currentVelocityX, movementSmoothTime);

        // Yön Döndürme (Artık ApplyFacingScale içinde eşik kontrolü var ama yine de burada çağıralım)
        if (Mathf.Abs(rawHorizontalInput) > 0.01f)
        {
            ApplyFacingScale(rawHorizontalInput);
        }

        // Durum Kontrolleri
        bool grounded = IsGrounded();
        bool onMovingPlatform = IsOnMovingPlatform();
        bool isGroundedState = grounded || onMovingPlatform;
        bool touchingWall = OnWall();

        // Animasyon
        anim.SetBool("Run", Mathf.Abs(rawHorizontalInput) > 0.01f && isGroundedState);
        anim.SetBool("grounded", isGroundedState);
        
        // Koşarken toz efekti
        if (Mathf.Abs(rawHorizontalInput) > 0.01f && isGroundedState)
        {
            SpawnRunningDust();
        }

        // --- Coyote Time Logic ---
        if (isGroundedState)
        {
            coyoteCounter = coyoteTime;
            hasDoubleJumped = false; 
            canDoubleJump = true; // Yerde olunca double jump hakkı yenilenir (standart mekanik)
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        // --- Jump Buffer Logic ---
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // --- Zıplama İşlemleri ---
        // 1. Normal / Coyote Time Zıplama
        if (jumpBufferCounter > 0f && coyoteCounter > 0f && wallJumpCooldown > 0.2f)
        {
            PerformJump(jumpPower);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }
        // 2. Wall Jump
        else if (jumpBufferCounter > 0f && touchingWall && !isGroundedState) 
        {
            PerformWallJump();
            jumpBufferCounter = 0f;
        }
        // 3. Double Jump (Standart - her zaman kullanılabilir)
        else if (Input.GetKeyDown(KeyCode.Space) && !isGroundedState && canDoubleJump && !hasDoubleJumped && !touchingWall)
        {
            PerformDoubleJump();
        }

        // --- Variable Jump Height ---
        if (Input.GetKeyUp(KeyCode.Space) && body.linearVelocity.y > 0f)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * variableJumpMultiplier);
        }

        // --- Ayak Sesi ---
        HandleFootsteps(isGroundedState);

        // Time slow compensation updates
        wallJumpCooldown += Time.deltaTime * MovementTimeCompensation;
    }

    private void FixedUpdate()
    {
        // Puzzle aktifken hareket yapma
        if (GameManager.IsPuzzleActive || Time.timeScale == 0f)
        {
            // Gravity'yi de sıfırla ki düşmesin
            if (GameManager.IsPuzzleActive && body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.gravityScale = 0f;
            }
            return;
        }
        
        bool grounded = IsGrounded();
        bool touchingWall = OnWall();

        // Time slow compensation
        float compensatedSpeed = speed * MovementTimeCompensation;
        
        // Hareket Uygulama
        body.linearVelocity = new Vector2(horizontalInput * compensatedSpeed, body.linearVelocity.y);

        // Wall Slide Logic
        if (touchingWall && !grounded && rawHorizontalInput != 0) 
        {
            body.gravityScale = wallSlideGravity;
            if (body.linearVelocity.y < -2f)
                body.linearVelocity = new Vector2(body.linearVelocity.x, -2f);
        }
        else
        {
            body.gravityScale = groundGravity;
        }
    }

    private void PerformJump(float power)
    {
        float compensatedPower = power * JumpTimeCompensation;
        body.linearVelocity = new Vector2(body.linearVelocity.x, compensatedPower);
        anim.SetTrigger("jump");
        SpawnJumpDust(); // Zıplama efekti
        
        // Zıplama afterimage efekti
        if (spriteRenderer != null)
        {
            DashAfterimage.Instance?.SpawnSingleAfterimage(spriteRenderer);
        }
    }

    private void PerformDoubleJump()
    {
        float compensatedPower = doubleJumpPower * JumpTimeCompensation;
        body.linearVelocity = new Vector2(body.linearVelocity.x, compensatedPower * 1.3f);
        anim.SetTrigger("jump"); 
        hasDoubleJumped = true;
        canDoubleJump = false; 
        SpawnJumpDust(); // Double jump efekti
        
        // Double jump afterimage efekti (daha belirgin)
        if (spriteRenderer != null)
        {
            DashAfterimage.Instance?.SpawnSingleAfterimage(spriteRenderer);
            // Kısa gecikmeyle ikinci gölge
            StartCoroutine(SpawnDelayedAfterimage(0.05f));
        }
        Debug.Log("Double Jump kullanıldı!");
    }

    private void PerformWallJump()
    {
        float compensatedJumpPower = jumpPower * JumpTimeCompensation; 
        float compensatedWallPush = 6f * JumpTimeCompensation;         
        
        float direction = -Mathf.Sign(transform.localScale.x);
        
        body.linearVelocity = new Vector2(direction * compensatedWallPush, compensatedJumpPower);

        ApplyFacingScale(direction);
        wallJumpCooldown = 0;
        
        // Wall jump afterimage efekti
        if (spriteRenderer != null)
        {
            DashAfterimage.Instance?.SpawnSingleAfterimage(spriteRenderer);
            StartCoroutine(SpawnDelayedAfterimage(0.05f));
        }
        
        // Wall jump sonrası double jump hakkı yenilenir (standart mekanik)
        canDoubleJump = true;
        hasDoubleJumped = false;
        coyoteCounter = 0f; 
    }
    
    /// <summary>
    /// Gecikmeli afterimage spawn (double jump ve wall jump için)
    /// </summary>
    private System.Collections.IEnumerator SpawnDelayedAfterimage(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (spriteRenderer != null)
        {
            DashAfterimage.Instance?.SpawnSingleAfterimage(spriteRenderer);
        }
    }

    private void HandleFootsteps(bool isWalking)
    {
        if (isWalking && Mathf.Abs(horizontalInput) > 0.01f)
        {
            footstepTimer -= Time.deltaTime * MovementTimeCompensation;

            if (footstepTimer <= 0)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.PlayFootstepSound();
                }
                footstepTimer = footstepInterval;
            }
        }
        else
        {
            footstepTimer = 0;
        }
    }

    private void ApplyFacingScale(float direction)
    {
        Vector3 parentScale = transform.parent ? transform.parent.lossyScale : Vector3.one;
        float safeX = Mathf.Abs(parentScale.x) < 0.0001f ? 1f : parentScale.x;
        float safeY = Mathf.Abs(parentScale.y) < 0.0001f ? 1f : parentScale.y;
        float safeZ = Mathf.Abs(parentScale.z) < 0.0001f ? 1f : parentScale.z;

        float targetX = Mathf.Abs(defaultWorldScale.x) * Mathf.Sign(direction == 0 ? 1f : direction);

        transform.localScale = new Vector3(
            targetX / safeX,
            defaultWorldScale.y / safeY,
            defaultWorldScale.z / safeZ
        );
    }

    private bool IsGrounded()
    {
        Vector2 boxSize = new Vector2(
            boxCollider.bounds.size.x * 0.9f,
            boxCollider.bounds.size.y
        );

        RaycastHit2D hit = Physics2D.BoxCast(
            boxCollider.bounds.center,
            boxSize,
            0,
            Vector2.down,
            0.1f,
            groundLayer
        );

        bool grounded = hit.collider != null;

        if (grounded && !wasGrounded)
        {
            SpawnGroundParticle(hit.point, hit.normal);
        }

        wasGrounded = grounded;
        return grounded;
    }

    private bool OnWall()
    {
        RaycastHit2D hit = Physics2D.BoxCast(
            boxCollider.bounds.center,
            boxCollider.bounds.size,
            0,
            new Vector2(transform.localScale.x, 0),
            0.1f,
            wallLayer
        );
        return hit.collider != null;
    }

    private bool IsOnMovingPlatform()
    {
        if (transform.parent == null) return false;
        return transform.parent.GetComponent<MovingPlatform>() != null;
    }

    public bool canAttack()
    {
        return true;
    }

    private void SpawnGroundParticle(Vector2 position, Vector2 normal)
    {
        // Önce kod ile oluşturulan efekti dene
        if (landingDustEffect != null)
        {
            landingDustEffect.transform.position = position;
            landingDustEffect.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
            landingDustEffect.Emit(12); // Toz parçacıkları
            return;
        }
        
        // Fallback: prefab varsa kullan
        if (groundHitParticle == null) return;

        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);

        ParticleSystem ps = Instantiate(
            groundHitParticle,
            position,
            rotation
        );

        ps.Play();
        Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
    }
    
    private void CreateLandingDustEffect()
    {
        GameObject dustObj = new GameObject("LandingDustEffect");
        dustObj.transform.SetParent(transform);
        dustObj.transform.localPosition = Vector3.zero;
        
        landingDustEffect = dustObj.AddComponent<ParticleSystem>();
        
        var main = landingDustEffect.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.3f;
        main.startColor = new Color(0.7f, 0.65f, 0.5f, 0.8f); // Toprak/toz rengi
        main.playOnAwake = false;
        
        // Emisyon - sadece burst ile
        var emission = landingDustEffect.emission;
        emission.rateOverTime = 0;
        
        // Şekil - yanlara doğru yayılsın
        var shape = landingDustEffect.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 70f;
        shape.radius = 0.15f;
        shape.rotation = new Vector3(-90f, 0f, 0f); // Yukarı/yanlara bak
        
        // Boyut azalması
        var sizeOverLifetime = landingDustEffect.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.2f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Renk solması
        var colorOverLifetime = landingDustEffect.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.75f, 0.7f, 0.55f), 0f),
                new GradientColorKey(new Color(0.6f, 0.55f, 0.45f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.7f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        // Velocity over lifetime - yavaşlama
        var velocityOverLifetime = landingDustEffect.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.speedModifier = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
        
        // Renderer - yuvarlak parçacık
        var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 5;
        
        // Yuvarlak texture oluştur
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - Mathf.Clamp01(dist / radius);
                alpha = alpha * alpha;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        mat.mainTexture = tex;
        renderer.material = mat;
    }
    
    private void CreateRunningDustEffect()
    {
        GameObject dustObj = new GameObject("RunningDustEffect");
        dustObj.transform.SetParent(transform);
        dustObj.transform.localPosition = new Vector3(0f, -0.5f, 0f); // Ayak hizasında
        
        runningDustEffect = dustObj.AddComponent<ParticleSystem>();
        
        var main = runningDustEffect.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.1f;
        main.startColor = new Color(0.65f, 0.6f, 0.5f, 0.5f); // Açık toz rengi
        main.playOnAwake = false;
        
        // Emisyon
        var emission = runningDustEffect.emission;
        emission.rateOverTime = 0;
        
        // Şekil - arkaya doğru
        var shape = runningDustEffect.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.05f;
        shape.rotation = new Vector3(90f, 0f, 0f); // Arkaya bak
        
        // Boyut azalması
        var sizeOverLifetime = runningDustEffect.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.8f);
        sizeCurve.AddKey(0.3f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Renk solması
        var colorOverLifetime = runningDustEffect.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.7f, 0.65f, 0.55f), 0f),
                new GradientColorKey(new Color(0.6f, 0.55f, 0.45f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.4f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        // Renderer
        var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 4;
        
        // Yuvarlak texture
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - Mathf.Clamp01(dist / radius);
                alpha = alpha * alpha;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        mat.mainTexture = tex;
        renderer.material = mat;
    }
    
    private void SpawnRunningDust()
    {
        if (runningDustEffect == null) return;
        
        runDustTimer += Time.deltaTime;
        if (runDustTimer >= runDustInterval)
        {
            runDustTimer = 0f;
            
            // Koşma yönüne göre arkaya doğru toz çıksın
            var shape = runningDustEffect.shape;
            float direction = Mathf.Sign(transform.localScale.x);
            shape.rotation = new Vector3(90f, direction > 0 ? 180f : 0f, 0f);
            
            runningDustEffect.transform.position = transform.position + new Vector3(-direction * 0.2f, -0.4f, 0f);
            runningDustEffect.Emit(2);
        }
    }
    
    private void CreateJumpDustEffect()
    {
        GameObject dustObj = new GameObject("JumpDustEffect");
        dustObj.transform.SetParent(transform);
        dustObj.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        
        jumpDustEffect = dustObj.AddComponent<ParticleSystem>();
        
        var main = jumpDustEffect.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.15f);
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.5f;
        main.startColor = new Color(0.8f, 0.75f, 0.6f, 0.6f);
        main.playOnAwake = false;
        
        var emission = jumpDustEffect.emission;
        emission.rateOverTime = 0;
        
        // Şekil - daire şeklinde yanlara
        var shape = jumpDustEffect.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f;
        shape.arc = 180f;
        shape.rotation = new Vector3(90f, 0f, 0f);
        
        // Boyut
        var sizeOverLifetime = jumpDustEffect.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.6f);
        sizeCurve.AddKey(0.2f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Renk
        var colorOverLifetime = jumpDustEffect.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.85f, 0.8f, 0.65f), 0f),
                new GradientColorKey(new Color(0.7f, 0.65f, 0.5f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.5f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        // Renderer
        var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 4;
        
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - Mathf.Clamp01(dist / radius);
                alpha = alpha * alpha;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        mat.mainTexture = tex;
        renderer.material = mat;
    }
    
    private void SpawnJumpDust()
    {
        if (jumpDustEffect == null) return;
        
        jumpDustEffect.transform.position = transform.position + new Vector3(0f, -0.4f, 0f);
        jumpDustEffect.Emit(8);
    }

}