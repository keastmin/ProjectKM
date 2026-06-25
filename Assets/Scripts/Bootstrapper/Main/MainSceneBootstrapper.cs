using UnityEngine;

public class MainSceneBootstrapper : Bootstrapper
{
    [Header("UI")]
    [SerializeField] private MainMenuCanvas _mainMenuCanvas;

    [SerializeField] private GameState _startGameState = GameState.Main;
    [SerializeField] private InputState _startInputState = InputState.UI;

    public override void InitializeScene(GameRunContext context)
    {
        StartInitializeSequence(context);
    }

    private void StartInitializeSequence(GameRunContext context)
    {
        if(_mainMenuCanvas == null)
        {
            Debug.LogError("메인 메뉴 캔버스가 없음");
            return;
        }

        context.GameManager.SetGameState(_startGameState);
        context.InputModeManager.PushInputState(_startInputState);
        _mainMenuCanvas.InitMainMenuCanvas(context);
    }
}