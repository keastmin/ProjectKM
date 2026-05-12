using System;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    public class PlayerSkillController : MonoBehaviour
    {
        [SerializeField] private SkillDefinition _qSkill;
        [SerializeField] private SkillDefinition _eSkill;

        public SkillDefinition QSkill => _qSkill;
        public SkillDefinition ESkill => _eSkill;

        public event Action<SkillDefinition> OnQSkillEquiped;

        private void Awake()
        {
            
        }

        private void Update()
        {
            
        }

        public bool IsEnableUseSkill(PlayerSkillSlot slot)
        {
            if(slot == PlayerSkillSlot.Q)
            {
                if (QSkill == null) return false;
            }

            return true;
        }

        public string GetCurrentEquipedSkillID(PlayerSkillSlot slot)
        {
            if (slot == PlayerSkillSlot.Q)
                return QSkill.SkillId;
            return "";
        }

        public void EquipSkill(PlayerSkillSlot slot, SkillDefinition skill)
        {
            if(slot == PlayerSkillSlot.Q)
            {
                _qSkill = skill;
                OnQSkillEquiped?.Invoke(skill);
            }
        }
    }
}
