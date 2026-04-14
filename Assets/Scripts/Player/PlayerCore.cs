using NoiRC.SRMove;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    [RequireComponent(typeof(InputController))]
    [RequireComponent(typeof(CharacterMover))]
    [RequireComponent(typeof(HitController))]
    [RequireComponent(typeof(TargetingController))]
    [RequireComponent(typeof(AttackEffectController))]
    [RequireComponent(typeof(Animator))]
    public class PlayerCore : MonoBehaviour, IDamageable, IDodgeTimingReceiver
    {
        [Header("움직임")]
        [SerializeField] private float _jogSpeed = 5f;
        [SerializeField] private float _runSpeed = 8f;

        [Header("공격")]
        [SerializeField] private AttackData[] _katanaComboDatas; // 콤보 공격
        [SerializeField] private AttackData _dodgeCounterData; // 회피 반격

        [Header("모션 워핑 데이터")]
        [SerializeField] private float _basicComboAttackMotionWarpSpeed = 20f;
        [SerializeField] private MotionWarpProfile _runTurnMotionInfo;

        [Header("카메라")]
        [SerializeField] private Camera _playerCamera;

        [Header("상태")]
        [SerializeField] private StateVariableContainter _stateVariables;

        [Header("Perfect Dodge")]
        [SerializeField] private float _perfectDodgeSlowTimeScale = 0.15f;
        [SerializeField] private float _perfectDodgeSlowDownDuration = 0.08f;
        [SerializeField] private float _perfectDodgeSlowHoldDuration = 2f;
        [SerializeField] private float _perfectDodgeRecoverDuration = 0.35f;

        // 컴포넌트
        private InputController _inputController;
        //private CharacterMover _characterMover;
        private AvatarMover _avatarMover;
        private HitController _hitController;
        private TargetingController _targetingController;
        private AttackEffectController _attackEffectController;
        private Animator _animator;
        private readonly HashSet<Component> _activeDodgeTimingSources = new();
        private Coroutine _perfectDodgeTimeScaleCoroutine;

        // 속도
        private float _targetSpeed;
        private float _currentSpeed;

        // 상태머신
        private StateMachine _fsm;

        public StateMachine FSM => _fsm;
        public InputController InputController => _inputController;
        //public CharacterMover CharacterMover => _characterMover;
        public AvatarMover Mover => _avatarMover;
        public HitController HitController => _hitController;
        public TargetingController TargetingController => _targetingController;
        public AttackEffectController AttackEffectController => _attackEffectController;
        public Animator Animator => _animator;
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
        public MotionWarpProfile RunTurnMotionInfo => _runTurnMotionInfo;
        public AttackData[] KatanaComboDatas => _katanaComboDatas;
        public AttackData DodgeCounterData => _dodgeCounterData;
        public StateVariableContainter StateVariables => _stateVariables;

        public bool DamageFlag { get; set; } = false;
        public bool CanReceiveDamage => _fsm?.CanReceiveDamage ?? true;

        private void Awake()
        {
            TryGetComponent(out _inputController);
            //TryGetComponent(out _characterMover);
            TryGetComponent(out _avatarMover);
            TryGetComponent(out _hitController);
            TryGetComponent(out _targetingController);
            TryGetComponent(out _attackEffectController);
            TryGetComponent(out _animator);
            _fsm = new StateMachine(this);
        }

        private void Start()
        {
            _fsm.InitStateMachine(_fsm.IdleState);
        }

        private void Update()
        {
            _fsm.Tick();
        }

        private void FixedUpdate()
        {
            SmoothSpeedChanger();
            _fsm.FixedTick();
        }

        private void LateUpdate()
        {
            _fsm.LateTick();
        }

        private void OnAnimatorMove()
        {
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
        }

        public void SetDodgeTimingActive(Component source, bool isActive)
        {
            if (source == null)
            {
                return;
            }

            if (isActive)
            {
                _activeDodgeTimingSources.Add(source);
            }
            else
            {
                _activeDodgeTimingSources.Remove(source);
            }

            StateVariables.DodgeVariable.CanPerfectDodge = _activeDodgeTimingSources.Count > 0;
        }

        public void TriggerPerfectDodgeTimeScale()
        {
            if (_perfectDodgeTimeScaleCoroutine != null)
            {
                StopCoroutine(_perfectDodgeTimeScaleCoroutine);
            }

            _perfectDodgeTimeScaleCoroutine = StartCoroutine(PerfectDodgeTimeScaleRoutine());
        }

        private IEnumerator PerfectDodgeTimeScaleRoutine()
        {
            const float normalTimeScale = 1f;
            float targetTimeScale = Mathf.Clamp(_perfectDodgeSlowTimeScale, 0.01f, normalTimeScale);

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
    }
}
