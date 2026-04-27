using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "BasicComboAttackData", menuName = "Scriptable Objects/BasicComboAttackData")]
public class BasicComboAttackData : AttackData
{
    [SerializeField] private string _id;
    [FormerlySerializedAs("AnimationName")]
    [SerializeField] private string _animationName;
    [FormerlySerializedAs("Damage")]
    [SerializeField] private float _damageMagnification = 10f;
    [FormerlySerializedAs("Timing")]
    [SerializeField] private AttackTimingProfile _timing;
    [FormerlySerializedAs("MotionWarp")]
    [SerializeField] private AdditionalRootmotion _additionalRootmotion;

    public override string Id => _id;
    public override string AnimationName => _animationName;
    public override float DamageMagnification => _damageMagnification;
    public override AttackTimingProfile TimingProfile => _timing;
    public override AdditionalRootmotion AdditionalRootmotion => _additionalRootmotion;

    private void OnValidate()
    {
        _id = _id ?? string.Empty;
        _animationName = _animationName ?? string.Empty;
        _damageMagnification = Mathf.Max(0f, _damageMagnification);
    }
}
