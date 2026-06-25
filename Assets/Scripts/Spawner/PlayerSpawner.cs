using Player;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private PlayerCore _playerPrefab;

    public PlayerCore SpawnPlayer(GameRunContext context, PlayerInstance playerInstance, Camera mainCamera)
    {
        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = transform.rotation;
        PlayerCore player = Instantiate(_playerPrefab, spawnPosition, spawnRotation);
        if (player == null)
        {
            Debug.LogError("PlayerCore가 없음");
            return null;
        }
        player.InitializePlayer(context, playerInstance, mainCamera);
        return player;
    }
}
