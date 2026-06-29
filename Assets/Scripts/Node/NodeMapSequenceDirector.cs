using UnityEngine;

public class NodeMapSequenceDirector : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private InputModeManager _inputModeManager;
    [SerializeField] private CombatCanvas _combatCanvas;
    [SerializeField] private NodeMapPlayerDetector _nodeMapPlayerDetector;
    [SerializeField] private NodeMapCameraController _nodeMapCameraController;
    [SerializeField] private NodeMapView _nodeMapView;
    [SerializeField] private NodeMapGenerator _nodeMapGenerator;

    private void OnEnable()
    {
        BindPlayerDetectEvent();
    }

    private void OnDisable()
    {
        UnbindPlayerDetectEvent();
    }

    public void InitializeNodeMapSequenceDirector(GameRunContext context)
    {
        _gameManager = context.GameManager;
        _inputModeManager = context.InputModeManager;
        _combatCanvas = context.CombatCanvas;
    }

    private void BindPlayerDetectEvent()
    {
        if (_nodeMapPlayerDetector == null)
            return;

        _nodeMapPlayerDetector.OnPlayerInNodeMapRange -= StartNodeMapSequence;
        _nodeMapPlayerDetector.OnPlayerInNodeMapRange += StartNodeMapSequence;
    }

    private void UnbindPlayerDetectEvent()
    {
        if (_nodeMapPlayerDetector == null)
            return;

        _nodeMapPlayerDetector.OnPlayerInNodeMapRange -= StartNodeMapSequence;
    }

    // 노드 맵 진입 연출 시퀀스 시작
    private void StartNodeMapSequence()
    {
        // 전투 UI 끄기
        _combatCanvas.SetActiveCombatUI(false);

        // 게임 상태 변화
        _inputModeManager.PushInputState(InputState.NodeMap);
        _gameManager.SetGameState(GameState.NodeMap);

        // 카메라 연출
        _nodeMapCameraController.NodeMapCinemachineBlend();

        // 노드 표시
        _nodeMapView.SetActiveNodeView(true);
    }
}
