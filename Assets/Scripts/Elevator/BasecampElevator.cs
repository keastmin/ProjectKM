using Player;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasecampElevator : MonoBehaviour
{
    [SerializeField] private string _nodeMapSceneName = "NodeMapScene";
    [SerializeField] private string _basecampSceneName = "BasecampScene";
    [SerializeField] private ElevatorDoorOpener _elevatorDoorOpener;
    [SerializeField] private ElevatorDetector _playerInElevatorDetector;

    public event Action OnPlayerInElevator;

    private PlayerCore _playerCore;
    private PlayerCinemachineController _playerCinemachineController;
    private SceneFlowManager _sceneFlowManager;

    private void OnEnable()
    {
        BindDetectEvent();
    }

    private void OnDisable()
    {
        UnbindDetectEvent();
    }

    public void InitializeElevator(PlayerCore player, GameRunContext context)
    {
        _playerCore = player;
        _playerCinemachineController = context.PlayerCinemachineController;
        _sceneFlowManager = context.SceneFlowManager;

        BindDetectEvent();
    }

    private void BindDetectEvent()
    {
        if(_playerInElevatorDetector == null)
        {
            Debug.LogError("ElevatorDetector가 없음");
            return;
        }
        if (_sceneFlowManager == null)
            return;

        _playerInElevatorDetector.OnDetectPlayerEnter -= SetPlayerInElevator;
        _playerInElevatorDetector.OnDetectPlayerEnter += SetPlayerInElevator;

        _sceneFlowManager.OnSwitchSceneComplete -= SwitchToNodeMap;
        _sceneFlowManager.OnSwitchSceneComplete += SwitchToNodeMap;
    }

    private void UnbindDetectEvent()
    {
        if (_playerInElevatorDetector == null)
        {
            Debug.LogError("ElevatorDetector가 없음");
            return;
        }
        if (_sceneFlowManager == null)
            return;

        _playerInElevatorDetector.OnDetectPlayerEnter -= SetPlayerInElevator;

        _sceneFlowManager.OnSwitchSceneComplete -= SwitchToNodeMap;
    }

    private void SetPlayerInElevator()
    {
        BlockElevatorDoor(true);

        List<GameObject> objList = new List<GameObject>();
        objList.Add(_playerCore.gameObject);
        objList.Add(_playerCinemachineController.gameObject);
        objList.Add(this.gameObject);
        _sceneFlowManager.SwitchSceneWithMoveGameObjects(_nodeMapSceneName, _basecampSceneName, objList);
    }

    public void BlockElevatorDoor(bool block)
    {
        _elevatorDoorOpener.BlockElevatorDoorOpen(block);
    }

    private void SwitchToNodeMap()
    {
        // 엘레베이터에서 플레이어 감지 막기
        _playerInElevatorDetector.BlockDetect(true);

        // 문 열기 기다리기
        StartCoroutine(WaitOpenDoorRoutine(2f));
    }

    private IEnumerator WaitOpenDoorRoutine(float waitSeconds)
    {
        yield return new WaitForSeconds(waitSeconds);
        BlockElevatorDoor(false);
    }
}