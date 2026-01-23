using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Follow Player")]
    [SerializeField] private Transform player;
    [SerializeField] private float followSpeed = 2f;
    [SerializeField] private float lookAheadDistance = 2f;

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
            
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
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
        
        float duration = 1.5f;
        float targetSize = 2f; 
        float rotationAmount = 360f * 2f; 
        
        float startSize = cam.orthographicSize;
        Vector3 startPos = transform.position;
        
        float time = 0f;
        
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float smoothT = t * t * t; 
            
            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, smoothT);
            
            if (player != null)
            {
                Vector3 targetPos = new Vector3(player.position.x, player.position.y, -10f);
                transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            }
            
            float currentRot = Mathf.Lerp(0, rotationAmount, smoothT);
            transform.rotation = Quaternion.Euler(0, 0, currentRot);
            
            yield return null;
        }
        
        onComplete?.Invoke();
    }

    public void MoveToNewRoom(Transform _newRoom)
    {
        currentPosX = _newRoom.position.x;
    }
}