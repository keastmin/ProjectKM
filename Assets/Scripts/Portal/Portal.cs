using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private string _nextSceneName;
    [SerializeField] private Camera _mainCamera;

    private void OnTriggerEnter(Collider other)
    {
        LoadingController.LoadScene(_nextSceneName);
    }

    private void Update()
    {
        LookCamera();
    }

    private void LookCamera()
    {
        Vector3 camPos = _mainCamera.transform.position;
        Vector3 portalPos = transform.position;
        Vector3 dir = portalPos - camPos;
        dir.y = 0f;

        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }
}
