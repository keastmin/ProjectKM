using UnityEngine;

public class EnemyDodgeTimingDataPlayer
{
    private EnemyCore _core;
    private EnemyStateAuthoringDodgeTimingBlock[] _blocks;
    
    public EnemyDodgeTimingDataPlayer(EnemyCore core, EnemyStateAuthoringDodgeTimingBlock[] blocks)
    {
        _core = core;
        _blocks = blocks;
    }

    public void NotifyReciever(float normalizedTime)
    {
        if (_core == null || _blocks == null || _blocks.Length == 0)
        {
            return;
        }

        foreach (EnemyStateAuthoringDodgeTimingBlock block in _blocks)
        {
            if (block == null || !block.IsOpen(normalizedTime))
            {
                continue;
            }

            Quaternion rotation = Quaternion.Euler(block.RotationEuler);
            Vector3 center = block.PositionOffset;
            if (block.BindingMode == EnemyStateAuthoringDodgeAreaBindingMode.AttachToTransform)
            {
                Transform attachTransform = string.IsNullOrEmpty(block.AttachTransformPath)
                    ? _core.transform
                    : _core.transform.Find(block.AttachTransformPath);

                if (attachTransform == null)
                {
                    continue;
                }

                center = attachTransform.TransformPoint(block.PositionOffset);
                rotation = attachTransform.rotation * rotation;
            }

            Vector3 halfExtents = new Vector3(
                Mathf.Max(0.01f, block.Size.x),
                Mathf.Max(0.01f, block.Size.y),
                Mathf.Max(0.01f, block.Size.z)) * 0.5f;

            Collider[] overlappedColliders = Physics.OverlapBox(center, halfExtents, rotation, ~0, QueryTriggerInteraction.Collide);
            foreach (Collider overlappedCollider in overlappedColliders)
            {
                if (overlappedCollider == null)
                {
                    continue;
                }

                Player.PlayerCore playerCore = overlappedCollider.GetComponentInParent<Player.PlayerCore>();
                if (playerCore != null)
                {
                    playerCore.RecievePerfectDodgeInfo(_core);
                }
            }
        }
    }
}
