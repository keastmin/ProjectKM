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
    [RequireComponent(typeof(PlayerWeaponController))]
    [RequireComponent(typeof(Animator))]
    public class PlayerCore : MonoBehaviour, IDamageable
    {
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
        private int _dodgeAvailableCount; // 현재 남은 연속 회피 가능 횟수
        private float _currentDodgeCooldownTimer; // 현재 회피 쿨타임 타이머
        private EnemyCore _dodgeEnemy; // 회피 타이밍을 준 주체 적

        [Header("스킬")]
        [SerializeField] private List<SkillDefinition> _skillDatas;

        [Header("상호작용")]
        [SerializeField] private LayerMask _interactLayer;

        // 매니저
        private SaveDataManager _saveDataManager;
        private InputModeManager _inputModeManager;

        // 컴포넌트
        private InputController _inputController;
        private AvatarMover _avatarMover;
        private HitController _hitController;
        private TargetingController _targetingController;
        private AttackEffectController _attackEffectController;
        private PlayerSkillController _skillController;
        private Animator _animator;
        private MeshTrailEffectController _trailEffector;
        private PlayerWeaponController _weaponController;
        private Coroutine _perfectDodgeTimeScaleCoroutine;
        private Coroutine _hitStopCoroutine;
        private bool _canPerfectDodge = false;
        private bool _isInitialized = false;

        // 인스턴스
        private PlayerInstance _playerInstance;

        // 속도
        private float _targetSpeed;
        private float _currentSpeed;

        // 상태머신
        private StateMachine _fsm;

        // 스탯
        private bool _isDead = false;

        // Action
        public event Action<float, float> OnHealthChanged;
        public event Action<int> OnDodgeCountChanged;
        public event Action<float, float> OnDodgeTimerRunning;
        public event Action<SkillDefinition> OnQSkillChanged; 
        public event Action<float> OnPerfectDodge; // 완벽 회피 발동

        public SaveDataManager SaveDataManager => _saveDataManager;
        public InputModeManager InputModeManager => _inputModeManager;
        public StateMachine FSM => _fsm;
        public InputController InputController => _inputController;
        public AvatarMover Mover => _avatarMover;
        public HitController HitController => _hitController;
        public TargetingController TargetingController => _targetingController;
        public AttackEffectController AttackEffectController => _attackEffectController;
        public PlayerSkillController SkillController => _skillController;
        public Animator Animator => _animator;
        public MeshTrailEffectController TrailEffector => _trailEffector;
        public PlayerWeaponController WeaponController => _weaponController;
        public Transform CameraPivot => _cameraPivot;
        public float JogSpeed => Instance.JogSpeed;
        public float RunSpeed => Instance.RunSpeed;
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
        public PlayerInstance Instance
        {
            get
            {
                return _playerInstance;
            }
            private set
            {
                _playerInstance = value;
            }
        }
        public float HP
        {
            get 
            { 
                return Instance.CurrentHealth; 
            }
            set
            {
                Instance.CurrentHealth = Mathf.Clamp(value, 0f, MaxHealth);
                OnHealthChanged?.Invoke(Instance.CurrentHealth, MaxHealth);
            }
        }
        public float MaxHealth => Instance.MaxHealth;
        public float Strength => Instance.Strength;
        public float Defense => Instance.Defence;
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

        }

        private void OnEnable()
        {

        }

        private void OnDisable()
        {

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

            if (Input.GetKeyDown(KeyCode.C))
            {
                WeaponController.ChangeNextWeapon();
                SaveWeaponSlotsToInstance();
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

            if (InputController.InteractInput)
            {
                Debug.Log("누름");
                Vector3 origin = transform.position + (Vector3.up * 1.2f);
                if (Physics.Raycast(origin, transform.forward, out RaycastHit hitInfo, 5f, _interactLayer))
                {
                    Debug.Log("감지됨");
                    IInteraction inter = hitInfo.collider.GetComponentInParent<IInteraction>();
                    if(inter != null)
                    {
                        Debug.Log("존재함");
                        inter.Interaction();
                    }
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
            HP -= (damage - Defense);
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
            _playerCamera = mainCamera;
        }

        public void ChangeWeaponSlotOrder(List<WeaponSlot> weaponSlotList)
        {
            if (weaponSlotList == null)
                return;
            WeaponController.UnequipWeapon();
            WeaponController.ChangeWeaponSlotOrder(weaponSlotList);
            WeaponController.EquipWeapon();
            SaveWeaponSlotsToInstance();
        }

        public void PerfectDodgeSequence()
        {
            TriggerPerfectDodgeTimeScale();
            TrailEffector.PerfactDodgeMeshTrailEffectOn(DodgeCounterDuration);
            OnPerfectDodge?.Invoke(DodgeCounterDuration);
        }

        public void InitializePlayer(
            GameRunContext context,
            PlayerInstance instance,
            Camera camera)
        {
            Instance = instance;
            CacheComponents();
            BindManagers(context.SaveDataManager, context.InputModeManager);
            BindInputStateEvent(context.InputModeManager);
            SyncWeaponSlotsWithInstance();
            BindCameraReference(camera);
            BindComponentEvents();
            InitializeFSM();
            _isInitialized = true;
        }

        private void SyncWeaponSlotsWithInstance()
        {
            if (_playerInstance == null || _weaponController == null)
            {
                return;
            }

            if (_playerInstance.WeaponSlots != null && _playerInstance.WeaponSlots.Count > 0)
            {
                _weaponController.InitializeWeaponSlots(_playerInstance.WeaponSlots, _playerInstance.WeaponIndex);
                return;
            }

            _playerInstance.SetWeaponSlots(_weaponController.GetWeaponSlotOrder(), _weaponController.WeaponIndex);
        }

        private void SaveWeaponSlotsToInstance()
        {
            if (_playerInstance == null || _weaponController == null)
            {
                return;
            }

            _playerInstance.SetWeaponSlots(_weaponController.GetWeaponSlotOrder(), _weaponController.WeaponIndex);
        }

        private void CacheComponents()
        {
            TryGetComponent(out _inputController);
            TryGetComponent(out _avatarMover);
            TryGetComponent(out _hitController);
            TryGetComponent(out _targetingController);
            TryGetComponent(out _attackEffectController);
            TryGetComponent(out _skillController);
            TryGetComponent(out _weaponController);
            TryGetComponent(out _animator);
            TryGetComponent(out _cinemachineImpulseSource);
            TryGetComponent(out _trailEffector);
        }

        private void BindManagers(SaveDataManager saveDataManager, InputModeManager inputModeManager)
        {
            _saveDataManager = saveDataManager;
            _inputModeManager = inputModeManager;
        }

        private void BindComponentEvents()
        {
            _skillController.OnQSkillEquiped += QSkillChange;
        }

        private void BindInputStateEvent(InputModeManager inputModeManager)
        {
            inputModeManager.OnChangeInputState += BlockInput;
        }

        private void InitializeFSM()
        {
            _fsm = new StateMachine(this);
            _fsm.InitStateMachine(_fsm.IdleState);
        }

        private void BlockInput(InputState state)
        {
            if(state == InputState.Combat)
            {
                InputController.BlockInput = false;
            }
            else if(state == InputState.UI)
            {
                InputController.BlockInput = true;
            }
            else if(state == InputState.NodeMap)
            {
                InputController.BlockInput = true;
            }
        }
    }
}
