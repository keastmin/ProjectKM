using UnityEngine;

[System.Serializable]
public class AttackTimingDefinition
{
    [SerializeField] private string _id;
    [SerializeField] [Range(0f, 1f)] private float _normalizedTime = 0.5f;

    public string Id => _id;
    public float NormalizedTime => _normalizedTime;

    public void Clamp()
    {
        _normalizedTime = Mathf.Clamp01(_normalizedTime);
        _id = _id ?? string.Empty;
    }
}

[CreateAssetMenu(fileName = "ComboAttackData", menuName = "Player/Combat/Combo Attack Data")]
public class ComboAttackData : ScriptableObject
{
    [Header("Animation")]
    [SerializeField] private AnimationClip _animationClip;
    [SerializeField] private GameObject _previewModelPrefab;

    [Header("Combo Window")]
    [SerializeField] [Range(0f, 1f)] private float _comboInputStartNormalizedTime = 0.35f;
    [SerializeField] [Range(0f, 1f)] private float _comboInputEndNormalizedTime = 0.7f;

    [Header("Attack Timings")]
    [SerializeField] private AttackTimingDefinition[] _attackTimings = new AttackTimingDefinition[0];

    public AnimationClip AnimationClip => _animationClip;
    public GameObject PreviewModelPrefab => _previewModelPrefab;
    public float ComboInputStartNormalizedTime => _comboInputStartNormalizedTime;
    public float ComboInputEndNormalizedTime => _comboInputEndNormalizedTime;
    public AttackTimingDefinition[] AttackTimings => _attackTimings;

    private void OnValidate()
    {
        _comboInputStartNormalizedTime = Mathf.Clamp01(_comboInputStartNormalizedTime);
        _comboInputEndNormalizedTime = Mathf.Clamp01(_comboInputEndNormalizedTime);

        if (_comboInputEndNormalizedTime < _comboInputStartNormalizedTime)
        {
            _comboInputEndNormalizedTime = _comboInputStartNormalizedTime;
        }

        if (_attackTimings == null)
        {
            _attackTimings = new AttackTimingDefinition[0];
            return;
        }

        for (int i = 0; i < _attackTimings.Length; i++)
        {
            if (_attackTimings[i] == null)
            {
                _attackTimings[i] = new AttackTimingDefinition();
            }

            _attackTimings[i].Clamp();
        }
    }
}
