using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Follow Player")]
    [SerializeField] private Transform player;
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float catchUpSpeed = 15f; // Oyuncu uzaklaştığında daha hızlı yakala

    [Header("Zoom")]
    [SerializeField] private bool overrideOrthographicSize = true;
    [SerializeField] private float targetOrthographicSize = 7f;
    [SerializeField] private float zoomSmoothTime = 0.1f;
    
    [Header("Room Camera (Optional)")]
    [SerializeField] private bool useRoomCamera = false;
    [SerializeField] private float roomTransitionSpeed = 3f;
    
    private float currentPosX;
    private float lookAhead;
    private Vector3 velocity = Vector3.zero;

    private Camera cam;
    private float zoomVelocity;
    private bool isExitingLevel = false;

    private void Awake()
    {
        cam = GetComponent<Camera>();

        if (player == null)
        {
            // Otomatik olarak "Player" tag'li objeyi bul
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogWarning("CameraController: Player referansı atanmadı ve 'Player' tag'li obje bulunamadı!");
        }
    }

    private void LateUpdate()
    {
        if (player == null || isExitingLevel) return;

        ApplyZoom();

        if (useRoomCamera)
        {
            // Room camera modu
            transform.position = Vector3.SmoothDamp(
                transform.position, 
                new Vector3(currentPosX, transform.position.y, transform.position.z), 
                ref velocity, 
                roomTransitionSpeed
            );
        }
        else
        {
            // Follow player modu (daha hızlı ve smooth)
            float facing = Mathf.Sign(player.lossyScale.x);
            if (Mathf.Approximately(facing, 0f)) facing = 1f;
            lookAhead = Mathf.Lerp(lookAhead, lookAheadDistance * facing, Time.deltaTime * followSpeed);
            
            Vector3 targetPos = new Vector3(
                player.position.x + lookAhead, 
                player.position.y, 
                transform.position.z
            );
            
            // Oyuncu ile kamera arasındaki mesafeyi hesapla
            float distanceToPlayer = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.y),
                new Vector2(player.position.x, player.position.y)
            );
            
            // Mesafe arttıkça hızı artır (catch-up mekanizması)
            float dynamicSpeed = followSpeed;
            if (distanceToPlayer > 2f)
            {
                dynamicSpeed = Mathf.Lerp(followSpeed, catchUpSpeed, (distanceToPlayer - 2f) / 5f);
            }
            
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * dynamicSpeed);
        }
    }

    private void ApplyZoom()
    {
        if (!overrideOrthographicSize || cam == null) return;
        if (!cam.orthographic) return;

        if (zoomSmoothTime <= 0f)
        {
            cam.orthographicSize = targetOrthographicSize;
            return;
        }

        cam.orthographicSize = Mathf.SmoothDamp(
            cam.orthographicSize,
            targetOrthographicSize,
            ref zoomVelocity,
            zoomSmoothTime
        );
    }

    public void SetZoom(float orthographicSize)
    {
        targetOrthographicSize = orthographicSize;
    }

    public System.Collections.IEnumerator PlayLevelExitAnimation(System.Action onComplete)
    {
        isExitingLevel = true;
        
        // Hızlı ve etkileyici geçiş - karakteri zoomlamak yerine hızlıca fade'e geç
        float duration = 0.4f; // Çok kısa, fade out ile birleşecek
        float startSize = cam.orthographicSize;
        float targetSize = startSize * 1.3f; // Hafif zoom out (zoomlama yerine)
        
        float time = 0f;
        
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            // Ease out cubic - başta hızlı, sonda yavaş
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);
            
            // Hafif zoom out efekti
            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, smoothT);
            
            yield return null;
        }
        
        onComplete?.Invoke();
    }

    public void MoveToNewRoom(Transform _newRoom)
    {
        currentPosX = _newRoom.position.x;
    }
}