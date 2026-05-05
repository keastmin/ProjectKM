using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum EnemyStateAuthoringRootmotionSpace
{
    Local = 0,
    World = 1
}

public enum EnemyStateAuthoringDodgeAreaBindingMode
{
    World = 0,
    AttachToTransform = 1
}

[Serializable]
public sealed class EnemyStateAuthoringRootmotionBlock
{
    [SerializeField, Range(0f, 1f)] private float _startNormalizedTime;
    [SerializeField, Range(0f, 1f)] private float _endNormalizedTime = 0.2f;
    [SerializeField] private EnemyStateAuthoringRootmotionSpace _space = EnemyStateAuthoringRootmotionSpace.Local;
    [SerializeField] private bool _constrainToGroundPlane = true;
    [SerializeField] private Vector3 _direction = Vector3.forward;
    [SerializeField] private float _distance = 1f;
    [SerializeField] private AnimationCurve _speedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    public float StartNormalizedTime => _startNormalizedTime;
    public float EndNormalizedTime => _endNormalizedTime;
    public EnemyStateAuthoringRootmotionSpace Space => _space;
    public bool ConstrainToGroundPlane => _constrainToGroundPlane;
    public Vector3 Direction => _direction;
    public float Distance => _distance;
    public AnimationCurve SpeedCurve => _speedCurve;
    public Vector3 TargetDeltaPosition => GetTargetDeltaPosition();

    public void SetData(
        float startNormalizedTime,
        float endNormalizedTime,
        EnemyStateAuthoringRootmotionSpace space,
        bool constrainToGroundPlane,
        Vector3 direction,
        float distance,
        AnimationCurve speedCurve)
    {
        _startNormalizedTime = startNormalizedTime;
        _endNormalizedTime = endNormalizedTime;
        _space = space;
        _constrainToGroundPlane = constrainToGroundPlane;
        _direction = direction;
        _distance = distance;
        _speedCurve = speedCurve != null && speedCurve.length > 0
            ? new AnimationCurve(speedCurve.keys)
            : AnimationCurve.Linear(0f, 0f, 1f, 1f);

        Validate();
    }

    public Vector3 EvaluateCumulativeDelta(float normalizedTime, Quaternion localBasisRotation)
    {
        Vector3 delta = GetTargetDeltaPosition() * EvaluateCumulativeRatio(normalizedTime);
        if (_constrainToGroundPlane)
        {
            delta.y = 0f;
        }

        return _space == EnemyStateAuthoringRootmotionSpace.Local
            ? localBasisRotation * delta
            : delta;
    }

    public float EvaluateCumulativeRatio(float normalizedTime)
    {
        if (_endNormalizedTime <= _startNormalizedTime)
        {
            return normalizedTime >= _endNormalizedTime ? 1f : 0f;
        }

        if (normalizedTime <= _startNormalizedTime)
        {
            return 0f;
        }

        if (normalizedTime >= _endNormalizedTime)
        {
            return 1f;
        }

        float t = Mathf.InverseLerp(_startNormalizedTime, _endNormalizedTime, normalizedTime);
        return Mathf.Clamp01(_speedCurve.Evaluate(t));
    }

    public void Validate()
    {
        _startNormalizedTime = Mathf.Clamp01(_startNormalizedTime);
        _endNormalizedTime = Mathf.Clamp01(_endNormalizedTime);
        if (_endNormalizedTime < _startNormalizedTime)
        {
            _endNormalizedTime = _startNormalizedTime;
        }

        if (_direction == Vector3.zero)
        {
            _direction = Vector3.forward;
        }

        _distance = Mathf.Max(0f, _distance);
        NormalizeSpeedCurve();
    }

    private Vector3 GetTargetDeltaPosition()
    {
        return _direction == Vector3.zero ? Vector3.zero : _direction.normalized * _distance;
    }

    private void NormalizeSpeedCurve()
    {
        if (_speedCurve == null || _speedCurve.length < 2)
        {
            _speedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            _speedCurve.preWrapMode = WrapMode.ClampForever;
            _speedCurve.postWrapMode = WrapMode.ClampForever;
            return;
        }

        Keyframe[] keys = _speedCurve.keys;
        float previousValue = 0f;

        for (int i = 0; i < keys.Length; i++)
        {
            Keyframe key = keys[i];
            key.time = Mathf.Clamp01(key.time);
            key.value = Mathf.Clamp01(key.value);

            if (i == 0)
            {
                key.time = 0f;
                key.value = 0f;
            }
            else if (i == keys.Length - 1)
            {
                key.time = 1f;
                key.value = 1f;
            }
            else
            {
                key.value = Mathf.Max(previousValue, key.value);
            }

            previousValue = key.value;
            keys[i] = key;
        }

        _speedCurve.keys = keys;
        _speedCurve.preWrapMode = WrapMode.ClampForever;
        _speedCurve.postWrapMode = WrapMode.ClampForever;
    }
}

[Serializable]
public sealed class EnemyStateAuthoringDodgeTimingBlock
{
    [SerializeField, Range(0f, 1f)] private float _startNormalizedTime;
    [SerializeField, Range(0f, 1f)] private float _endNormalizedTime = 0.2f;
    [SerializeField] private EnemyStateAuthoringDodgeAreaBindingMode _bindingMode = EnemyStateAuthoringDodgeAreaBindingMode.World;
    [SerializeField] private string _attachTransformPath = string.Empty;
    [SerializeField] private Vector3 _positionOffset;
    [SerializeField] private Vector3 _rotationEuler;
    [SerializeField] private Vector3 _size = Vector3.one;

    public float StartNormalizedTime => _startNormalizedTime;
    public float EndNormalizedTime => _endNormalizedTime;
    public EnemyStateAuthoringDodgeAreaBindingMode BindingMode => _bindingMode;
    public string AttachTransformPath => _attachTransformPath;
    public Vector3 PositionOffset => _positionOffset;
    public Vector3 RotationEuler => _rotationEuler;
    public Vector3 Size => _size;

    public void SetData(
        float startNormalizedTime,
        float endNormalizedTime,
        EnemyStateAuthoringDodgeAreaBindingMode bindingMode,
        string attachTransformPath,
        Vector3 positionOffset,
        Vector3 rotationEuler,
        Vector3 size)
    {
        _startNormalizedTime = startNormalizedTime;
        _endNormalizedTime = endNormalizedTime;
        _bindingMode = bindingMode;
        _attachTransformPath = attachTransformPath ?? string.Empty;
        _positionOffset = positionOffset;
        _rotationEuler = rotationEuler;
        _size = size;

        Validate();
    }

    public bool IsOpen(float normalizedTime)
    {
        float clampedTime = Mathf.Clamp01(normalizedTime);
        return clampedTime >= _startNormalizedTime && clampedTime <= _endNormalizedTime;
    }

    public void Validate()
    {
        _startNormalizedTime = Mathf.Clamp01(_startNormalizedTime);
        _endNormalizedTime = Mathf.Clamp01(_endNormalizedTime);
        if (_endNormalizedTime < _startNormalizedTime)
        {
            _endNormalizedTime = _startNormalizedTime;
        }

        _attachTransformPath ??= string.Empty;
        _size = new Vector3(
            Mathf.Max(0.01f, _size.x),
            Mathf.Max(0.01f, _size.y),
            Mathf.Max(0.01f, _size.z));
    }
}

#if UNITY_EDITOR
public enum EnemyStateAuthoringActionBlockType
{
    AttackTiming,
    DodgeTiming,
    AdditionalRootmotion
}

[System.Serializable]
public sealed class EnemyStateAuthoringActionBlockPreviewData
{
    public EnemyStateAuthoringActionBlockType Type;
    public float StartTime;
    public float EndTime = 0.2f;
    public string Memo;
    public float PreviewValue = 1f;
    public bool PreviewRootmotion = true;
    public EnemyStateAuthoringRootmotionSpace RootmotionSpace = EnemyStateAuthoringRootmotionSpace.Local;
    public bool ConstrainRootmotionToGroundPlane = true;
    public Vector3 RootmotionDirection = Vector3.forward;
    public float RootmotionDistance = 1f;
    public AnimationCurve RootmotionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    public bool PreviewDodgeArea = true;
    public EnemyStateAuthoringDodgeAreaBindingMode DodgeAreaBindingMode = EnemyStateAuthoringDodgeAreaBindingMode.World;
    public string DodgeAttachTransformPath = string.Empty;
    public Vector3 DodgeAreaPositionOffset;
    public Vector3 DodgeAreaRotationEuler;
    public Vector3 DodgeAreaSize = Vector3.one;
}
#endif

[CreateAssetMenu(fileName = "EnemyStateAuthoring", menuName = "Enemy/Enemy State Authoring")]
public sealed class EnemyStateAuthoringAsset : ScriptableObject
{
    [SerializeField] private string _animatorStateName;
    [SerializeField] private EnemyStateAuthoringRootmotionBlock[] _additionalRootmotionBlocks = Array.Empty<EnemyStateAuthoringRootmotionBlock>();
    [SerializeField] private EnemyStateAuthoringDodgeTimingBlock[] _dodgeTimingBlocks = Array.Empty<EnemyStateAuthoringDodgeTimingBlock>();

    public string AnimatorStateName => _animatorStateName;
    public EnemyStateAuthoringRootmotionBlock[] AdditionalRootmotionBlocks => _additionalRootmotionBlocks;
    public EnemyStateAuthoringDodgeTimingBlock[] DodgeTimingBlocks => _dodgeTimingBlocks;

    public Vector3 EvaluateAdditionalRootmotionCumulativeDelta(float normalizedTime, Quaternion localBasisRotation)
    {
        Vector3 result = Vector3.zero;
        if (_additionalRootmotionBlocks == null)
        {
            return result;
        }

        float clampedTime = Mathf.Clamp01(normalizedTime);
        for (int i = 0; i < _additionalRootmotionBlocks.Length; i++)
        {
            EnemyStateAuthoringRootmotionBlock block = _additionalRootmotionBlocks[i];
            if (block == null)
            {
                continue;
            }

            result += block.EvaluateCumulativeDelta(clampedTime, localBasisRotation);
        }

        return result;
    }

#if UNITY_EDITOR
    [SerializeField, HideInInspector, FormerlySerializedAs("ModelAsset")]
    private GameObject _editorModelAsset;

    [SerializeField, HideInInspector, FormerlySerializedAs("AnimationClip")]
    private AnimationClip _editorAnimationClip;

    [SerializeField, HideInInspector, FormerlySerializedAs("ApplyRootMotion")]
    private bool _editorApplyRootMotion;

    [SerializeField, HideInInspector, FormerlySerializedAs("BackgroundColor")]
    private Color _editorBackgroundColor = new(0.16f, 0.17f, 0.19f, 1f);

    [SerializeField, HideInInspector, FormerlySerializedAs("ShowGrid")]
    private bool _editorShowGrid = true;

    [SerializeField, HideInInspector, FormerlySerializedAs("CameraTarget")]
    private Vector3 _editorCameraTarget = new(0f, 1f, 0f);

    [SerializeField, HideInInspector, FormerlySerializedAs("CameraDistance")]
    private float _editorCameraDistance = 5f;

    [SerializeField, HideInInspector, FormerlySerializedAs("CameraYaw")]
    private float _editorCameraYaw = 180f;

    [SerializeField, HideInInspector, FormerlySerializedAs("CameraPitch")]
    private float _editorCameraPitch = 12f;

    [SerializeField, HideInInspector, FormerlySerializedAs("AnimationNormalizedTime")]
    private float _editorAnimationNormalizedTime;

    [SerializeField, HideInInspector, FormerlySerializedAs("IsAnimationPlaying")]
    private bool _editorIsAnimationPlaying;

    [SerializeField, HideInInspector]
    private List<EnemyStateAuthoringActionBlockPreviewData> _editorActionBlocks = new();

    [SerializeField, HideInInspector]
    private int _editorSelectedActionBlockIndex = -1;

    public string EditorAnimatorStateName
    {
        get => _animatorStateName;
        set => _animatorStateName = value;
    }

    public GameObject EditorModelAsset
    {
        get => _editorModelAsset;
        set => _editorModelAsset = value;
    }

    public AnimationClip EditorAnimationClip
    {
        get => _editorAnimationClip;
        set => _editorAnimationClip = value;
    }

    public bool EditorApplyRootMotion
    {
        get => _editorApplyRootMotion;
        set => _editorApplyRootMotion = value;
    }

    public Color EditorBackgroundColor
    {
        get => _editorBackgroundColor;
        set => _editorBackgroundColor = value;
    }

    public bool EditorShowGrid
    {
        get => _editorShowGrid;
        set => _editorShowGrid = value;
    }

    public Vector3 EditorCameraTarget
    {
        get => _editorCameraTarget;
        set => _editorCameraTarget = value;
    }

    public float EditorCameraDistance
    {
        get => _editorCameraDistance;
        set => _editorCameraDistance = value;
    }

    public float EditorCameraYaw
    {
        get => _editorCameraYaw;
        set => _editorCameraYaw = value;
    }

    public float EditorCameraPitch
    {
        get => _editorCameraPitch;
        set => _editorCameraPitch = value;
    }

    public float EditorAnimationNormalizedTime
    {
        get => _editorAnimationNormalizedTime;
        set => _editorAnimationNormalizedTime = value;
    }

    public bool EditorIsAnimationPlaying
    {
        get => _editorIsAnimationPlaying;
        set => _editorIsAnimationPlaying = value;
    }

    public List<EnemyStateAuthoringActionBlockPreviewData> EditorActionBlocks
    {
        get
        {
            _editorActionBlocks ??= new List<EnemyStateAuthoringActionBlockPreviewData>();
            return _editorActionBlocks;
        }
    }

    public int EditorSelectedActionBlockIndex
    {
        get => _editorSelectedActionBlockIndex;
        set => _editorSelectedActionBlockIndex = value;
    }

    public void SyncActionBlocksFromEditor()
    {
        SyncAdditionalRootmotionBlocksFromEditor();
        SyncDodgeTimingBlocksFromEditor();
    }

    public void SyncAdditionalRootmotionBlocksFromEditor()
    {
        if (_editorActionBlocks == null)
        {
            _additionalRootmotionBlocks = Array.Empty<EnemyStateAuthoringRootmotionBlock>();
            return;
        }

        List<EnemyStateAuthoringRootmotionBlock> rootmotionBlocks = new();
        for (int i = 0; i < _editorActionBlocks.Count; i++)
        {
            EnemyStateAuthoringActionBlockPreviewData editorBlock = _editorActionBlocks[i];
            if (editorBlock == null || editorBlock.Type != EnemyStateAuthoringActionBlockType.AdditionalRootmotion)
            {
                continue;
            }

            EnemyStateAuthoringRootmotionBlock rootmotionBlock = new();
            rootmotionBlock.SetData(
                editorBlock.StartTime,
                editorBlock.EndTime,
                editorBlock.RootmotionSpace,
                editorBlock.ConstrainRootmotionToGroundPlane,
                editorBlock.RootmotionDirection,
                editorBlock.RootmotionDistance,
                editorBlock.RootmotionCurve);
            rootmotionBlocks.Add(rootmotionBlock);
        }

        _additionalRootmotionBlocks = rootmotionBlocks.ToArray();
    }

    private void SyncDodgeTimingBlocksFromEditor()
    {
        if (_editorActionBlocks == null)
        {
            _dodgeTimingBlocks = Array.Empty<EnemyStateAuthoringDodgeTimingBlock>();
            return;
        }

        List<EnemyStateAuthoringDodgeTimingBlock> dodgeTimingBlocks = new();
        for (int i = 0; i < _editorActionBlocks.Count; i++)
        {
            EnemyStateAuthoringActionBlockPreviewData editorBlock = _editorActionBlocks[i];
            if (editorBlock == null || editorBlock.Type != EnemyStateAuthoringActionBlockType.DodgeTiming)
            {
                continue;
            }

            EnemyStateAuthoringDodgeTimingBlock dodgeTimingBlock = new();
            dodgeTimingBlock.SetData(
                editorBlock.StartTime,
                editorBlock.EndTime,
                editorBlock.DodgeAreaBindingMode,
                editorBlock.DodgeAttachTransformPath,
                editorBlock.DodgeAreaPositionOffset,
                editorBlock.DodgeAreaRotationEuler,
                editorBlock.DodgeAreaSize);
            dodgeTimingBlocks.Add(dodgeTimingBlock);
        }

        _dodgeTimingBlocks = dodgeTimingBlocks.ToArray();
    }
#endif

    private void OnValidate()
    {
        if (_additionalRootmotionBlocks == null)
        {
            _additionalRootmotionBlocks = Array.Empty<EnemyStateAuthoringRootmotionBlock>();
        }

        for (int i = 0; i < _additionalRootmotionBlocks.Length; i++)
        {
            _additionalRootmotionBlocks[i] ??= new EnemyStateAuthoringRootmotionBlock();
            _additionalRootmotionBlocks[i].Validate();
        }

        if (_dodgeTimingBlocks == null)
        {
            _dodgeTimingBlocks = Array.Empty<EnemyStateAuthoringDodgeTimingBlock>();
        }

        for (int i = 0; i < _dodgeTimingBlocks.Length; i++)
        {
            _dodgeTimingBlocks[i] ??= new EnemyStateAuthoringDodgeTimingBlock();
            _dodgeTimingBlocks[i].Validate();
        }
    }
}
