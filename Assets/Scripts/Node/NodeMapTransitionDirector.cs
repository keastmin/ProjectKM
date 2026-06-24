using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class NodeMapTransitionDirector : MonoBehaviour
{
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private BasecampElevator _basecampElevator;
    [SerializeField] private ExternalWorldSceneAdditiveController _externalWorldSceneAdditiveController;

    private GameManager _gameManager;
    private InputModeManager _inputModeManager;

    private CinemachineBrain _cinemachineBrain;

    private NodeMapReferenceFinder _nodeMapReferenceFinder;
    private NodeMapPlayerDetector _nodeMapPlayerDetector;
    private NodeMapViewController _nodeMapViewController;
    private NodeMapInteractor _nodeMapInteractor;
    private bool _isFindNodeMapPlayerDetector;

    private void OnEnable()
    {
        BindPlayerInElevatorEvent();
        BindPlayerInNodeMapRangeEvent();
    }

    private void OnDisable()
    {
        UnbindPlayerInElevatorEvent();
        UnbindPlayerInNodeMapRangeEvent();
    }

    public void InitializeNodeMapTransitionDirector(
        GameManager gameManager, 
        InputModeManager inputModeManager, 
        CinemachineBrain cinemachineBrain)
    {
        _gameManager = gameManager;
        _inputModeManager = inputModeManager;
        _cinemachineBrain = cinemachineBrain;
    }

    private void BindPlayerInElevatorEvent()
    {
        if(_basecampElevator == null)
        {
            Debug.LogError("BasecampElevator가 없음");
            return;
        }

        _basecampElevator.OnPlayerInElevator -= StartNodeMapLoadSequence;
        _basecampElevator.OnPlayerInElevator += StartNodeMapLoadSequence;
    }

    private void UnbindPlayerInElevatorEvent()
    {
        if (_basecampElevator == null)
        {
            Debug.LogError("BasecampElevator가 없음");
            return;
        }

        _basecampElevator.OnPlayerInElevator -= StartNodeMapLoadSequence;
    }

    private void BindPlayerInNodeMapRangeEvent()
    {
        if (!_isFindNodeMapPlayerDetector)
            return;

        if (_nodeMapPlayerDetector == null)
        {
            Debug.LogError("BasecampElevator가 없음");
            return;
        }

        _nodeMapPlayerDetector.OnPlayerInNodeMapRange -= SequenceAfterSceneLoaded;
        _nodeMapPlayerDetector.OnPlayerInNodeMapRange += SequenceAfterSceneLoaded;
    }

    private void UnbindPlayerInNodeMapRangeEvent()
    {
        if (!_isFindNodeMapPlayerDetector)
            return;

        if (_nodeMapPlayerDetector == null)
        {
            Debug.LogError("BasecampElevator가 없음");
            return;
        }

        _nodeMapPlayerDetector.OnPlayerInNodeMapRange -= SequenceAfterSceneLoaded;
    }

    // 코루틴 시퀀스로 변경하기
    private void StartNodeMapLoadSequence()
    {
        if(_externalWorldSceneAdditiveController == null)
        {
            Debug.LogError("ExternalWorldSceneAdditiveController가 없음");
            return;
        }
        if(_basecampElevator == null)
        {
            Debug.LogError("BasecampElevator가 없음");
            return;
        }

        StartCoroutine(NodeMapLoadSequence());      
    }

    private IEnumerator NodeMapLoadSequence()
    {
        _basecampElevator.BlockElevatorDoor(true);

        yield return new WaitForSeconds(1f); // 문이 완전히 닫힐 시간 기다리기

        yield return _externalWorldSceneAdditiveController.DestroyBasecampObjects(); // 연구소 오브젝트들 정리하기

        yield return _externalWorldSceneAdditiveController.LoadNodeMapScene(); // 외부 월드 씬 로드하기

        FindNodeMapReference();

        yield return new WaitForSeconds(1f); // 절차 후 기다렸다 문열기

        _basecampElevator.BlockElevatorDoor(false);
    }

    private void FindNodeMapReference()
    {
        // 노드 참조 파인더 찾기
        _nodeMapReferenceFinder = FindAnyObjectByType<NodeMapReferenceFinder>();
        if (_nodeMapReferenceFinder == null)
        {
            Debug.LogError("NodeMapReferenceFinder가 없음");
            return;
        }

        // 노드맵 플레이어 감지기 받고 초기화
        _nodeMapPlayerDetector = _nodeMapReferenceFinder.NodeMapPlayerDetector;
        if (_nodeMapPlayerDetector == null)
        {
            Debug.LogError("NodeMapPlayerDetector가 없음");
            return;
        }
        _isFindNodeMapPlayerDetector = true;
        BindPlayerInNodeMapRangeEvent();

        // 노드맵 시작 연출 컨트롤러 받기
        _nodeMapViewController = _nodeMapReferenceFinder.NodeMapViewController;
        if(_nodeMapViewController == null)
        {
            Debug.LogError("NodeMapViewController가 없음");
            return;
        }

        // 노드맵 상호작용에 참조 전달
        _nodeMapInteractor = _nodeMapReferenceFinder.NodeMapInteractor;
        if(_nodeMapInteractor == null)
        {
            Debug.LogError("NodeMapInteractor가 없음");
            return;
        }
        _nodeMapInteractor.InitializeNodeMapInteractor(_mainCamera, _inputModeManager);
    }

    private void SequenceAfterSceneLoaded()
    {
        if (_gameManager == null || _inputModeManager == null)
            return;

        // 게임 상태 변경
        _gameManager.SetGameState(GameState.NodeMap);
        _inputModeManager.PushInputState(InputState.NodeMap);

        // 노드맵 진입 시퀀스 실행
        StartCoroutine(StartNodeMapSequence());
    }

    private IEnumerator StartNodeMapSequence()
    {
        yield return _nodeMapViewController.StartNodeMapCinemachineBlend(_cinemachineBrain);
    }
}