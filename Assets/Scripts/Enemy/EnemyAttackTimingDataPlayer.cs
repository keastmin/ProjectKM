using Player;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackTimingDataPlayer
{
    private EnemyCore _core;
    private EnemyStateAuthoringAttackTimingBlock[] _blocks;

    private HashSet<IDamageable> _damageHash;

    public EnemyAttackTimingDataPlayer(EnemyCore core, EnemyStateAuthoringAttackTimingBlock[] blocks)
    {
        _core = core;
        _blocks = blocks;
        _damageHash = new HashSet<IDamageable>();
    }

    public void ClearDamageHashSet()
    {
        _damageHash.Clear();
    }

    public void GiveDamage(float normalizedTime, float damage, LayerMask layer, Transform model)
    {
        if (_core == null || _blocks == null || _blocks.Length == 0)
        {
            return;
        }

        float clampedNormalizedTime = Mathf.Clamp01(normalizedTime);
        foreach (EnemyStateAuthoringAttackTimingBlock block in _blocks)
        {
            if (block == null || !block.IsOpen(clampedNormalizedTime))
            {
                continue;
            }

            Quaternion rotation = Quaternion.Euler(block.RotationEuler);
            Vector3 center = block.PositionOffset;
            if (block.BindingMode == EnemyStateAuthoringDodgeAreaBindingMode.AttachToTransform)
            {
                Transform attachTransform = string.IsNullOrEmpty(block.AttachTransformPath)
                    ? _core.transform
                    : model.Find(block.AttachTransformPath);

                if (attachTransform == null)
                {
                    Debug.Log("attackTransform¿Ã æ¯¿Ω");
                    continue;
                }

                center = attachTransform.TransformPoint(block.PositionOffset);
                rotation = attachTransform.rotation * rotation;
            }

            Vector3 halfExtents = new Vector3(
                Mathf.Max(0.01f, block.Size.x),
                Mathf.Max(0.01f, block.Size.y),
                Mathf.Max(0.01f, block.Size.z)) * 0.5f;

            Collider[] overlappedColliders = Physics.OverlapBox(center, halfExtents, rotation, layer, QueryTriggerInteraction.Collide);
            foreach (Collider overlappedCollider in overlappedColliders)
            {
                if (overlappedCollider == null)
                {
                    continue;
                }

                IDamageable damageable = overlappedCollider.GetComponentInParent<IDamageable>();
                if (damageable == null || _damageHash.Contains(damageable))
                {
                    continue;
                }

                _damageHash.Add(damageable);
                damageable.TakeDamage(damage);
            }
        }
    }
}
