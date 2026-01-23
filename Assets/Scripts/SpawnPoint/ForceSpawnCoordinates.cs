using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ForceSpawnCoordinates : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnX;
    [SerializeField] private float spawnY;

    private static string lastSceneName;

    private void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (lastSceneName == currentScene)
        {
            if (CheckpointData.HasCheckpoint)
            {
                return;
            }
        }

        lastSceneName = currentScene;

        StartCoroutine(SetPositionRoutine());
    }

    private IEnumerator SetPositionRoutine()
    {
        yield return null;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(spawnX, spawnY, 0);

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector2.zero;
#else
                rb.velocity = Vector2.zero;
#endif
            }
        }

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            float currentZ = mainCam.transform.position.z;
            mainCam.transform.position = new Vector3(spawnX, spawnY, currentZ);
        }
        else
        {
            Camera cam = FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                cam.transform.position = new Vector3(spawnX, spawnY, cam.transform.position.z);
            }
        }
    }
}