using UnityEngine;

public class BasecampElevator : MonoBehaviour
{
    [SerializeField] private ElevatorDoorOpener _elevatorDoorOpener;
    [SerializeField] private ElevatorDetector _elevatorDetector;

    private bool _isInPlayerElevator;

    public bool IsInPlayerElevator => _isInPlayerElevator;

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

    private void Initialize()
    {
        if(!TryGetComponent(out _elevatorDoorOpener))
        {
            Debug.LogError("엘레베이터 문 오프너가 없음");
            return;
        }
        _isInPlayerElevator = false;
    }

    private void BindDetectEvent()
    {
        if(_elevatorDetector == null)
        {
            Debug.LogError("엘레베이터 디텍터가 없음");
            return;
        }

        _elevatorDetector.OnDetectPlayerEnter -= SetPlayerInElevator;
        _elevatorDetector.OnDetectPlayerEnter += SetPlayerInElevator;
    }

    private void UnbindDetectEvent()
    {
        if (_elevatorDetector == null)
        {
            Debug.LogError("엘레베이터 디텍터가 없음");
            return;
        }

        _elevatorDetector.OnDetectPlayerEnter -= SetPlayerInElevator;
    }

    private void SetPlayerInElevator()
    {
        _elevatorDoorOpener.BlockElevatorDoorOpen(true);
        _isInPlayerElevator = true;
    }
}
