using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] public float speed = 10f;
    [SerializeField] public float jumpPower = 15f;
    [SerializeField] private float footstepInterval = 0.3f;
    [SerializeField] private float groundGravity = 7f;
    [SerializeField] private float wallSlideGravity = 1.5f;
    [SerializeField] private float movementSmoothTime = 0.05f;

    [Header("Game Feel Settings")]
    [SerializeField] private float coyoteTime = 0.15f; 
    [SerializeField] private float jumpBufferTime = 0.1f; 
    [SerializeField] private float variableJumpMultiplier = 0.5f; 

    [Header("Double Jump Settings")]
    [SerializeField] private float doubleJumpPower = 15f; 

    [Header("Physics Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    private Rigidbody2D body;
    private Animator anim;
    private BoxCollider2D boxCollider;

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
        defaultWorldScale = transform.lossyScale;
    }

    private void Update()
    {
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
    }

    private void PerformDoubleJump()
    {
        float compensatedPower = doubleJumpPower * JumpTimeCompensation;
        body.linearVelocity = new Vector2(body.linearVelocity.x, compensatedPower * 1.3f);
        anim.SetTrigger("jump"); 
        hasDoubleJumped = true;
        canDoubleJump = false; 
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
        
        // Wall jump sonrası double jump hakkı yenilenir (standart mekanik)
        canDoubleJump = true;
        hasDoubleJumped = false;
        coyoteCounter = 0f; 
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
        Vector2 boxSize = new Vector2(boxCollider.bounds.size.x * 0.9f, boxCollider.bounds.size.y);
        RaycastHit2D hit = Physics2D.BoxCast(
            boxCollider.bounds.center,
            boxSize,
            0,
            Vector2.down,
            0.1f,
            groundLayer
        );
        return hit.collider != null;
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
}