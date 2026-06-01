using Unity.Cinemachine;
using UnityEngine;

public class PlayerCinemachineController : MonoBehaviour
{
    private CinemachineCamera _cineCam;
    private CinemachineInputAxisController _inputAxisController;

    public float FOV => _cineCam.Lens.FieldOfView;

    private void Awake()
    {
        TryGetComponent(out _cineCam);
        TryGetComponent(out _inputAxisController);
    }

    private void OnEnable()
    {
        if(GameManager.Instance != null)
        {
            GameManager.Instance.OnChangeGameState += StopCameraRotate;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnChangeGameState -= StopCameraRotate;
        }
    }

    public bool TrySetTarget(Transform target)
    {
        if (!DebugUtil.IsExistComponent(_cineCam) || !DebugUtil.IsExistComponent(target))
            return false;

        _cineCam.Target.TrackingTarget = target;
        return true;
    }

    private void StopCameraRotate(GameState prev, GameState curr)
    {
        switch (curr)
        {
            case GameState.UI:
                _inputAxisController.enabled = false;
                break;
            case GameState.Game:
                _inputAxisController.enabled = true;
                break;
            case GameState.NodeMap:
                _inputAxisController.enabled = false;
                break;
            default:
                _inputAxisController.enabled = true;
                break;
        }
    }

    public void SetFOV(float fov)
    {
        _cineCam.Lens.FieldOfView = fov;
    }

    public void SetActiveCinemachine(bool active)
    {
        _cineCam.enabled = active;
    }
}
