using UnityEngine;

public class Portal : MonoBehaviour, IInteraction
{
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Transform _portalTransform;

    private void Update()
    {
        LookCamera();
    }

    private void LookCamera()
    {
        Vector3 camPos = _mainCamera.transform.position;
        Vector3 portalPos = _portalTransform.position;
        Vector3 dir = portalPos - camPos;
        dir.y = 0f;

        _portalTransform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    public void Interaction()
    {
        
    }
}
