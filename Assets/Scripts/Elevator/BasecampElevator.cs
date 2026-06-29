using Player;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BasecampElevator : MonoBehaviour
{
    [SerializeField] private string _nodeMapSceneName = "NodeMapScene";
    [SerializeField] private string _basecampSceneName = "BasecampScene";
    [SerializeField] private ElevatorDoorOpener _elevatorDoorOpener;
    [SerializeField] private ElevatorDetector _playerInElevatorDetector;
    [SerializeField] private Camera _combatUICamera;
    [SerializeField] private CombatCanvas _combatCanvas;
    [SerializeField] private Camera _basecampUICamera;

    private PlayerCore _playerCore;
    private SceneFlowManager _sceneFlowManager;
    private Camera _mainCamera;

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
        Debug.Log("엘레베이터 초기화");

        _playerCore = player;
        _sceneFlowManager = context.SceneFlowManager;
        _mainCamera = context.MainCamera;

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
        Debug.Log("엘리베이터 들어감");

        var mainCameraData = _mainCamera.GetUniversalAdditionalCameraData();
        mainCameraData.cameraStack.Remove(_basecampUICamera);

        List<GameObject> objList = new List<GameObject>();
        objList.Add(_playerCore.gameObject);
        objList.Add(this.gameObject);
        objList.Add(_combatUICamera.gameObject);
        objList.Add(_combatCanvas.gameObject);
        _sceneFlowManager.SwitchSceneWithMoveGameObjects(_nodeMapSceneName, _basecampSceneName, objList);
    }

    public void BlockElevatorDoor(bool block)
    {
        _elevatorDoorOpener.BlockElevatorDoorOpen(block);
    }

    private void SwitchToNodeMap(string unloadSceneName, string loadedSceneName)
    {
        if (unloadSceneName == _basecampSceneName && loadedSceneName == _nodeMapSceneName)
        {
            // 엘레베이터에서 플레이어 감지 막기
            _playerInElevatorDetector.BlockDetect(true);

            // 문 열기 기다리기
            StartCoroutine(WaitOpenDoorRoutine(2f));
        }
    }

    private IEnumerator WaitOpenDoorRoutine(float waitSeconds)
    {
        yield return new WaitForSeconds(waitSeconds);
        BlockElevatorDoor(false);
    }
}