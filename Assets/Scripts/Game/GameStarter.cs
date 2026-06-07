using Player;
using System;
using Unity.VisualScripting;
using UnityEngine;

public class GameStarter : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerCore _playerPrefab;
    [SerializeField] private PlayerCinemachineController _playerCineCamPrefab;
    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private PlayerStatData _playerStatData;

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
           !DebugUtil.IsExistComponent(_playerSpawnPoint))
        {
            return;
        }

        var player = Instantiate(_playerPrefab, _playerSpawnPoint.transform.position, _playerSpawnPoint.transform.rotation);
        var cineCam = Instantiate(_playerCineCamPrefab);
        if (!cineCam.TrySetTarget(player.CameraPivot))
        {
            Debug.LogError("플레이어 카메라 연결 실패");
            return;
        }

        Camera mainCamera = _mainCamera != null ? _mainCamera : Camera.main;
        PlayerInstance playerInstance = GetPlayerInstance();
        if (playerInstance == null)
        {
            return;
        }

        player.InitializePlayer(playerInstance, mainCamera);

        OnPlayerSpawnedAction?.Invoke(player);
        OnPlayerCinemachineControllerSpawnedAction?.Invoke(cineCam);
    }

    private PlayerInstance GetPlayerInstance()
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.GetOrCreatePlayerInstance(_playerStatData);
        }

        if (_playerStatData == null)
        {
            Debug.LogError("PlayerStatData is null.");
            return null;
        }

        return new PlayerInstance(_playerStatData);
    }
}
