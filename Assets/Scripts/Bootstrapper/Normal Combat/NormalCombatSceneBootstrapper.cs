using UnityEngine;

public class NormalCombatSceneBootstrapper : MonoBehaviour
{
    [SerializeField] private PlayerSpawner _playerSpawner;
    [SerializeField] private EnemySpawner _enemySpawner;
    [SerializeField] private CombatCanvas _combatCanvas;

    private GameManager _gameManager;
    private InputModeManager _inputModeManager;
    private SaveDataManager _saveDataManager;

    public void InitializeNormalCombatScene()
    {

    }
}