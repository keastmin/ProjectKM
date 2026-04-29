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

    private const float RotateSpeed = 360f; // degree per second

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
    }

    public void Tick()
    {
        _hasPlayer = _core.IsPlayerInDetectRange(out _playerCollider);

        if (_hasPlayer)
        {
            UpdateTargetRotation(_playerCollider);
        }
    }

    public void FixedTick()
    {
        if (!_hasPlayer)
            return;

        RotateTowardsTarget();
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
    }

    private void RotateTowardsTarget()
    {
        Quaternion nextRotation = Quaternion.RotateTowards(
            _core.Rigidbody.rotation,
            _targetRotation,
            RotateSpeed * Time.fixedDeltaTime);

        _core.Rigidbody.MoveRotation(nextRotation);
    }
}
