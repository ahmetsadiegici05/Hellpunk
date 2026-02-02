using UnityEngine;

/// <summary>
/// FireWaterfall için hasar zone'u - trigger olarak çalışır, fiziksel çarpışma yapmaz
/// </summary>
public class FireWaterfallDamageZone : MonoBehaviour
{
    private FireWaterfall parentWaterfall;
    private Collider2D myCollider;
    
    public void Initialize(FireWaterfall waterfall)
    {
        parentWaterfall = waterfall;
        myCollider = GetComponent<Collider2D>();
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Player ile fiziksel çarpışmayı engelle
        if (other.CompareTag("Player") && myCollider != null)
        {
            Physics2D.IgnoreCollision(myCollider, other, true);
        }
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        if (parentWaterfall != null)
        {
            parentWaterfall.ApplyDamageToPlayer(other);
        }
    }
}
