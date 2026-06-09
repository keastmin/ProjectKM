using Player;
using UnityEngine;

public class CombatCanvas : MonoBehaviour
{
    [SerializeField] private PlayerCore _player;
    [SerializeField] private CombatUI _combatUI;

    private void Awake()
    {
        if (_player != null)
        {
            PlayerReferenceInject(_player);
            return;
        }
    }

    private void PlayerReferenceInject(PlayerCore player)
    {
        _player = player;
        _combatUI.InitCombatUI(_player);
    }
}
