using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public partial class CharacterMover : MonoBehaviour
    {
        [Header("콜라이더")]
        [SerializeField] private float _height = 2f;
        [SerializeField] private float _thickness = 1f;
        [SerializeField] private Vector3 _offset = Vector3.zero;

        [Header("계단")]
        [SerializeField] private float _stepUpHeight = 0.3f;

        private Rigidbody _rigidbody;
        private CapsuleCollider _collider;

        // 속도
        private Vector3 _inputVelocity = Vector3.zero;

        private void OnValidate()
        {
            InitComponents(); // 컴포넌트 초기화
            InitColliderDimensions(); // 콜라이더 초기화
        }

        private void Awake()
        {
            OnValidate();
        }

        private void FixedUpdate()
        {
            SetVelocity(_inputVelocity);
            ClearUpdate();
        }

        // 컴포넌트를 초기화하는 함수
        private void InitComponents()
        {
            // 리지드바디
            TryGetComponent(out _rigidbody);
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rigidbody.useGravity = false;
            _rigidbody.freezeRotation = true;

            // 캡슐 콜라이더
            TryGetComponent(out _collider);
        }

        // 캡슐 콜라이더의 높이, 두께를 초기화하는 함수
        private void InitColliderDimensions()
        {
            ColliderUtil.SetHeight(_collider, _height, _stepUpHeight, _offset);
            ColliderUtil.SetThickness(_collider, _thickness);
        }
    }
}