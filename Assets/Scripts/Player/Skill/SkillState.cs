using Player;

public abstract class SkillState : StateBase
{
    protected readonly SkillDefinition _definition;
    protected readonly PlayerSkillSlot _slot;

    public SkillDefinition Definition => _definition;
    public PlayerSkillSlot Slot => _slot;

    protected SkillState(PlayerCore core, SkillDefinition definition, PlayerSkillSlot slot) : base(core)
    {
        _definition = definition;
        _slot = slot;
    }

    protected void TransitionToIdle()
    {
        _core.FSM.Transition(_core.FSM.IdleState);
    }
}
