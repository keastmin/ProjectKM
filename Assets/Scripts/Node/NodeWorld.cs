using Unity.Cinemachine;
using UnityEngine;

public class NodeWorld : MonoBehaviour
{
    [SerializeField] private NodeMapCameraController _nodeMapCameraController;

    public void SetCamActive(bool active)
    {
        if (_nodeMapCameraController == null)
        {
            Debug.LogError("노드 맵 카메라 컨트롤러가 없습니다");
            return;
        }

        _nodeMapCameraController.SetActiveCinemachine(active);
    }

    public void SetFOV(float fov)
    {
        if (_nodeMapCameraController == null)
        {
            Debug.LogError("노드 맵 카메라 컨트롤러가 없습니다");
            return;
        }

        _nodeMapCameraController.SetFOV(fov);
    }

    public float GetFOV()
    {
        if (_nodeMapCameraController == null)
        {
            Debug.LogError("노드 맵 카메라 컨트롤러가 없습니다");
            return 0f;
        }

        return _nodeMapCameraController.GetFOV();
    }
}