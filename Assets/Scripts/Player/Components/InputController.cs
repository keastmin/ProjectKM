using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(PlayerInput))]
    public class InputController : MonoBehaviour
    {
        [SerializeField] private string _moveName;
        [SerializeField] private string _runName;

        private PlayerInput _pi;

        private InputAction _moveAction;
        private InputAction _runAction;

        public Vector2 MoveInput { get; private set; }
        public bool RunInput { get; private set; }

        private void Awake()
        {
            TryGetComponent(out _pi);
            _moveAction = _pi.actions[_moveName];
            _runAction = _pi.actions[_runName];
        }

        private void Update()
        {
            DetectMoveInput();
            DetectRunInput();
        }

        private void DetectMoveInput()
        {
            MoveInput = _moveAction.ReadValue<Vector2>();
        }

        private void DetectRunInput()
        {
            RunInput = _runAction.WasPressedThisFrame();
        }
    }
}