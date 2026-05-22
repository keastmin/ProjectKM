using NoiRC.SRMove;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(InputController))]
    [RequireComponent(typeof(HitController))]
    [RequireComponent(typeof(TargetingController))]
    [RequireComponent(typeof(AttackEffectController))]
    [RequireComponent(typeof(PlayerSkillController))]
    [RequireComponent(typeof(Animator))]
    public class PlayerCore : MonoBehaviour, IDamageable
    {
        [Header("스탯")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _attackPower = 10f;
        [SerializeField] private float _defensePower = 5f;

        [Header("움직임")]
        [SerializeField] private float _jogSpeed = 5f;
        [SerializeField] private float _runSpeed = 8f;

        [Header("공격")]
        [SerializeField] private AttackData[] _katanaComboDatas; // 콤보 공격
        [SerializeField] private AttackData _dodgeCounterData; // 회피 반격
        [SerializeField] private AttackData _dashAttackData; // 대쉬 공격
        [SerializeField] private float _hitStopDuration = 0.03f; // 히트스탑 지속 시간

        [Header("모션 워핑 데이터")]
        [SerializeField] private float _basicComboAttackMotionWarpSpeed = 20f;

        [Header("카메라")]
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private CinemachineImpulseSource _cinemachineImpulseSource;
        [SerializeField] private VolumeEffect _volumeEffect;
        [SerializeField] private Transform _cameraPivot;

        [Header("상태")]
        [SerializeField] private StateVariableContainter _stateVariables;

        [Header("회피")]
        [SerializeField] private float _perfectDodgeSlowTimeScale = 0.15f;
        [SerializeField] private float _perfectDodgeSlowStartOffset = 0.12f;
        [SerializeField] private float _perfectDodgeSlowDownDuration = 0.1f;
        [SerializeField] private float _perfectDodgeSlowHoldDuration = 0.11f;
        [SerializeField] private float _perfectDodgeRecoverDuration = 0.1f;
        [SerializeField] private int _maxDodgeAvailableCount = 2; // 최대 연속 회피 가능 횟수
        [SerializeField] private float _dodgeCooldown = 2f; // 회피 쿨타임
        [SerializeField]
        private AnimationCurve _vignetteCurve =
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.3f, 0.35f),
                new Keyframe(1f, 0f)); // 완벽 회피 시 화면을 흑백으로 만드는 효과를 위한 커브
        private int _dodgeAvailableCount; // 현재 남은 연속 회피 가능 횟수
        private float _currentDodgeCooldownTimer; // 현재 회피 쿨타임 타이머
        private EnemyCore _dodgeEnemy; // 회피 타이밍을 준 주체 적

        [Header("스킬")]
        [SerializeField] private List<SkillDefinition> _skillDatas;

        // 컴포넌트
        private InputController _inputController;
        //private CharacterMover _characterMover;
        private AvatarMover _avatarMover;
        private HitController _hitController;
        private TargetingController _targetingController;
        private AttackEffectController _attackEffectController;
        private PlayerSkillController _skillController;
        private Animator _animator;
        private MeshTrailEffectController _trailEffector;
        private readonly HashSet<Component> _activeDodgeTimingSources = new();
        private Coroutine _perfectDodgeTimeScaleCoroutine;
        private Coroutine _hitStopCoroutine;
        private Coroutine _bindingWaitCoroutine;
        private bool _canPerfectDodge = false;
        private bool _isCoreReady = false;
        private bool _isInitialized = false;

        // 속도
        private float _targetSpeed;
        private float _currentSpeed;

        // 상태머신
        private StateMachine _fsm;

        // 스탯
        private float _hp;
        private bool _isDead = false;

        // Action
        public event Action<float, float> OnHealthChanged;
        public event Action<int> OnDodgeCountChanged;
        public event Action<float, float> OnDodgeTimerRunning;
        public event Action<SkillDefinition> OnQSkillChanged;

        public StateMachine FSM => _fsm;
        public InputController InputController => _inputController;
        //public CharacterMover CharacterMover => _characterMover;
        public AvatarMover Mover => _avatarMover;
        public HitController HitController => _hitController;
        public TargetingController TargetingController => _targetingController;
        public AttackEffectController AttackEffectController => _attackEffectController;
        public PlayerSkillController SkillController => _skillController;
        public Animator Animator => _animator;
        public VolumeEffect VolumeEffect => _volumeEffect;
        public MeshTrailEffectController TrailEffector => _trailEffector;
        public Transform CameraPivot => _cameraPivot;
        public float JogSpeed => _jogSpeed;
        public float RunSpeed => _runSpeed;
        public float TargetSpeed
        {
            get { return _targetSpeed; }
            set { _targetSpeed = value; }
        }
        public float CurrentSpeed
        {
            get { return _currentSpeed; }
            set { _currentSpeed = value; }
        }
        public float DodgeCounterDuration => _perfectDodgeSlowDownDuration + _perfectDodgeSlowHoldDuration;
        public float BasicComboAttackMotionWarpSpeed => _basicComboAttackMotionWarpSpeed;
        public Camera PlayerCamera => _playerCamera;
        public AttackData[] KatanaComboDatas => _katanaComboDatas;
        public AttackData DodgeCounterData => _dodgeCounterData;
        public AttackData DashAttackData => _dashAttackData;
        public StateVariableContainter StateVariables => _stateVariables;
        public bool DamageFlag { get; set; } = false;
        public bool CanReceiveDamage => _fsm?.CanReceiveDamage ?? true;
        public bool IsInitialized => _isInitialized;
        public float CurrentDodgeCooldownTimer
        {
            get
            {
                return _currentDodgeCooldownTimer;
            }
            set
            {
                _currentDodgeCooldownTimer = value;
                OnDodgeTimerRunning?.Invoke(_currentDodgeCooldownTimer, _dodgeCooldown);
            }
        }
        public int DodgeAvailableCount
        {
            get
            {
                return _dodgeAvailableCount;
            }
            private set
            {
                _dodgeAvailableCount = value;
                OnDodgeCountChanged?.Invoke(_dodgeAvailableCount);
            }
        }
        public bool CanPerfectDodge => _canPerfectDodge;
        public float HP
        {
            get { return _hp; }
            set
            {
                _hp = value;
                _hp = Mathf.Clamp(_hp, 0f, MaxHealth);
                OnHealthChanged?.Invoke(_hp, MaxHealth);
            }
        }
        public float MaxHealth => _maxHealth;
        public float AttackPower => _attackPower;
        public float DefensePower => _defensePower;
        public bool IsDead
        {
            get
            {
                return _isDead;
            }
            set
            {
                _isDead = value;
            }
        }
        public EnemyCore DodgeEnemy => _dodgeEnemy;
        public List<SkillDefinition> SkillDatas => _skillDatas;

        private void Awake()
        {
            TryGetComponent(out _inputController);
            //TryGetComponent(out _characterMover);
            TryGetComponent(out _avatarMover);
            TryGetComponent(out _hitController);
            TryGetComponent(out _targetingController);
            TryGetComponent(out _attackEffectController);
            TryGetComponent(out _skillController);
            _skillController.OnQSkillEquiped += QSkillChange;

            TryGetComponent(out _animator);
            TryGetComponent(out _cinemachineImpulseSource);
            TryGetComponent(out _trailEffector);
            _fsm = new StateMachine(this);
            HP = MaxHealth;

            DodgeAvailableCount = _maxDodgeAvailableCount;
            CurrentDodgeCooldownTimer = _dodgeCooldown;
            _isDead = false;
            _isCoreReady = true;
        }

        private void Start()
        {
            TryInitializeWhenReady();
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                SkillController.EquipSkill(PlayerSkillSlot.Q, SkillDatas[0]);
            }

            if(HP <= 0f && !IsDead)
            {
                FSM.Transition(FSM.DeathState);
                return;
            }

            if(CurrentDodgeCooldownTimer < _dodgeCooldown)
            {
                CurrentDodgeCooldownTimer += Time.deltaTime;
                if(CurrentDodgeCooldownTimer >= _dodgeCooldown)
                {
                    DodgeAvailableCount = _maxDodgeAvailableCount;
                }
            }

            _fsm.Tick();

            SetPerfectDodgeEnable(false);
        }

        private void FixedUpdate()
        {
            if (!_isInitialized)
            {
                return;
            }

            SmoothSpeedChanger();
            _fsm.FixedTick();
        }

        private void LateUpdate()
        {
            if (!_isInitialized)
            {
                return;
            }

            _fsm.LateTick();
        }

        private void OnAnimatorMove()
        {
            if (!_isInitialized)
            {
                return;
            }

            _fsm.AnimationTick();
        }

        private bool HasRequiredBindings()
        {
            return _playerCamera != null && _volumeEffect != null;
        }

        private bool TryInitializeWhenReady()
        {
            if (_isInitialized)
            {
                return true;
            }

            if (!_isCoreReady)
            {
                return false;
            }

            if (!HasRequiredBindings())
            {
                if (_bindingWaitCoroutine == null)
                {
                    _bindingWaitCoroutine = StartCoroutine(WaitForRequiredBindings());
                }

                return false;
            }

            if (_bindingWaitCoroutine != null)
            {
                StopCoroutine(_bindingWaitCoroutine);
                _bindingWaitCoroutine = null;
            }

            _fsm.InitStateMachine(_fsm.IdleState);
            _isInitialized = true;
            return true;
        }

        private IEnumerator WaitForRequiredBindings()
        {
            while (!HasRequiredBindings())
            {
                yield return null;
            }

            _bindingWaitCoroutine = null;
            TryInitializeWhenReady();
        }

        private void SmoothSpeedChanger()
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, TargetSpeed, Time.fixedDeltaTime * 8f);
            if (Mathf.Abs(CurrentSpeed - TargetSpeed) <= 0.01f) CurrentSpeed = TargetSpeed;
            _animator.SetFloat("MoveSpeed", CurrentSpeed);
        }

        public void TakeDamage(float damage)
        {
            if (!CanReceiveDamage)
            {
                return;
            }

            DamageFlag = true;
            HP -= (damage - DefensePower);
        }

        public void Heal(float amount)
        {
            HP += amount;
        }

        public void TriggerPerfectDodgeTimeScale()
        {
            if (_perfectDodgeTimeScaleCoroutine != null)
            {
                StopCoroutine(_perfectDodgeTimeScaleCoroutine);
            }

            _perfectDodgeTimeScaleCoroutine = StartCoroutine(PerfectDodgeTimeScaleRoutine());
        }

        public void StopPerfectDodgeTimeScaleImmediate()
        {
            if (_perfectDodgeTimeScaleCoroutine != null)
            {
                StopCoroutine(_perfectDodgeTimeScaleCoroutine);
                _perfectDodgeTimeScaleCoroutine = null;
            }

            Time.timeScale = 1f;
        }

        private IEnumerator PerfectDodgeTimeScaleRoutine()
        {
            const float normalTimeScale = 1f;
            float targetTimeScale = Mathf.Clamp(_perfectDodgeSlowTimeScale, 0.01f, normalTimeScale);

            yield return new WaitForSecondsRealtime(_perfectDodgeSlowStartOffset);

            yield return LerpTimeScale(Time.timeScale, targetTimeScale, _perfectDodgeSlowDownDuration);

            Time.timeScale = targetTimeScale;
            yield return new WaitForSecondsRealtime(_perfectDodgeSlowHoldDuration);

            yield return LerpTimeScale(Time.timeScale, normalTimeScale, _perfectDodgeRecoverDuration);

            Time.timeScale = normalTimeScale;
            _perfectDodgeTimeScaleCoroutine = null;
        }

        private IEnumerator LerpTimeScale(float start, float end, float duration)
        {
            if (duration <= 0f)
            {
                Time.timeScale = end;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Time.timeScale = Mathf.Lerp(start, end, t);
                yield return null;
            }

            Time.timeScale = end;
        }

        // 회피 카운트를 감소시킴
        public void ConsumeDodge() 
        {
            CurrentDodgeCooldownTimer = 0f;
            DodgeAvailableCount--; 
        }

        // 카메라 흔들림 트리거
        public void CameraShake()
        {
            if(_cinemachineImpulseSource != null)
            {
                _cinemachineImpulseSource.GenerateImpulse();
            }
            else
            {
                Debug.Log("시네머신 임펄스 소스가 없습니다.");
            }
        }

        // 히트스탑 트리거
        public void StartHitStop()
        {
            if (_hitStopCoroutine != null)
                return;

            _hitStopCoroutine = StartCoroutine(HitStop());
        }

        // 히트스탑 적용
        private IEnumerator HitStop()
        {
            float originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(_hitStopDuration);
            Time.timeScale = originalTimeScale;
            _hitStopCoroutine = null;
        }

        public void RecievePerfectDodgeInfo(EnemyCore dodgeEnemy)
        {
            SetPerfectDodgeEnable(true);
            _dodgeEnemy = dodgeEnemy;
        }

        public void SetPerfectDodgeEnable(bool isPerfectDodgeTiming)
        {
            _canPerfectDodge = isPerfectDodgeTiming;
        }

        private void QSkillChange(SkillDefinition skill)
        {
            OnQSkillChanged?.Invoke(skill);
        }

        public void BindCameraReference(Camera mainCamera)
        {
            if (_playerCamera != null)
            {
                TryInitializeWhenReady();
                return;
            }

            if (mainCamera == null)
            {
                TryInitializeWhenReady();
                return;
            }

            _playerCamera = mainCamera;
            TryInitializeWhenReady();
        }

        public void BindVolumeEffectReference(VolumeEffect volumeEffect)
        {
            if (_volumeEffect != null)
            {
                TryInitializeWhenReady();
                return;
            }

            if (volumeEffect == null)
            {
                TryInitializeWhenReady();
                return;
            }

            _volumeEffect = volumeEffect;
            TryInitializeWhenReady();
        }
    }
}
