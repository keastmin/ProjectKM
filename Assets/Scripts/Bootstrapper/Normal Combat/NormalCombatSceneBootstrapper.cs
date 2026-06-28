using Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class NormalCombatSceneBootstrapper : Bootstrapper
{
    [SerializeField] private PlayerSpawner _playerSpawner;
    [SerializeField] private EnemySpawner _enemySpawner;
    [SerializeField] private CombatCanvas _combatCanvas;
    [SerializeField] private VolumeEffect _volumeEffect;
    [SerializeField] private Camera _uiCamera;

    public override void InitializeScene(GameRunContext context)
    {
        PlayerCore player = _playerSpawner.SpawnPlayer(context, context.SaveDataManager.SavedPlayerInstance, context.MainCamera);
        _combatCanvas.InitializeCombatCanvas(player);
        _volumeEffect.InitializeVolumeEffect(player);
        context.PlayerCinemachineController.InitializePlayerCinemachineController(player, context.InputModeManager);

        context.GameManager.SetGameState(GameState.Combat);
        context.InputModeManager.ClearInputState();
        context.InputModeManager.PushInputState(InputState.Combat);
        var mainCameraData = context.MainCamera.GetComponent<UniversalAdditionalCameraData>();
        mainCameraData.cameraStack.Add(_uiCamera);
    }
}