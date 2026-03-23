using UnityEngine;

namespace Player
{
    public class CameraDistanceDitherFade : MonoBehaviour
    {
        private static readonly int CameraFadeId = Shader.PropertyToID("_CameraFade");

        [Header("References")]
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private Transform _fadeOrigin;
        [SerializeField] private Renderer[] _targetRenderers;

        [Header("Distance Fade")]
        [SerializeField] private float _fadeStartDistance = 3.0f;
        [SerializeField] private float _fadeEndDistance = 0.8f;
        [SerializeField] private AnimationCurve _fadeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();

            if (_fadeOrigin == null)
            {
                _fadeOrigin = transform;
            }

            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_targetRenderers == null || _targetRenderers.Length == 0)
            {
                _targetRenderers = GetComponentsInChildren<Renderer>(true);
            }
        }

        private void LateUpdate()
        {
            if (_targetCamera == null || _fadeStartDistance <= _fadeEndDistance)
            {
                ApplyFade(0f);
                return;
            }

            float distance = Vector3.Distance(_targetCamera.transform.position, _fadeOrigin.position);
            float normalized = Mathf.InverseLerp(_fadeStartDistance, _fadeEndDistance, distance);
            float fade = Mathf.Clamp01(_fadeCurve.Evaluate(normalized));

            ApplyFade(fade);
        }

        private void ApplyFade(float fade)
        {
            if (_targetRenderers == null)
            {
                return;
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            for (int i = 0; i < _targetRenderers.Length; i++)
            {
                Renderer rendererComponent = _targetRenderers[i];
                if (rendererComponent == null)
                {
                    continue;
                }

                rendererComponent.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(CameraFadeId, fade);
                rendererComponent.SetPropertyBlock(_propertyBlock);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _fadeStartDistance = Mathf.Max(_fadeStartDistance, 0f);
            _fadeEndDistance = Mathf.Max(_fadeEndDistance, 0f);
        }
#endif
    }
}
