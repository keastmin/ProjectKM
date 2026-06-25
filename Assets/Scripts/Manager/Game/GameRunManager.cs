using Player;
using Unity.Cinemachine;
using UnityEngine;

public class GameRunManager : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private InputModeManager _inputModeManager;
    [SerializeField] private SaveDataManager _saveDataManager;
    [SerializeField] private SceneFlowManager _sceneFlowManager;
    [SerializeField] private CinemachineBrain _cinemachineBrain;
    [SerializeField] private Camera _mainCamera;

    public GameRunContext Context { get; private set; }

    private void Awake()
    {
        Context = new GameRunContext(_gameManager, _inputModeManager, _saveDataManager, _sceneFlowManager, _cinemachineBrain, _mainCamera);
    }
}
