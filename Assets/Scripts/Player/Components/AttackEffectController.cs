using System.Collections.Generic;
using UnityEngine;

public class AttackEffectController : MonoBehaviour
{
    public struct AttackEffectData
    {
        public static AttackEffectData Empty => new AttackEffectData(null, null);
        public ParticleSystem AttackEffect;
        public Transform EffectSpawnTransform;
        public AttackEffectData(ParticleSystem particle, Transform spawnTransform)
        {
            AttackEffect = particle;
            EffectSpawnTransform = spawnTransform;
        }
    }

    [SerializeField] private EffectID[] _effectIDs;

    public Dictionary<string, AttackEffectData> EffectDic;

    private void Awake()
    {
        EffectDic = new Dictionary<string, AttackEffectData>();
        foreach (var e in _effectIDs)
        {
            EffectDic.Add(e.EffectName, new AttackEffectData(e.AttackEffect, e.SpawnTransform));
        }
    }

    public bool TryGetAttackEffectData(string effectId, out AttackEffectData effectData)
    {
        if (string.IsNullOrWhiteSpace(effectId))
        {
            effectData = AttackEffectData.Empty;
            return false;
        }

        return EffectDic.TryGetValue(effectId, out effectData);
    }
}
