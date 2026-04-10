using System.Collections.Generic;
using UnityEngine;

public class EnemyNoticeDodgeTiming : MonoBehaviour
{
    private readonly HashSet<IDodgeTimingReceiver> _receivers = new();

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

    private void NotifyReceiver(Collider other, bool isActive)
    {
        IDodgeTimingReceiver receiver = other.GetComponentInParent(typeof(IDodgeTimingReceiver)) as IDodgeTimingReceiver;

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
