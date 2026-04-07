using NoiRC.SRMove;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(InputController))]
    [RequireComponent(typeof(CharacterMover))]
    [RequireComponent(typeof(HitController))]
    [RequireComponent(typeof(TargetingController))]
    [RequireComponent(typeof(AttackEffectController))]
    [RequireComponent(typeof(Animator))]
    public class PlayerCore : MonoBehaviour, IDamageable
    {
        [Header("무기")]
        [SerializeField] private GameObject _katana;

        [Header("움직임")]
        [SerializeField] private float _jogSpeed = 5f;
        [SerializeField] private float _runSpeed = 8f;

        [Header("콤보 공격")]
        [SerializeField] private BasicComboAttackData[] _katanaComboDatas;

        [Header("모션 워핑 데이터")]
        [SerializeField] private float _basicComboAttackMotionWarpSpeed = 20f;
        [SerializeField] private MotionWarpProfile _runTurnMotionInfo;

        [Header("카메라")]
        [SerializeField] private Camera _playerCamera;

        [Header("상태")]
        [SerializeField] private StateVariableContainter _stateVariables;

        // 컴포넌트
        private InputController _inputController;
        //private CharacterMover _characterMover;
        private AvatarMover _avatarMover;
        private HitController _hitController;
        private TargetingController _targetingController;
        private AttackEffectController _attackEffectController;
        private Animator _animator;

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
        public float BasicComboAttackMotionWarpSpeed => _basicComboAttackMotionWarpSpeed;
        public Camera PlayerCamera => _playerCamera;
        public MotionWarpProfile RunTurnMotionInfo => _runTurnMotionInfo;
        public GameObject Katana => _katana;
        public BasicComboAttackData[] KatanaComboDatas => _katanaComboDatas;
        public StateVariableContainter StateVariables => _stateVariables;

        public bool DamageFlag { get; set; } = false;

        private void Awake()
        {
            _katana.SetActive(false);

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
            // 완벽 회피가 아니면 언제든 데미지를 받을 수 있음
            if (DamageFlag)
            {
                if (StateVariables.DodgeVariable.IsPerfactDodge)
                {
                    DamageFlag = false;
                }
                else
                {
                    _fsm.Transition(_fsm.DamagedState);
                    return;
                }
            }

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
            DamageFlag = true;
        }

        private void OnDrawGizmos()
        {

        }

        private void OnDrawGizmosSelected()
        {
            DebugDodgeField(
                StateVariables.DodgeVariable.Debug,
                StateVariables.DodgeVariable.Radius,
                StateVariables.DodgeVariable.Height,
                StateVariables.DodgeVariable.Offset);
        }

        private void DebugDodgeField(bool isActive, float radius, float height, Vector3 offset)
        {
            if (!isActive)
                return;

            Gizmos.color = Color.purple;

            // offset을 로컬 기준으로 쓰고 싶으면 TransformPoint 사용
            Vector3 center = transform.TransformPoint(offset);

            Vector3 up = transform.up;
            Vector3 right = transform.right;
            Vector3 forward = transform.forward;

            // 캡슐 높이는 최소 지름 이상이어야 함
            float clampedHeight = Mathf.Max(height, radius * 2f);

            // 가운데 원기둥 부분 높이
            float cylinderHeight = clampedHeight - radius * 2f;
            float halfCylinder = cylinderHeight * 0.5f;

            Vector3 topCenter = center + up * halfCylinder;
            Vector3 bottomCenter = center - up * halfCylinder;

            // 위/아래 반구
            Gizmos.DrawWireSphere(topCenter, radius);
            Gizmos.DrawWireSphere(bottomCenter, radius);

            // 옆면 4개 라인
            Gizmos.DrawLine(topCenter + right * radius, bottomCenter + right * radius);
            Gizmos.DrawLine(topCenter - right * radius, bottomCenter - right * radius);
            Gizmos.DrawLine(topCenter + forward * radius, bottomCenter + forward * radius);
            Gizmos.DrawLine(topCenter - forward * radius, bottomCenter - forward * radius);
        }
    }
}