using System;
using UnityEngine;

public class BasecampElevator : MonoBehaviour
{
    [SerializeField] private ElevatorDoorOpener _elevatorDoorOpener;
    [SerializeField] private ElevatorDetector _playerInElevatorDetector;

    public event Action OnPlayerInElevator;

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
            Debug.LogError("ElevatorDoorOpener가 없음");
            return;
        }
    }

    private void BindDetectEvent()
    {
        if(_playerInElevatorDetector == null)
        {
            Debug.LogError("ElevatorDetector가 없음");
            return;
        }

        _playerInElevatorDetector.OnDetectPlayerEnter -= SetPlayerInElevator;
        _playerInElevatorDetector.OnDetectPlayerEnter += SetPlayerInElevator;
    }

    private void UnbindDetectEvent()
    {
        if (_playerInElevatorDetector == null)
        {
            Debug.LogError("ElevatorDetector가 없음");
            return;
        }

        _playerInElevatorDetector.OnDetectPlayerEnter -= SetPlayerInElevator;
    }

    private void SetPlayerInElevator()
    {
        OnPlayerInElevator?.Invoke();
    }

    public void BlockElevatorDoor(bool block)
    {
        _elevatorDoorOpener.BlockElevatorDoorOpen(block);
    }
}
