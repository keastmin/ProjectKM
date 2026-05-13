using System;
using System.Collections.Generic;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EnemyStateBlockAttribute : Attribute
{
    public string MenuPath { get; }

    public EnemyStateBlockAttribute(string menuPath)
    {
        MenuPath = string.IsNullOrWhiteSpace(menuPath) ? "Custom Block" : menuPath;
    }
}

[Serializable]
public abstract class EnemyStateTimelineBlock
{
    [SerializeField, HideInInspector, Range(0f, 1f)] private float _startNormalizedTime;
    [SerializeField, HideInInspector, Range(0f, 1f)] private float _endNormalizedTime = 0.2f;

    public float StartNormalizedTime => _startNormalizedTime;
    public float EndNormalizedTime => _endNormalizedTime;
    public virtual string DisplayName => GetType().Name;

    public bool IsOpen(float normalizedTime)
    {
        float clampedTime = Mathf.Clamp01(normalizedTime);
        return clampedTime >= _startNormalizedTime && clampedTime <= _endNormalizedTime;
    }

    public void SetTime(float startNormalizedTime, float endNormalizedTime)
    {
        _startNormalizedTime = startNormalizedTime;
        _endNormalizedTime = endNormalizedTime;
        Validate();
    }

    public virtual void Validate()
    {
        _startNormalizedTime = Mathf.Clamp01(_startNormalizedTime);
        _endNormalizedTime = Mathf.Clamp01(_endNormalizedTime);
        if (_endNormalizedTime < _startNormalizedTime)
        {
            _endNormalizedTime = _startNormalizedTime;
        }
    }

    public abstract EnemyStateBlockRunner CreateRunner();
}

public abstract class EnemyStateBlockRunner
{
    public virtual void OnStateEnter(EnemyStateRuntimeContext context)
    {
    }

    public virtual void OnBlockEnter(float normalizedTime)
    {
    }

    public virtual void OnBlockTick(float normalizedTime)
    {
    }

    public virtual void OnBlockFixedTick(float normalizedTime)
    {
    }

    public virtual void OnBlockLateTick(float normalizedTime)
    {
    }

    public virtual void OnBlockExit(float normalizedTime)
    {
    }

    public virtual void OnStateExit()
    {
    }
}

public sealed class EnemyStateRuntimeContext
{
    public EnemyCore Core { get; }
    public Animator Animator { get; }
    public Rigidbody Rigidbody { get; }
    public Transform Root { get; }

    public EnemyStateRuntimeContext(EnemyCore core)
    {
        Core = core;
        Animator = core != null ? core.GetComponentInChildren<Animator>() : null;
        Rigidbody = core != null ? core.GetComponent<Rigidbody>() : null;
        Root = core != null ? core.transform : null;
    }

    public T GetComponent<T>() where T : Component
    {
        return Core != null ? Core.GetComponent<T>() : null;
    }

    public T GetComponentInChildren<T>() where T : Component
    {
        return Core != null ? Core.GetComponentInChildren<T>() : null;
    }
}

public sealed class EnemyStateCustomBlockPlayer
{
    private sealed class BlockRuntime
    {
        public EnemyStateTimelineBlock Block { get; }
        public EnemyStateBlockRunner Runner { get; }
        public bool IsOpen { get; set; }

        public BlockRuntime(EnemyStateTimelineBlock block, EnemyStateBlockRunner runner)
        {
            Block = block;
            Runner = runner;
        }
    }

    private readonly List<BlockRuntime> _runtimes = new();
    private float _lastNormalizedTime;

    public void Enter(EnemyStateAuthoringAsset asset, EnemyCore core)
    {
        Exit();

        if (asset == null || asset.CustomBlocks == null)
        {
            return;
        }

        EnemyStateRuntimeContext context = new(core);
        foreach (EnemyStateTimelineBlock block in asset.CustomBlocks)
        {
            if (block == null)
            {
                continue;
            }

            EnemyStateBlockRunner runner = block.CreateRunner();
            if (runner == null)
            {
                continue;
            }

            runner.OnStateEnter(context);
            _runtimes.Add(new BlockRuntime(block, runner));
        }
    }

    public void Tick(float normalizedTime)
    {
        float clampedTime = Mathf.Clamp01(normalizedTime);
        _lastNormalizedTime = clampedTime;

        for (int i = 0; i < _runtimes.Count; i++)
        {
            BlockRuntime runtime = _runtimes[i];
            bool shouldBeOpen = runtime.Block.IsOpen(clampedTime);

            if (shouldBeOpen)
            {
                if (!runtime.IsOpen)
                {
                    runtime.IsOpen = true;
                    runtime.Runner.OnBlockEnter(clampedTime);
                }

                runtime.Runner.OnBlockTick(clampedTime);
            }
            else if (runtime.IsOpen)
            {
                runtime.IsOpen = false;
                runtime.Runner.OnBlockExit(clampedTime);
            }
        }
    }

    public void FixedTick(float normalizedTime)
    {
        float clampedTime = Mathf.Clamp01(normalizedTime);

        for (int i = 0; i < _runtimes.Count; i++)
        {
            BlockRuntime runtime = _runtimes[i];
            if (runtime.IsOpen)
            {
                runtime.Runner.OnBlockFixedTick(clampedTime);
            }
        }
    }

    public void LateTick(float normalizedTime)
    {
        float clampedTime = Mathf.Clamp01(normalizedTime);

        for (int i = 0; i < _runtimes.Count; i++)
        {
            BlockRuntime runtime = _runtimes[i];
            if (runtime.IsOpen)
            {
                runtime.Runner.OnBlockLateTick(clampedTime);
            }
        }
    }

    public void Exit()
    {
        for (int i = 0; i < _runtimes.Count; i++)
        {
            BlockRuntime runtime = _runtimes[i];
            if (runtime.IsOpen)
            {
                runtime.IsOpen = false;
                runtime.Runner.OnBlockExit(_lastNormalizedTime);
            }

            runtime.Runner.OnStateExit();
        }

        _runtimes.Clear();
        _lastNormalizedTime = 0f;
    }
}
