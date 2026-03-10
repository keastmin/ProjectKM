using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(InputController))]
    [RequireComponent(typeof(CharacterMover))]
    [RequireComponent(typeof(Animator))]
    public class PlayerCore : MonoBehaviour
    {
        [Header("움직임")]
        [SerializeField] private float _jogSpeed = 5f;
        [SerializeField] private float _runSpeed = 8f;

        [Header("카메라")]
        [SerializeField] private Camera _playerCamera;

        private InputController _inputController;
        private CharacterMover _characterMover;
        private Animator _animator;

        private StateMachine _fsm;

        public StateMachine FSM => _fsm;
        public InputController InputController => _inputController;
        public CharacterMover CharacterMover => _characterMover;
        public Animator Animator => _animator;
        public float JogSpeed => _jogSpeed;
        public float RunSpeed => _runSpeed;
        public Camera PlayerCamera => _playerCamera;

        private void Awake()
        {
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
    }
}