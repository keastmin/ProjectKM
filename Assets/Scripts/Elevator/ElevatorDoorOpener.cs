using UnityEngine;

public class ElevatorDoorOpener : MonoBehaviour
{
    [SerializeField] private Transform _leftDoorTransform;
    [SerializeField] private Transform _rightDoorTransform;
    [SerializeField] private ElevatorDetector _elevatorDoorDetector;
    [SerializeField] private float _openDistance = 5f;
    [SerializeField] private float _openSpeed = 8f;

    private Vector3 _leftDoorOpenTargetPosition;
    private Vector3 _rightDoorOpenTargetPosition;
    private Vector3 _leftDoorCloseTargetPosition;
    private Vector3 _rightDoorCloseTargetPosition;

    private bool _isInitialized;
    private bool _doorFlag;
    private bool _isBlock;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        BindDetectEvent();
    }

    private void OnDisable()
    {
        UnbindDetectEvent();
    }

    private void Update()
    {
        if (!_isInitialized)
            return;

        ElevatorDoorMove(_doorFlag);
    }

    private void Initialize()
    {
        _isInitialized = false;

        _doorFlag = false;
        _isBlock = false;

        if (_leftDoorTransform == null || _rightDoorTransform == null)
        {
            Debug.LogError("문이 없음");
            return;
        }

        _leftDoorOpenTargetPosition = _leftDoorTransform.position - (_leftDoorTransform.right * _openDistance);
        _rightDoorOpenTargetPosition = _rightDoorTransform.position + (_rightDoorTransform.right * _openDistance);

        _leftDoorCloseTargetPosition = _leftDoorTransform.position;
        _rightDoorCloseTargetPosition = _rightDoorTransform.position;

        _isInitialized = true;
    }

    private void BindDetectEvent()
    {
        if(_elevatorDoorDetector == null)
        {
            Debug.LogError("ElevatorDetector가 없음");
            return;
        }

        _elevatorDoorDetector.OnDetectPlayerEnter -= DoorFlagSetTrue;
        _elevatorDoorDetector.OnDetectPlayerEnter += DoorFlagSetTrue;

        _elevatorDoorDetector.OnDetectPlayerExit -= DoorFlagSetFalse;
        _elevatorDoorDetector.OnDetectPlayerExit += DoorFlagSetFalse;
    }

    private void UnbindDetectEvent()
    {
        if (_elevatorDoorDetector == null)
        {
            Debug.LogError("ElevatorDetector가 없음");
            return;
        }

        _elevatorDoorDetector.OnDetectPlayerEnter -= DoorFlagSetTrue;

        _elevatorDoorDetector.OnDetectPlayerExit -= DoorFlagSetFalse;
    }

    private void DoorFlagSetTrue()
    {
        _doorFlag = true;
    }

    private void DoorFlagSetFalse()
    {
        _doorFlag = false;
    }

    private void ElevatorDoorMove(bool flag)
    {
        Vector3 leftDoorTarget = (flag ? _leftDoorOpenTargetPosition : _leftDoorCloseTargetPosition);
        Vector3 rightDoorTarget = (flag ? _rightDoorOpenTargetPosition : _rightDoorCloseTargetPosition);

        // 문을 여는 동작을 막으면 무조건 닫히게 하기
        if (_isBlock)
        {
            leftDoorTarget = _leftDoorCloseTargetPosition;
            rightDoorTarget = _rightDoorCloseTargetPosition;
        }

        Move(_leftDoorTransform, leftDoorTarget);
        Move(_rightDoorTransform, rightDoorTarget);
    }

    private void Move(Transform door, Vector3 target)
    {
        door.position = Vector3.Lerp(door.position, target, Time.deltaTime * _openSpeed);
    }

    // 문이 열리는 동작 막기
    public void BlockElevatorDoorOpen(bool block)
    {
        _isBlock = block;
    }
}