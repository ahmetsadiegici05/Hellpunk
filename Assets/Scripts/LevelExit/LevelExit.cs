using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelExit : MonoBehaviour
{
    [SerializeField] private string nextSceneName;
    [SerializeField] private float loadDelay = 1f;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hasTriggered && collision.CompareTag("Player"))
        {
            hasTriggered = true;
            // Yeni geçiş kontrolcüsünü kullan
            if (SceneTransitionController.Instance != null)
            {
                SceneTransitionController.Instance.LoadSceneWithTransition(nextSceneName);
            }
            else
            {
                // Fallback
                Invoke(nameof(LoadNextLevel), loadDelay);
            }
        }
    }

    private void LoadNextLevel()
    {
        Time.timeScale = 1f;

        // Yeni bölüme geçildiğinde checkpoint sıfırlansın ki yanlış yerde doğmayalım
        CheckpointData.HasCheckpoint = false;

        SceneManager.LoadScene(nextSceneName);
    }
}