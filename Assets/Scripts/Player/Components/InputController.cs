using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(PlayerInput))]
    public class InputController : MonoBehaviour
    {
        [SerializeField] private string _moveName;

        private PlayerInput _pi;

        private InputAction _moveAction;

        public Vector2 MoveInput { get; private set; }

        private void Awake()
        {
            TryGetComponent(out _pi);
            _moveAction = _pi.actions[_moveName];
        }

        private void Update()
        {
            DetectMoveInput();
        }

        private void DetectMoveInput()
        {
            MoveInput = _moveAction.ReadValue<Vector2>();
        }
    }
}