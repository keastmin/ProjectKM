using Player;
using UnityEngine;

public class StateBase
{
    protected PlayerCore _core;
    public virtual bool CanReceiveDamage => true;

    public StateBase(PlayerCore core)
    {
        _core = core;
    }

    public virtual void Enter()
    {

    }

    public virtual void Tick()
    {

    }

    public virtual void FixedTick()
    {

    }

    public virtual void LateTick()
    {

    }

    public virtual void AnimationTick()
    {

    }

    public virtual void Exit()
    {

    }
}
