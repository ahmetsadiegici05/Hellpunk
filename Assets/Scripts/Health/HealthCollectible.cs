using UnityEngine;

public class HealthCollectible : MonoBehaviour
{
    [SerializeField] private float healthValue;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            collision.GetComponent<Health>().AddHealth(healthValue);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayHealSound();
            }

            gameObject.SetActive(false);
        }
    }
}