using System.Collections.Generic;
using UnityEngine;

public class EnemyCore : MonoBehaviour, IDamageable
{
    [Header("능력치")]
    [SerializeField] private EnemyAttributesSO _enemyAttributesSO;

    [Header("UI")]
    [SerializeField] private EnemyHPUI _enemyHPUI;

    [Header("감지")]
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private float _detectRadius;

    private float _maxHP;
    private float _currentHP;
    private float _attack;
    private float _defence;
    private float _walkSpeed;
    private float _runSpeed;

    public bool DamagedFlag { get; set; }

    // 플레이어 감지
    private Collider[] _detectedColliders;
    private Collider _detectedPlayer;

    public LayerMask PlayerLayer => _playerLayer;
    public float DetectRadius => _detectRadius;
    public Collider DetectedPlayer => _detectedPlayer;

    public float MaxHP => _maxHP;
    public float CurrentHP
    {
        get => _currentHP;
        set
        {
            _currentHP = value;
        }
    }

    protected virtual void Awake()
    {
        InitializeAttributes(_enemyAttributesSO);
        _detectedColliders = new Collider[3];
    }

    protected virtual void Update()
    {
        // 플레이어 감지
        DetectPlayer();
    }

    public void InitializeEnemyCore(GameRunContext context)
    {
        _enemyHPUI.InitializeEnemyHPUI(context.MainCamera);
    }

    public virtual void TakeDamage(float damage)
    {
        DamagedFlag = true;
    }

    private void DetectPlayer()
    {
        _detectedPlayer = null;
        int detectedCount = Physics.OverlapSphereNonAlloc(transform.position, DetectRadius, _detectedColliders, PlayerLayer);
        if (detectedCount > 0)
            _detectedPlayer = _detectedColliders[0];
    }

    protected virtual void InitializeAttributes(EnemyAttributesSO attributesSO)
    {
        _maxHP = attributesSO.MaxHP;
        CurrentHP = _maxHP;
    }
}