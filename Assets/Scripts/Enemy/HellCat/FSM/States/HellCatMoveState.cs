using UnityEngine;

public class HellCatMoveState : IState
{
    private HellCatCore _core;

    private int _leftWalkAnimHash;
    private int _rightWalkAnimHash;
    private int _forwardWalkAnimHash;
    private int _backwardWalkAnimHash;

    private Collider _playerCollider;
    private bool _hasPlayer;
    private Quaternion _targetRotation;
    private float _distanceToPlayer;
    private float _preAttackStrafeTimer;
    private float _strafeDirectionTimer;
    private int _strafeDirection = 1;
    private int _currentAnimHash;

    public HellCatMoveState(HellCatCore core)
    {
        _core = core;
        _leftWalkAnimHash = Animator.StringToHash("Base Layer." + core.LeftWalkStateData.AnimationName);
        _rightWalkAnimHash = Animator.StringToHash("Base Layer." + core.RightWalkStateData.AnimationName);
        _forwardWalkAnimHash = Animator.StringToHash("Base Layer." + core.ForwardWalkStateData.AnimationName);
        _backwardWalkAnimHash = Animator.StringToHash("Base Layer." + core.BackwardWalkStateData.AnimationName);
    }

    public void Enter()
    {
        _hasPlayer = false;
        _preAttackStrafeTimer = 0f;
        ResetStrafeDirection();
        PlayMoveAnimation(_forwardWalkAnimHash);
    }

    public void Tick()
    {
        if (_core.DamagedFlag)
        {
            _core.FSM.Transition(_core.FSM.DamagedState);
            return;
        }

        _hasPlayer = _core.IsPlayerInDetectRange(out _playerCollider);

        if (!_hasPlayer)
        {
            _core.FSM.Transition(_core.FSM.IdleState);
            return;
        }

        UpdateTargetRotation(_playerCollider);
        UpdateStrafeDirection();
        UpdatePreAttackTimer();

        //if (CanAttack())
        //{
        //    _core.FSM.Transition(_core.FSM.BasicAttackState);
        //    return;
        //}
    }

    public void FixedTick()
    {
        if (!_hasPlayer)
            return;

        RotateTowardsTarget();
        MoveAroundPlayer();
    }

    public void LateTick()
    {
    }

    public void AnimationTick()
    {
    }

    public void Exit()
    {
    }

    private void UpdateTargetRotation(Collider playerCollider)
    {
        Vector3 direction = playerCollider.transform.position - _core.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();
        _targetRotation = Quaternion.LookRotation(direction);
        _distanceToPlayer = Vector3.Distance(_core.transform.position, playerCollider.transform.position);
    }

    private void RotateTowardsTarget()
    {
        Quaternion nextRotation = Quaternion.RotateTowards(
            _core.Rigidbody.rotation,
            _targetRotation,
            _core.MoveRotationSpeed * Time.fixedDeltaTime);

        _core.Rigidbody.MoveRotation(nextRotation);
    }

    private void MoveAroundPlayer()
    {
        Vector3 toPlayer = _playerCollider.transform.position - _core.transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.0001f)
            return;

        Vector3 forwardToPlayer = toPlayer.normalized;
        Vector3 rightAroundPlayer = Vector3.Cross(Vector3.up, forwardToPlayer).normalized;
        Vector3 desiredMove = rightAroundPlayer * (_strafeDirection * _core.StrafeSpeed);

        if (_distanceToPlayer > _core.CombatDistance)
        {
            desiredMove += forwardToPlayer * _core.WalkSpeed;
        }
        else if (_distanceToPlayer < _core.AttackRange * 0.85f)
        {
            desiredMove -= forwardToPlayer * _core.RetreatSpeed;
        }

        if (desiredMove.sqrMagnitude < 0.0001f)
            return;

        Vector3 velocity = Vector3.ClampMagnitude(desiredMove, _core.WalkSpeed + _core.StrafeSpeed);
        _core.Rigidbody.MovePosition(_core.Rigidbody.position + velocity * Time.fixedDeltaTime);
        PlayMoveAnimation(SelectMoveAnimation(velocity));
    }

    private void UpdatePreAttackTimer()
    {
        if (_distanceToPlayer <= _core.AttackRange)
        {
            _preAttackStrafeTimer += Time.deltaTime;
        }
        else
        {
            _preAttackStrafeTimer = 0f;
        }
    }

    private void UpdateStrafeDirection()
    {
        _strafeDirectionTimer -= Time.deltaTime;

        if (_strafeDirectionTimer > 0f)
            return;

        ResetStrafeDirection();
    }

    private void ResetStrafeDirection()
    {
        _strafeDirection = Random.value < 0.5f ? -1 : 1;
        float interval = Mathf.Max(0.1f, _core.StrafeDirectionChangeInterval);
        _strafeDirectionTimer = Random.Range(interval * 0.7f, interval * 1.3f);
    }

    private bool CanAttack()
    {
        if (!_core.IsBasicAttackEnable || _distanceToPlayer > _core.AttackRange)
            return false;

        float angle = Quaternion.Angle(_core.Rigidbody.rotation, _targetRotation);
        return angle <= _core.AttackFacingAngle && _preAttackStrafeTimer >= _core.PreAttackStrafeTime;
    }

    private int SelectMoveAnimation(Vector3 velocity)
    {
        Vector3 localMove = _core.transform.InverseTransformDirection(velocity.normalized);

        if (Mathf.Abs(localMove.x) > Mathf.Abs(localMove.z))
        {
            return localMove.x < 0f ? _leftWalkAnimHash : _rightWalkAnimHash;
        }

        return localMove.z >= 0f ? _forwardWalkAnimHash : _backwardWalkAnimHash;
    }

    private void PlayMoveAnimation(int animHash)
    {
        if (_currentAnimHash == animHash)
            return;

        _currentAnimHash = animHash;
        _core.Animator.CrossFade(animHash, 0.08f, 0, 0f);
    }
}
