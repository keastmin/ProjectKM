using UnityEngine;

[CreateAssetMenu(fileName = "ComboAttackData", menuName = "Player/Combat/Combo Attack Data")]
public class ComboAttackData : ScriptableObject
{
    [Header("Animation")]
    [SerializeField] private AnimationClip _animationClip;
    [SerializeField] private GameObject _previewModelPrefab;

    [Header("Combo Window")]
    [SerializeField] [Range(0f, 1f)] private float _comboInputStartNormalizedTime = 0.35f;
    [SerializeField] [Range(0f, 1f)] private float _comboInputEndNormalizedTime = 0.7f;

    public AnimationClip AnimationClip => _animationClip;
    public GameObject PreviewModelPrefab => _previewModelPrefab;
    public float ComboInputStartNormalizedTime => _comboInputStartNormalizedTime;
    public float ComboInputEndNormalizedTime => _comboInputEndNormalizedTime;

    private void OnValidate()
    {
        _comboInputStartNormalizedTime = Mathf.Clamp01(_comboInputStartNormalizedTime);
        _comboInputEndNormalizedTime = Mathf.Clamp01(_comboInputEndNormalizedTime);

        if (_comboInputEndNormalizedTime < _comboInputStartNormalizedTime)
        {
            _comboInputEndNormalizedTime = _comboInputStartNormalizedTime;
        }
    }
}
