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
        [SerializeField] private string _qSkillName;
        [SerializeField] private string _eSkillName;
        [SerializeField] private string _interactName;

        private PlayerInput _pi;

        private InputAction _moveAction;
        private InputAction _dodgeAction;
        private InputAction _basicComboAttackAction;
        private InputAction _qSkillAction;
        private InputAction _eSkillAction;
        private InputAction _interactAction;

        public Vector2 MoveInput { get; private set; }
        public bool DodgeInput { get; private set; }
        public bool BasicComboAttackInput { get; private set; }
        public bool QSkillInput { get; private set; }
        public bool ESkillInput { get; private set; }
        public bool InteractInput { get; private set; }

        public bool BlockInput = false;

        private void Awake()
        {
            TryGetComponent(out _pi);
            _moveAction = _pi.actions[_moveName];
            _dodgeAction = _pi.actions[_dodgeName];
            _basicComboAttackAction = _pi.actions[_basicComboAttackName];
            _qSkillAction = _pi.actions[_qSkillName];
            _eSkillAction = _pi.actions[_eSkillName];
            _interactAction = _pi.actions[_interactName];
            BlockInput = false;
        }

        private void Update()
        {
            if (BlockInput)
            {
                ResetInput();
                return;
            }

            DetectMoveInput();
            DetectDodgeInput();
            DetectBasicComboAttackInput();
            DetectQSkillInput();
            DetectESkillInput();
            DetectInteractInput();
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

        private void DetectQSkillInput()
        {
            QSkillInput = _qSkillAction.WasPressedThisFrame();
        }

        private void DetectESkillInput()
        {
            ESkillInput = _eSkillAction.WasPressedThisFrame();
        }

        private void DetectInteractInput()
        {
            InteractInput = _interactAction.WasPressedThisFrame();
        }

        private void ResetInput()
        {
            MoveInput = Vector2.zero;
            DodgeInput = false;
            BasicComboAttackInput = false;
            QSkillInput = false;
            ESkillInput = false;
            InteractInput = false;
        }
    }
}