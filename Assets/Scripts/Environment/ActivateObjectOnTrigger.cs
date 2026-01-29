using UnityEngine;
using System.Collections;

public class ActivateObjectOnTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private string triggerTag = "Player";

    [Header("Target Object")]
    [SerializeField] private GameObject targetObject;

    [Header("Options")]
    [SerializeField] private bool activateOnlyOnce = true;
    [SerializeField] private float activateDelay = 0.5f;

    private bool activated = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activateOnlyOnce && activated)
            return;

        if (other.CompareTag(triggerTag))
        {
            activated = true;
            StartCoroutine(ActivateAfterDelay());
        }
    }

    private IEnumerator ActivateAfterDelay()
    {
        yield return new WaitForSeconds(activateDelay);

        if (targetObject != null)
            targetObject.SetActive(true);
    }
}
