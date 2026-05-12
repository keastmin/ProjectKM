using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    public class PlayerSkillController : MonoBehaviour
    {
        [SerializeField] private SkillDefinition _qSkill;
        [SerializeField] private SkillDefinition _eSkill;

        private readonly Dictionary<SkillDefinition, float> _cooldownRemainingBySkill = new();
        private readonly List<SkillDefinition> _cooldownUpdateBuffer = new();
        private readonly List<SkillDefinition> _completedCooldownBuffer = new();
        private PlayerCore _core;

        public SkillDefinition QSkill => _qSkill;
        public SkillDefinition ESkill => _eSkill;

        private void Awake()
        {
            TryGetComponent(out _core);
        }

        private void Update()
        {
            TickCooldowns();
        }

        public void Equip(PlayerSkillSlot slot, SkillDefinition skill)
        {
            if (slot == PlayerSkillSlot.Q)
            {
                _qSkill = skill;
                return;
            }

            _eSkill = skill;
        }

        public SkillDefinition GetEquippedSkill(PlayerSkillSlot slot)
        {
            return slot == PlayerSkillSlot.Q ? _qSkill : _eSkill;
        }

        public bool TryUseEquippedSkill(PlayerSkillSlot slot)
        {
            SkillDefinition skill = GetEquippedSkill(slot);
            if (skill == null || _core == null || IsCooldownRunning(skill))
            {
                return false;
            }

            SkillState state = skill.CreateState(_core, slot);
            if (state == null)
            {
                return false;
            }

            StartCooldown(skill);
            _core.FSM.Transition(state);
            return true;
        }

        public bool IsCooldownRunning(SkillDefinition skill)
        {
            return skill != null &&
                   _cooldownRemainingBySkill.TryGetValue(skill, out float remaining) &&
                   remaining > 0f;
        }

        public float GetCooldownRemaining(SkillDefinition skill)
        {
            if (skill == null)
            {
                return 0f;
            }

            return _cooldownRemainingBySkill.TryGetValue(skill, out float remaining) ? remaining : 0f;
        }

        public void StartCooldown(SkillDefinition skill)
        {
            if (skill == null || skill.Cooldown <= 0f)
            {
                return;
            }

            _cooldownRemainingBySkill[skill] = skill.Cooldown;
        }

        private void TickCooldowns()
        {
            if (_cooldownRemainingBySkill.Count == 0)
            {
                return;
            }

            _cooldownUpdateBuffer.Clear();
            _completedCooldownBuffer.Clear();

            foreach (KeyValuePair<SkillDefinition, float> pair in _cooldownRemainingBySkill)
            {
                _cooldownUpdateBuffer.Add(pair.Key);
            }

            for (int i = 0; i < _cooldownUpdateBuffer.Count; i++)
            {
                SkillDefinition skill = _cooldownUpdateBuffer[i];
                float remaining = _cooldownRemainingBySkill[skill] - Time.deltaTime;
                _cooldownRemainingBySkill[skill] = remaining;

                if (remaining <= 0f)
                {
                    _completedCooldownBuffer.Add(skill);
                }
            }

            for (int i = 0; i < _completedCooldownBuffer.Count; i++)
            {
                _cooldownRemainingBySkill.Remove(_completedCooldownBuffer[i]);
            }
        }
    }
}
