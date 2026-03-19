using System;
using UnityEngine;

[Serializable]
public struct EffectID
{
    public string EffectName;
    public ParticleSystem AttackEffect;
    public Transform SpawnTransform;
}
