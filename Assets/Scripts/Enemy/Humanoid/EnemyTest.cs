using Jorjouto.AnimComposerSystem;
using System;
using UnityEngine;

public class EnemyTest : MonoBehaviour
{
    private AnimCoordinatorComponent _coord;

    public AnimCoordinatorComponent Coord => _coord;

    protected virtual void Awake()
    {
        TryGetComponent(out _coord);
    }
}
