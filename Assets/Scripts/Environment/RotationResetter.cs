using UnityEngine;

public class RotationResetter : MonoBehaviour
{
    [SerializeField] private Transform rotationRoot;

    private void Awake()
    {
        ApplySavedRotation();
    }

    public void ApplySavedRotation()
    {
        if (GameManager.Instance == null || rotationRoot == null)
            return;

        float z = GameManager.Instance.lastTransformRotationValue;
        rotationRoot.rotation = Quaternion.Euler(0f, 0f, z);
    }
}
