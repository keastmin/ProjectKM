using Player;
using UnityEngine;

[CreateAssetMenu(fileName = "CrucialStrike", menuName = "Player/Skill/Crucial Strike")]
public class CrucialStrikeDefinition : SkillDefinition
{
    public override SkillState CreateState(PlayerCore core, PlayerSkillSlot slot)
    {
        return new CrucialStrikeState(core, this, slot);
    }
}
