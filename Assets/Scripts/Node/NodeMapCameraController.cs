using Unity.Cinemachine;
using UnityEngine;

public class NodeMapCameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _cinemachineCamera;

    private Vector3 _originPos;

    private void Awake()
    {
        _originPos = transform.position;
        SetActiveCinemachine(false);
    }

    private void OnEnable()
    {
        transform.position = _originPos;
    }

    private void OnDisable()
    {
        transform.position = _originPos;
    }

    private void Update()
    {
        MoveCamera();
    }

    public void SetActiveCinemachine(bool active)
    {
        if(_cinemachineCamera == null)
        {
            Debug.LogError("시네머신이 없습니다");
            return;
        }

        _cinemachineCamera.enabled = active;
    }

    public void SetFOV(float fov)
    {
        if(_cinemachineCamera == null)
        {
            Debug.LogError("시네머신이 없습니다");
            return;
        }

        _cinemachineCamera.Lens.FieldOfView = fov;
    }

    public float GetFOV()
    {
        if (_cinemachineCamera == null)
        {
            Debug.LogError("시네머신이 없습니다");
            return 0f;
        }

        return _cinemachineCamera.Lens.FieldOfView;
    }

    private void MoveCamera()
    {
        if (GameManager.Instance == null && GameManager.Instance.CurrentState != GameState.NodeMap)
            return;

        float wheelInput = Input.GetAxis("Mouse ScrollWheel");
        if(wheelInput > 0)
        {

        }
        else if(wheelInput < 0)
        {

        }
    }
}
