using Player;
using UnityEngine;

public abstract class SkillDefinition : ScriptableObject
{
    [SerializeField] private string _skillId;
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;
    [SerializeField] private float _cooldown;

    public string SkillId => _skillId;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public float Cooldown => _cooldown;

    public abstract SkillState CreateState(PlayerCore core, PlayerSkillSlot slot);

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(_skillId))
        {
            _skillId = name;
        }
    }
}
