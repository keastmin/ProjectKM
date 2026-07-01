using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [SerializeField] private InputModeManager _inputModeManager;
    [SerializeField] private StageUICanvas _stageUICanvas;

    private int _remainEnemyCount;

    public int RemainEnemyCount
    {
        get
        {
            return _remainEnemyCount;
        }
        set
        {
            _remainEnemyCount = value;
            if (_remainEnemyCount <= 0)
                ClearCombatStage();
        }
    }

    public void InitializeCombatManager(List<EnemyCore> enemies, InputModeManager inputModeManager)
    {
        _inputModeManager = inputModeManager;

        RemainEnemyCount = enemies.Count;
        foreach(var enemy in enemies)
        {
            enemy.OnEnemyDead -= HandleEnemyDead;
            enemy.OnEnemyDead += HandleEnemyDead;
        }
    }

    private void ClearCombatStage()
    {
        _inputModeManager.PushInputState(InputState.UI);
        _stageUICanvas.SetActiveStageScreen(true);
    }

    private void HandleEnemyDead()
    {
        RemainEnemyCount--;
    }
}