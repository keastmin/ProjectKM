using Player;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerCinemachineController : MonoBehaviour
{
    private CinemachineCamera _cineCam;
    private CinemachineInputAxisController _inputAxisController;

    public float FOV => _cineCam.Lens.FieldOfView;

    private PlayerCore _player;

    private void Awake()
    {
        TryGetComponent(out _cineCam);
        TryGetComponent(out _inputAxisController);
    }

    public void InitializePlayerCinemachineController(PlayerCore player)
    {
        _player = player;
        TrySetTarget(_player.CameraPivot);
    }

    public bool TrySetTarget(Transform target)
    {
        if(_cineCam == null)
        {
            Debug.LogError("시네머신 카메라가 없음");
            return false;
        }
        if(target == null)
        {
            Debug.LogError("타겟이 없음");
            return false;
        }

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
