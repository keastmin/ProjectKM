using Player;
using System;
using Unity.VisualScripting;
using UnityEngine;

public class GameStarter : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerCore _playerPrefab;
    [SerializeField] private PlayerCinemachineController _playerCineCamPrefab;
    [SerializeField] private VolumeEffect _volumeEffectPrefab;
    [SerializeField] private Transform _playerSpawnPoint;

    [Header("Protal")]
    [SerializeField] private GameObject _portalPrefab;

    [Header("Camera")]
    [SerializeField] private Camera _mainCamera;

    public event Action<PlayerCore> OnPlayerSpawnedAction;
    public event Action<PlayerCinemachineController> OnPlayerCinemachineControllerSpawnedAction;

    private void Start()
    {
        PlayerSpawn();
    }

    private void PlayerSpawn()
    {
        if(!DebugUtil.IsExistComponent(_playerPrefab) ||
           !DebugUtil.IsExistComponent(_playerCineCamPrefab) ||
           !DebugUtil.IsExistComponent(_volumeEffectPrefab) ||
           !DebugUtil.IsExistComponent(_playerSpawnPoint))
        {
            return;
        }

        var player = Instantiate(_playerPrefab, _playerSpawnPoint.transform.position, _playerSpawnPoint.transform.rotation);
        var cineCam = Instantiate(_playerCineCamPrefab);
        var volumeEffect = Instantiate(_volumeEffectPrefab);
        if (!cineCam.TrySetTarget(player.CameraPivot))
        {
            Debug.LogError("플레이어 카메라 연결 실패");
            return;
        }

        Camera mainCamera = _mainCamera != null ? _mainCamera : Camera.main;
        player.BindCameraReference(mainCamera);
        player.BindVolumeEffectReference(volumeEffect);

        OnPlayerSpawnedAction?.Invoke(player);
        OnPlayerCinemachineControllerSpawnedAction?.Invoke(cineCam);
    }
}
