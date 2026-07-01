using NUnit.Framework;
using UnityEngine;

public enum StrafingDirection
{
    Left,
    Right,
    Forward,
    Backward
}

public class HellCatStrafeState : IState
{
    private HellCatCore _core;

    private int _leftStrafeAnimHash;
    private int _rightStrafeAnimHash;
    private int _forwardStrafeAnimHash;
    private int _backStrafeAnimHash;

    private float _attackCooldown = 5f;
    private float _currentAttackCooldown = 0f;

    private Vector2 _strafeTimeRange;
    private float _currentStrafeTime;
    private float _targetStrafeTime;
    private StrafingDirection _strafingDirection;
    private Vector3 _normDir;

    public HellCatStrafeState(HellCatCore core)
    {
        _core = core;
        _leftStrafeAnimHash = Animator.StringToHash("Base Layer." + core.LeftStrafeStateData.AnimationName);
        _rightStrafeAnimHash = Animator.StringToHash("Base Layer." + core.RightStrafeStateData.AnimationName);
        _forwardStrafeAnimHash = Animator.StringToHash("Base Layer." + core.ForwardStrafeStateData.AnimationName);
        _backStrafeAnimHash = Animator.StringToHash("Base Layer." + core.BackwardStrafeStateData.AnimationName);
    } 

    public void Enter()
    {
        _strafingDirection = DecideStrafingDirection();

        _strafeTimeRange = new Vector2(1.5f, 2.5f);
        _currentStrafeTime = 0f;
        _targetStrafeTime = Random.Range(_strafeTimeRange.x, _strafeTimeRange.y);

        int hash = DecideAnimHash(_strafingDirection);
        _core.Animator.CrossFade(hash, 0.18f, 0, 0f);
    }

    public void Tick()
    {
        if (_core.IsDead)
        {
            _core.FSM.Transition(_core.FSM.DeadState);
            return;
        }

        _currentAttackCooldown += Time.deltaTime;

        if (_core.DamagedFlag)
        {
            _core.FSM.Transition(_core.FSM.DamagedState);
            return;
        }

        if(_currentAttackCooldown >= _attackCooldown)
        {
            _currentAttackCooldown = 0f;
            _core.FSM.Transition(_core.FSM.BiteAttackState);
            return;
        }

        if (_core.PlayerDistance > _core.ReChaseDistance)
        {
            _core.FSM.Transition(_core.FSM.ChaseState);
            return;
        }

        _currentStrafeTime += Time.deltaTime;
        if(_currentStrafeTime >= _targetStrafeTime)
        {
            var strafingDir = DecideStrafingDirection();
            if(strafingDir != _strafingDirection)
            {
                _strafingDirection = strafingDir;
                int hash = DecideAnimHash(_strafingDirection);
                _core.Animator.CrossFade(hash, 0.18f, 0, 0f);
            }

            _currentStrafeTime = 0f;
            _targetStrafeTime = Random.Range(_strafeTimeRange.x, _strafeTimeRange.y);
        }
    }

    public void FixedTick()
    {
        RotateTowardsPlayer();
        Strafe();
    }

    public void LateTick()
    {
        
    }

    public void AnimationTick()
    {

    }

    public void Exit()
    {
        _core.Rigidbody.linearVelocity = Vector3.zero;
    }

    private void RotateTowardsPlayer()
    {
        if(_core.DetectedPlayer != null)
        {
            _core.RequestModelRotationTowards(_core.DetectedPlayer.transform.position, 360f);
        }
    }

    private void Strafe()
    {
        _normDir = GetTargetNormalVector(_strafingDirection);
        _core.Rigidbody.linearVelocity = _normDir * _core.StrafeSpeed;
    }

    private StrafingDirection DecideStrafingDirection()
    {
        if(_core.DetectedPlayer != null)
        {
            var dist = _core.PlayerDistance;
            if(dist < _core.StrafingRange.x)
            {
                return StrafingDirection.Backward;
            }
            if(dist < _core.StrafingRange.y)
            {
                StrafingDirection[] randDir = new StrafingDirection[2];
                randDir[0] = StrafingDirection.Left; randDir[1] = StrafingDirection.Right;
                int randInt = Random.Range(0, 2);
                return randDir[randInt];
            }
        }

        return StrafingDirection.Forward;
    }

    private int DecideAnimHash(StrafingDirection dir)
    {
        int hash = _forwardStrafeAnimHash;
        if (dir == StrafingDirection.Left)
            hash = _leftStrafeAnimHash;
        else if (dir == StrafingDirection.Right)
            hash = _rightStrafeAnimHash;
        else if (dir == StrafingDirection.Backward)
            hash = _backStrafeAnimHash;
        return hash;
    }

    private Vector3 GetTargetNormalVector(StrafingDirection dir)
    {
        Vector3 normal = _core.FacingForward;
        if (dir == StrafingDirection.Left)
            normal = -_core.FacingRight;
        else if (dir == StrafingDirection.Right)
            normal = _core.FacingRight;
        else if (dir == StrafingDirection.Backward)
            normal = -_core.FacingForward;
        return normal;
    }
}
