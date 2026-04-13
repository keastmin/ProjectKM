using UnityEngine;

public abstract class AttackTimingProfile : ScriptableObject
{
    public abstract AttackTimingDefinition[] AttackTimings { get; }
    public abstract AttackEffectTimingDefinition[] AttackEffectTimings { get; }
}
