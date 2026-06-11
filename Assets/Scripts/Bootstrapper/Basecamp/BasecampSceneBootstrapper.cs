using Player;
using UnityEngine;

public class BasecampSceneBootstrapper : MonoBehaviour
{
    [SerializeField] private bool _playerSpawn = true;

    [Header("Scene Player")]
    [SerializeField] private PlayerCore _scenePlayer;
    [SerializeField] private PlayerCinemachineController _sceneCinemachineController;

    [Header("Prefab Player")]
    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private PlayerCore _playerPrefab;
    [SerializeField] private PlayerCinemachineController _cinemachineControllerPrefab;

    [Header("Effect")]
    [SerializeField] private VolumeEffect _volumeEffect;

    [Header("World")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Portal _portal;
    [SerializeField] private WeaponModeViewerOpener _weaponModeViewerOpener;
    [SerializeField] private PlayerUpgradeOpener _playerUpgradeOpener;

    [Header("UI")]
    [SerializeField] private BasecampCanvas _basecampCanvas;

    [SerializeField] private GameState _startGameState = GameState.Basecamp;
    [SerializeField] private InputState _startInputState = InputState.Combat;

    private void Start()
    {
        InitializeSquence();
    }

    private void InitializeSquence()
    {
        // 매니저 검사
        if (GameManager.Instance == null)
        {
            Debug.LogError("게임 매니저가 없음");
            return;
        }
        if (InputModeManager.Instance == null)
        {
            Debug.LogError("입력 모드 매니저가 없음");
            return;
        }
        if(SaveDataManager.Instance == null)
        {
            Debug.LogError("저장 데이터 매니저가 없음");
        }

        // 메인 카메라 검사
        if(_mainCamera == null)
        {
            Debug.LogError("메인 카메라가 없음");
            return;
        }

        // 플레이어 초기화
        PlayerCore player;
        if (_playerSpawn)
        {
            player = PlayerSpawn();
        }
        else
        {
            player = _scenePlayer;
        }
        if(player == null)
        {
            Debug.LogError("플레이어가 없음");
            return;
        }
        player.InitializePlayer(
            SaveDataManager.Instance,
            InputModeManager.Instance,
            _mainCamera);

        // 월드 오브젝트 검사
        if(_portal == null)
        {
            Debug.LogError("포탈이 없음");
            return;
        }
        if(_weaponModeViewerOpener == null)
        {
            Debug.LogError("무기 모드 뷰어 오프너가 없음");
            return;
        }
        if (_playerUpgradeOpener == null)
        {
            Debug.LogError("플레이어 업그레이드 오프너가 없음");
            return;
        }

        // 매니저 상태 초기화
        GameManager.Instance.SetGameState(_startGameState);
        InputModeManager.Instance.ClearInputState();
        InputModeManager.Instance.PushInputState(_startInputState);

        // 포탈 초기화
        _portal.InitializePortal(inputModeManager: InputModeManager.Instance, gameManager: GameManager.Instance);

        if(_volumeEffect == null)
        {
            Debug.LogError("볼륨 이펙트가 없음");
            return;
        }
        _volumeEffect.InitializeVolumeEffect(player);

        PlayerCinemachineController playerCinemachineController;
        if(_playerSpawn)
        {
            playerCinemachineController = PlayerCinemachineControllerSpawn();
        }
        else
        {
            playerCinemachineController = _sceneCinemachineController;
        }

        if(playerCinemachineController == null)
        {
            Debug.LogError("플레이어 시네머신 컨트롤러가 없음");
            return;
        }
        playerCinemachineController.InitializePlayerCinemachineController(player, InputModeManager.Instance);

        _basecampCanvas.InitBasecampCanvas(
            player, 
            InputModeManager.Instance,
            _weaponModeViewerOpener,
            _playerUpgradeOpener);
    }

    private PlayerCore PlayerSpawn()
    {
        if(_playerPrefab == null)
        {
            Debug.LogError("플레이어 프리팹이 없음");
            return null;
        }

        return Instantiate(_playerPrefab, _playerSpawnPoint.position, _playerSpawnPoint.rotation);
    }

    private PlayerCinemachineController PlayerCinemachineControllerSpawn()
    {
        if(_cinemachineControllerPrefab == null)
        {
            Debug.LogError("플레이어 시네머신 컨트롤러 프리팹이 없음");
            return null;
        }

        return Instantiate(_cinemachineControllerPrefab);
    }
}
