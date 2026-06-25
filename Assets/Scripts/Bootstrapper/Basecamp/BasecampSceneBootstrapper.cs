using Player;
using Unity.Cinemachine;
using UnityEngine;

public class BasecampSceneBootstrapper : Bootstrapper
{
    [SerializeField] private bool _playerSpawn = true;

    [Header("Scene Player")]
    [SerializeField] private PlayerSpawner _playerSpawner;
    [SerializeField] private PlayerCinemachineController _playerCinemachineController;

    [Header("Effect")]
    [SerializeField] private VolumeEffect _volumeEffect;

    [Header("World")]
    [SerializeField] private WeaponModeViewerOpener _weaponModeViewerOpener;
    [SerializeField] private PlayerUpgradeOpener _playerUpgradeOpener;

    [Header("UI")]
    [SerializeField] private BasecampCanvas _basecampCanvas;

    [Header("System")]
    [SerializeField] private NodeMapTransitionDirector _nodeMapTransitionDirector;

    [Header("Start Setting Variable")]
    [SerializeField] private GameState _startGameState = GameState.Basecamp;
    [SerializeField] private InputState _startInputState = InputState.Combat;

    public override void InitializeScene(GameRunContext context)
    {
        // 플레이어 초기화
        PlayerCore player = _playerSpawner.SpawnPlayer(context, context.SaveDataManager.SavedPlayerInstance, context.MainCamera);
        _playerCinemachineController.InitializePlayerCinemachineController(player, context.InputModeManager);

        // 월드 오브젝트 검사

        // 매니저 상태 초기화
        context.GameManager.SetGameState(_startGameState);
        context.InputModeManager.ClearInputState();
        context.InputModeManager.PushInputState(_startInputState);

        // 노드맵 전환 시스템 초기화
        _nodeMapTransitionDirector.InitializeNodeMapTransitionDirector(context.GameManager, context.InputModeManager, context.CinemachineBrain);

        // 볼륨 이펙트 초기화
        _volumeEffect.InitializeVolumeEffect(player);

        // 베이스캠프 UI 초기화
        _basecampCanvas.InitBasecampCanvas(
            player,
            context.InputModeManager,
            _weaponModeViewerOpener,
            _playerUpgradeOpener);
    }
}
