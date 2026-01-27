using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Shop"))
        {
            UIManager.Instance.OpenShopPanel();
        }
    }
}