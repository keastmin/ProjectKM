using System.Collections.Generic;
using UnityEngine;

public class EnemyNoticeDodgeTiming : MonoBehaviour
{
    private readonly HashSet<IDodgeTimingReceiver> _receivers = new();
    private Collider[] _triggerColliders;
    private void Awake()
    {
        _triggerColliders = GetComponents<Collider>();
    }
    private void OnEnable()
    {
        RefreshCurrentOverlaps();
    }

    private void OnDisable()
    {
        foreach (IDodgeTimingReceiver receiver in _receivers)
        {
            receiver.SetDodgeTimingActive(this, false);
        }


        _receivers.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        NotifyReceiver(other, true);

    }

    private void OnTriggerStay(Collider other)
    {
        NotifyReceiver(other, true);

    }

    private void OnTriggerExit(Collider other)
    {
        NotifyReceiver(other, false);
    }

    private void RefreshCurrentOverlaps()
    {
        if (_triggerColliders == null)
        {
            return;
        }

        foreach (Collider triggerCollider in _triggerColliders)
        {
            if (triggerCollider == null || !triggerCollider.enabled || !triggerCollider.isTrigger)
            {
                continue;
            }

            RefreshOverlaps(triggerCollider);
        }
    }

    private void RefreshOverlaps(Collider triggerCollider)
    {
        Bounds bounds = triggerCollider.bounds;
        Collider[] overlappedColliders = Physics.OverlapBox(
            bounds.center,
            bounds.extents,
            Quaternion.identity,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide);

        foreach (Collider overlappedCollider in overlappedColliders)
        {
            if (overlappedCollider == null || overlappedCollider == triggerCollider)
            {
                continue;
            }

            if (overlappedCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            NotifyReceiver(overlappedCollider, true);
        }
    }

    private void NotifyReceiver(Collider other, bool isActive)
    {
        IDodgeTimingReceiver receiver = other.GetComponentInParent(typeof(IDodgeTimingReceiver)) as IDodgeTimingReceiver;
        NotifyReceiver(receiver, isActive);
    }

    private void NotifyReceiver(IDodgeTimingReceiver receiver, bool isActive)
    {
        if (receiver == null)
        {
            return;
        }

        receiver.SetDodgeTimingActive(this, isActive);

        if (isActive)
        {
            _receivers.Add(receiver);
        }
        else
        {
            _receivers.Remove(receiver);
        }
    }
}
