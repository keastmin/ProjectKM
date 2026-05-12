using Player;
using UnityEngine;

[CreateAssetMenu(fileName = "CrucialStrike", menuName = "Player/Skill/Crucial Strike")]
public class CrucialStrikeDefinition : SkillDefinition
{
    public override StateBase CreateState(PlayerCore core)
    {
        return new CrucialStrikeState(core);
    }
}
