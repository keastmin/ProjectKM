using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyCore _enemyPrefab;

    public EnemyCore SpawnEnemy()
    {
        EnemyCore enemy = Instantiate(_enemyPrefab, transform.position, Quaternion.identity);
        return enemy;
    }
}