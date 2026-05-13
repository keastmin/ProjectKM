using System;
using UnityEngine;

[Serializable]
[EnemyStateBlock("Custom/Rotate To Player")]
public sealed class RotateToPlayerBlock : EnemyStateTimelineBlock
{
    [SerializeField] private float _rotateSpeed = 720f;

    public override string DisplayName => "Rotate To Player";

    public override EnemyStateBlockRunner CreateRunner()
    {
        return new Runner(this);
    }

    private sealed class Runner : EnemyStateBlockRunner
    {
        private readonly RotateToPlayerBlock _data;

        private EnemyCore _core;
        private Transform _root;

        public Runner(RotateToPlayerBlock data)
        {
            _data = data;
        }

        public override void OnStateEnter(EnemyStateRuntimeContext context)
        {
            _core = context.Core;
            _root = context.Root;
        }

        public override void OnBlockEnter(float normalizedTime)
        {
            Debug.Log("블록 시작");
        }

        public override void OnBlockTick(float normalizedTime)
        {
            if (_core == null || _root == null || _core.DetectedPlayer == null) return;

            Vector3 playerPos = _core.DetectedPlayer.transform.position;
            Vector3 enemyPos = _root.position;
            Vector3 dir = playerPos - enemyPos;
            dir.y = 0f;

            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            _root.rotation = Quaternion.RotateTowards(_root.rotation, targetRot, _data._rotateSpeed * Time.deltaTime);
        }

        public override void OnBlockExit(float normalizedTime)
        {
            Debug.Log("블록 끝");
        }

        public override void OnStateExit()
        {
            
        }
    }
}
