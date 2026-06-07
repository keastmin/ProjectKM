using UnityEngine;

public class MainSceneBootstrapper : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private MainMenuCanvas _mainMenuCanvas;

    [Header("Manager")]
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private InputModeManager _inputModeManager;
    [SerializeField] private SaveDataManager _saveDataManager;

    private void Start()
    {
        StartInitializeSequence();
    }

    private void StartInitializeSequence()
    {
        if(_saveDataManager == null)
        {
            Debug.LogError("저장 데이터 매니저 없음");
            return;
        }

        _mainMenuCanvas.InitMainMenuCanvas(_saveDataManager);
    }
}