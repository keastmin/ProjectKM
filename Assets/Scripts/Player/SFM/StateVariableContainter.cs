using System;
using UnityEngine;

[Serializable]
public class StateVariableContainter
{
    [SerializeField] private StateVariableDodge _dodgeVariable;

    public StateVariableDodge DodgeVariable => _dodgeVariable;
}
