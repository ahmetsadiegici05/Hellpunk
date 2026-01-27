using UnityEngine;

public class UltimateAbility : MonoBehaviour
{
    public static UltimateAbility Instance { get; private set; }

    public bool IsUltimateActive { get; private set; }
    public float ActiveTimeRemaining { get; private set; }
    public float ActiveDuration { get; private set; } = 2f; // Kaynaktaki UseUlti süresi 2sn [4]

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void Activate()
    {
        StartCoroutine(UltimateRoutine());
    }

    private System.Collections.IEnumerator UltimateRoutine()
    {
        IsUltimateActive = true;
        ActiveTimeRemaining = ActiveDuration;

        // GuitarSkillSystem'deki UseUlti mantığını buraya taşıyabilir veya orayı çağırabilirsiniz [4]
        if (GameManager.Instance != null && GameManager.Instance.ultiObject != null)
        {
            GameManager.Instance.ultiObject.SetActive(true);
            GameManager.Instance.ultiAnimator.SetTrigger("Ulti");
        }

        while (ActiveTimeRemaining > 0)
        {
            ActiveTimeRemaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (GameManager.Instance != null && GameManager.Instance.ultiObject != null)
            GameManager.Instance.ultiObject.SetActive(false);

        IsUltimateActive = false;
    }
}