using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Follow Player")]
    [SerializeField] private Transform player;
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float catchUpSpeed = 15f;

    [Header("Y Offset")]
    [SerializeField] private float yOffset = 3f;

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
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogWarning("CameraController: Player bulunamadı!");
        }
    }

    private void LateUpdate()
    {
        if (player == null || isExitingLevel) return;

        ApplyZoom();

        if (useRoomCamera)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                new Vector3(
                    currentPosX,
                    player.position.y + yOffset,
                    transform.position.z
                ),
                ref velocity,
                roomTransitionSpeed
            );
        }
        else
        {
            float facing = Mathf.Sign(player.lossyScale.x);
            if (Mathf.Approximately(facing, 0f)) facing = 1f;

            lookAhead = Mathf.Lerp(
                lookAhead,
                lookAheadDistance * facing,
                Time.deltaTime * followSpeed
            );

            Vector3 targetPos = new Vector3(
                player.position.x + lookAhead,
                player.position.y + yOffset,
                transform.position.z
            );

            float distanceToPlayer = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.y),
                new Vector2(player.position.x, player.position.y + yOffset)
            );

            float dynamicSpeed = followSpeed;
            if (distanceToPlayer > 2f)
            {
                dynamicSpeed = Mathf.Lerp(
                    followSpeed,
                    catchUpSpeed,
                    (distanceToPlayer - 2f) / 5f
                );
            }

            transform.position = Vector3.Lerp(
                transform.position,
                targetPos,
                Time.deltaTime * dynamicSpeed
            );
        }
    }

    private void ApplyZoom()
    {
        if (!overrideOrthographicSize || cam == null || !cam.orthographic) return;

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

        float duration = 0.4f;
        float startSize = cam.orthographicSize;
        float targetSize = startSize * 1.3f;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);

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
