using Player;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerCinemachineController : MonoBehaviour
{
    private CinemachineCamera _cineCam;
    private CinemachineInputAxisController _inputAxisController;

    public float FOV => _cineCam.Lens.FieldOfView;

    private InputModeManager _inputModeManager;

    private PlayerCore _player;

    private void Awake()
    {
        TryGetComponent(out _cineCam);
        TryGetComponent(out _inputAxisController);
    }

    private void OnEnable()
    {
        BindManagerEvents(_inputModeManager);
    }

    private void OnDisable()
    {
        UnbindManagerEvents(_inputModeManager);
    }

    public void InitializePlayerCinemachineController(PlayerCore player, InputModeManager inputModeManager)
    {
        _player = player;
        _inputModeManager = inputModeManager;
        BindManagerEvents(_inputModeManager);
        TrySetTarget(_player.CameraPivot);
    }

    private bool TrySetTarget(Transform target)
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

    private void BindManagerEvents(InputModeManager inputModeManager)
    {
        if (inputModeManager != null)
        {
            inputModeManager.OnChangeInputState -= BlockCameraRotate;
            inputModeManager.OnChangeInputState += BlockCameraRotate;
        }
    }

    private void UnbindManagerEvents(InputModeManager inputModeManager)
    {
        if (inputModeManager != null)
        {
            inputModeManager.OnChangeInputState -= BlockCameraRotate;
        }
    }

    private void BlockCameraRotate(InputState state)
    {
        if(state == InputState.Combat)
        {
            _inputAxisController.enabled = true;
        }
        else if(state == InputState.UI)
        {
            _inputAxisController.enabled = false;
        }
        else if(state == InputState.NodeMap)
        {
            _inputAxisController.enabled = false;
        }
    }
}
