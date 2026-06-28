using Player;
using UnityEngine;

public class CombatCanvas : MonoBehaviour
{
    [SerializeField] private CombatUI _combatUI;

    public void InitializeCombatCanvas(PlayerCore playerCore)
    {
        _combatUI.InitCombatUI(playerCore);
    }
}
