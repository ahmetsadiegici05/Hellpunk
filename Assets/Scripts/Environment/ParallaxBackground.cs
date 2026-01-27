using UnityEngine;

/// <summary>
/// 2D Platformer için optimize edilmiş X + Y Parallax arka plan sistemi
/// Sonsuz yatay ve dikey tekrar destekler
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [Header("Parallax Ayarları")]
    [Range(0f, 1f)] public float parallaxEffectX = 0.5f;
    [Range(0f, 1f)] public float parallaxEffectY = 0.2f;

    [Header("Ölçekleme")]
    public bool autoScaleToCamera = true;
    public float manualScale = 5f;
    public float extraScale = 1.2f;

    private Transform cameraTransform;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;

    private float textureUnitSizeX;
    private float textureUnitSizeY;
    private Vector3 lastCameraPosition;
    private float appliedScale;

    void Start()
    {
        mainCamera = Camera.main;
        cameraTransform = mainCamera.transform;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogError("ParallaxBackground: SpriteRenderer veya Sprite yok!");
            enabled = false;
            return;
        }

        CalculateAndApplyScale();

        transform.position = new Vector3(
            cameraTransform.position.x,
            cameraTransform.position.y,
            transform.position.z
        );

        lastCameraPosition = cameraTransform.position;

        CreateTiles2D();
    }

    void CalculateAndApplyScale()
    {
        Sprite sprite = spriteRenderer.sprite;
        float spriteHeight = sprite.bounds.size.y;
        float spriteWidth = sprite.bounds.size.x;

        if (autoScaleToCamera)
        {
            float cameraHeight = mainCamera.orthographicSize * 2f;
            appliedScale = (cameraHeight / spriteHeight) * extraScale;
        }
        else
        {
            appliedScale = manualScale;
        }

        transform.localScale = new Vector3(appliedScale, appliedScale, 1f);

        textureUnitSizeX = spriteWidth * appliedScale;
        textureUnitSizeY = spriteHeight * appliedScale;
    }

    void CreateTiles2D()
    {
        // 3x3 grid (-1,0,1) x (-1,0,1)
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0) continue;

                GameObject tile = new GameObject($"Tile_{x}_{y}");
                tile.transform.SetParent(transform);
                tile.transform.localPosition = new Vector3(
                    x * textureUnitSizeX / appliedScale,
                    y * textureUnitSizeY / appliedScale,
                    0
                );
                tile.transform.localScale = Vector3.one;

                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = spriteRenderer.sprite;
                sr.sortingLayerID = spriteRenderer.sortingLayerID;
                sr.sortingOrder = spriteRenderer.sortingOrder;
                sr.color = spriteRenderer.color;
            }
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;

        transform.position += new Vector3(
            deltaMovement.x * parallaxEffectX,
            deltaMovement.y * parallaxEffectY,
            0
        );

        lastCameraPosition = cameraTransform.position;

        float distX = cameraTransform.position.x - transform.position.x;
        if (Mathf.Abs(distX) >= textureUnitSizeX)
        {
            float offsetX = distX > 0 ? textureUnitSizeX : -textureUnitSizeX;
            transform.position += new Vector3(offsetX, 0, 0);
        }

        float distY = cameraTransform.position.y - transform.position.y;
        if (Mathf.Abs(distY) >= textureUnitSizeY)
        {
            float offsetY = distY > 0 ? textureUnitSizeY : -textureUnitSizeY;
            transform.position += new Vector3(0, offsetY, 0);
        }
    }
}
