using Player;
using Unity.Cinemachine;
using UnityEngine;

public class BasecampSceneBootstrapper : Bootstrapper
{
    [SerializeField] private bool _playerSpawn = true;

    [Header("Scene Player")]
    [SerializeField] private PlayerSpawner _playerSpawner;

    [Header("Effect")]
    [SerializeField] private VolumeEffect _volumeEffect;

    [Header("World")]
    [SerializeField] private WeaponModeViewerOpener _weaponModeViewerOpener;
    [SerializeField] private PlayerUpgradeOpener _playerUpgradeOpener;
    [SerializeField] private BasecampElevator _basecampElevator;

    [Header("UI")]
    [SerializeField] private BasecampCanvas _basecampCanvas;

    [Header("Start Setting Variable")]
    [SerializeField] private GameState _startGameState = GameState.Basecamp;
    [SerializeField] private InputState _startInputState = InputState.Combat;

    public override void InitializeScene(GameRunContext context)
    {
        // 플레이어 초기화
        PlayerCore player = _playerSpawner.SpawnPlayer(context, context.SaveDataManager.SavedPlayerInstance, context.MainCamera);
        context.PlayerCinemachineController.gameObject.SetActive(true);
        context.PlayerCinemachineController.InitializePlayerCinemachineController(player, context.InputModeManager);

        // 매니저 상태 초기화
        context.GameManager.SetGameState(_startGameState);
        context.InputModeManager.ClearInputState();
        context.InputModeManager.PushInputState(_startInputState);

        // 볼륨 이펙트 초기화
        _volumeEffect.InitializeVolumeEffect(player);

        // 베이스캠프 UI 초기화
        _basecampCanvas.InitBasecampCanvas(
            player,
            context.InputModeManager,
            _weaponModeViewerOpener,
            _playerUpgradeOpener);

        _basecampElevator.InitializeElevator(player, context);
    }
}
