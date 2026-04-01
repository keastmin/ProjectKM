using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(PlayerInput))]
    public class InputController : MonoBehaviour
    {
        [SerializeField] private string _moveName;
        [SerializeField] private string _dodgeName;
        [SerializeField] private string _basicComboAttackName;

        private PlayerInput _pi;

        private InputAction _moveAction;
        private InputAction _dodgeAction;
        private InputAction _basicComboAttackAction;

        public Vector2 MoveInput { get; private set; }
        public bool DodgeInput { get; private set; }
        public bool BasicComboAttackInput { get; private set; }

        private void Awake()
        {
            TryGetComponent(out _pi);
            _moveAction = _pi.actions[_moveName];
            _dodgeAction = _pi.actions[_dodgeName];
            _basicComboAttackAction = _pi.actions[_basicComboAttackName];
        }

        private void Update()
        {
            DetectMoveInput();
            DetectDodgeInput();
            DetectBasicComboAttackInput();
        }

        private void DetectMoveInput()
        {
            MoveInput = _moveAction.ReadValue<Vector2>();
        }

        private void DetectDodgeInput()
        {
            DodgeInput = _dodgeAction.WasPressedThisFrame();
        }

        private void DetectBasicComboAttackInput()
        {
            BasicComboAttackInput = _basicComboAttackAction.WasPressedThisFrame();
        }
    }
}