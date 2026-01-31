using UnityEngine;

public class HighlightArea : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;
            gameObject.SetActive(false);
        }
    }
}
