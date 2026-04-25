using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "GeneralAttackData", menuName = "Player/Combat/General Attack Data")]
public class GeneralAttackData : AttackData
{
    [SerializeField] private string _id;
    [FormerlySerializedAs("AnimationName")]
    [SerializeField] private string _animationName;
    [FormerlySerializedAs("Damage")]
    [SerializeField] private float _damage = 10f;
    [FormerlySerializedAs("_timing")]
    [SerializeField] private AttackTimingProfile _timingProfile;
    [FormerlySerializedAs("MotionWarp")]
    [SerializeField] private AdditionalRootmotion _additionalRootmotion;

    public override string Id => _id;
    public override string AnimationName => _animationName;
    public override float Damage => _damage;
    public override AttackTimingProfile TimingProfile => _timingProfile;
    public override AdditionalRootmotion AdditionalRootmotion => _additionalRootmotion;

    private void OnValidate()
    {
        _id = _id ?? string.Empty;
        _animationName = _animationName ?? string.Empty;
        _damage = Mathf.Max(0f, _damage);
    }
}
