using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "BasicComboAttackData", menuName = "Scriptable Objects/BasicComboAttackData")]
public class BasicComboAttackData : ScriptableObject
{
    [SerializeField] private string _id;
    [FormerlySerializedAs("AnimationName")]
    [SerializeField] private string _animationName;
    [FormerlySerializedAs("Damage")]
    [SerializeField] private float _damage = 10f;
    [FormerlySerializedAs("Timing")]
    [SerializeField] private ComboAttackData _timing;

    public string Id => _id;
    public string AnimationName => _animationName;
    public float Damage => _damage;
    public ComboAttackData Timing => _timing;

    private void OnValidate()
    {
        _id = _id ?? string.Empty;
        _animationName = _animationName ?? string.Empty;
        _damage = Mathf.Max(0f, _damage);
    }
}
