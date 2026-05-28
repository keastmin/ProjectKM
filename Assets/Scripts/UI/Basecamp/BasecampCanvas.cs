using Player;
using UnityEngine;

public class BasecampCanvas : MonoBehaviour
{
    [SerializeField] private PlayerCore _player;
    [SerializeField] private GameStarter _gameStarter;
    [SerializeField] private WeaponModeViewerUI _weaponModeViewerUI;

    private void Awake()
    {
        _gameStarter.OnPlayerSpawnedAction += PlayerReferenceInject;
        _weaponModeViewerUI.gameObject.SetActive(false);
    }

    private void PlayerReferenceInject(PlayerCore player)
    {
        _player = player;
    }
}
