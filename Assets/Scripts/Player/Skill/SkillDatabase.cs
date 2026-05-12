using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillDatabase", menuName = "Player/Skill/Skill Database")]
public class SkillDatabase : ScriptableObject
{
    [SerializeField] private List<SkillDefinition> _skills = new();

    private Dictionary<string, SkillDefinition> _skillById;

    public bool TryGetSkill(string skillId, out SkillDefinition skill)
    {
        EnsureLookup();
        return _skillById.TryGetValue(skillId, out skill);
    }

    private void EnsureLookup()
    {
        if (_skillById != null)
        {
            return;
        }

        _skillById = new Dictionary<string, SkillDefinition>();

        for (int i = 0; i < _skills.Count; i++)
        {
            SkillDefinition skill = _skills[i];
            if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
            {
                continue;
            }

            if (!_skillById.ContainsKey(skill.SkillId))
            {
                _skillById.Add(skill.SkillId, skill);
            }
        }
    }

    private void OnValidate()
    {
        _skillById = null;
    }
}
