using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(InputController))]
    [RequireComponent(typeof(CharacterMover))]
    [RequireComponent(typeof(Animator))]
    public class PlayerCore : MonoBehaviour
    {
        [Header("무기")]
        [SerializeField] private GameObject _katana;

        [Header("움직임")]
        [SerializeField] private float _jogSpeed = 5f;
        [SerializeField] private float _runSpeed = 8f;

        [Header("모션 워핑 데이터")]
        [SerializeField] private MotionWarpProfile _runTurnMotionInfo;

        [Header("카메라")]
        [SerializeField] private Camera _playerCamera;

        // 컴포넌트
        private InputController _inputController;
        private CharacterMover _characterMover;
        private Animator _animator;

        // 속도
        private float _targetSpeed;
        private float _currentSpeed;

        // 상태머신
        private StateMachine _fsm;

        public StateMachine FSM => _fsm;
        public InputController InputController => _inputController;
        public CharacterMover CharacterMover => _characterMover;
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
        public Camera PlayerCamera => _playerCamera;
        public MotionWarpProfile RunTurnMotionInfo => _runTurnMotionInfo;
        public GameObject Katana => _katana;

        private void Awake()
        {
            _katana.SetActive(false);

            TryGetComponent(out _inputController);
            TryGetComponent(out _characterMover);
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
    }
}