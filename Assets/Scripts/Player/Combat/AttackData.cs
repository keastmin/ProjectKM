using UnityEngine;

public abstract class AttackData : ScriptableObject
{
    public abstract string Id { get; }
    public abstract string AnimationName { get; }
    public abstract float DamageMagnification { get; }
    public abstract AttackTimingProfile TimingProfile { get; }
    public abstract AdditionalRootmotion AdditionalRootmotion { get; }
}
