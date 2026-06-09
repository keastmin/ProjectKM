using UnityEngine;

public class MainSceneBootstrapper : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private MainMenuCanvas _mainMenuCanvas;

    [SerializeField] private GameState _startGameState = GameState.Main;
    [SerializeField] private InputState _startInputState = InputState.UI;

    private void Start()
    {
        StartInitializeSequence();
    }

    private void StartInitializeSequence()
    {
        if(_mainMenuCanvas == null)
        {
            Debug.LogError("메인 메뉴 캔버스가 없음");
            return;
        }
        if(SaveDataManager.Instance == null)
        {
            Debug.LogError("저장 데이터 매니저 없음");
            return;
        }
        if(InputModeManager.Instance == null)
        {
            Debug.LogError("입력 모드 매니저가 없음");
            return;
        }
        if(GameManager.Instance == null)
        {
            Debug.LogError("게임 매니저가 없음");
            return;
        }

        GameManager.Instance.SetGameState(_startGameState);
        InputModeManager.Instance.PushInputState(_startInputState);
        _mainMenuCanvas.InitMainMenuCanvas(SaveDataManager.Instance);
    }
}