using Player;
using UnityEngine;

public class CombatUI : MonoBehaviour
{
    [SerializeField] private PlayerHPUI _hpUI;
    [SerializeField] private PlayerDodgeUI _dodgeUI;
    [SerializeField] private QSkillUI _qSkillUI;

    private PlayerCore _player;

    public void InitCombatUI(PlayerCore player)
    {
        _player = player;
        _hpUI.InitPlayerHPUI(_player);
        _dodgeUI.InitDodgeUI(_player);
        _qSkillUI.InitQSkillUI(_player);
    }
}