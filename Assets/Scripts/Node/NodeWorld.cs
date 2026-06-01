using Unity.Cinemachine;
using UnityEngine;

public class NodeWorld : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _nodeMapCinemachine;

    public float FOV => _nodeMapCinemachine.Lens.FieldOfView;

    private void Awake()
    {
        SetCamActive(false);
    }

    public void SetCamActive(bool active)
    {
        if (_nodeMapCinemachine == null)
        {
            Debug.LogError("노드 맵 시네머신이 없습니다");
            return;
        }

        _nodeMapCinemachine.enabled = active;
    }

    public void SetFOV(float fov)
    {
        _nodeMapCinemachine.Lens.FieldOfView = fov;
    }
}