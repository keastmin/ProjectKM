using UnityEngine;

public abstract class AttackData : ScriptableObject
{
    public abstract string Id { get; }
    public abstract string AnimationName { get; }
    public abstract float Damage { get; }
    public abstract AttackTimingProfile TimingProfile { get; }
}
