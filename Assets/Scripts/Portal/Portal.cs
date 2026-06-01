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
        if(GameManager.Instance == null)
        {
            Debug.LogError("게임 매니저가 없습니다");
            return;
        }

        GameManager.Instance.CurrentState = GameState.NodeMap;
    }
}
