using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum AdditionalRootmotionSpace
{
    Local = 0,
    World = 1
}

[Serializable]
public class AdditionalRootmotionSegment
{
    [SerializeField] private string _name = "Segment";
    [SerializeField] private bool _enabled = true;
    [SerializeField] [Range(0f, 1f)] private float _startNormalizedTime;
    [SerializeField] [Range(0f, 1f)] private float _endNormalizedTime = 1f;
    [SerializeField] private Vector3 _targetDeltaPosition = Vector3.forward;
    [SerializeField] private AnimationCurve _distributionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private Color _editorColor = new Color(0.35f, 0.75f, 1f, 1f);

    public string Name => _name;
    public bool Enabled => _enabled;
    public float StartNormalizedTime => _startNormalizedTime;
    public float EndNormalizedTime => _endNormalizedTime;
    public Vector3 TargetDeltaPosition => _targetDeltaPosition;
    public AnimationCurve DistributionCurve => _distributionCurve;
    public Color EditorColor => _editorColor;

    public void ImportLegacyData(string name, float startNormalizedTime, float endNormalizedTime, Vector3 targetDeltaPosition, AnimationCurve distributionCurve)
    {
        _name = name ?? string.Empty;
        _startNormalizedTime = startNormalizedTime;
        _endNormalizedTime = endNormalizedTime;
        _targetDeltaPosition = targetDeltaPosition;
        _distributionCurve = distributionCurve != null && distributionCurve.length > 0
            ? new AnimationCurve(distributionCurve.keys)
            : AnimationCurve.Linear(0f, 0f, 1f, 1f);

        Validate();
    }

    public void Validate()
    {
        _name ??= string.Empty;
        _startNormalizedTime = Mathf.Clamp01(_startNormalizedTime);
        _endNormalizedTime = Mathf.Clamp01(_endNormalizedTime);

        if (_endNormalizedTime < _startNormalizedTime)
        {
            _endNormalizedTime = _startNormalizedTime;
        }

        if (_editorColor.a <= 0f)
        {
            _editorColor = new Color(0.35f, 0.75f, 1f, 1f);
        }

        NormalizeDistributionCurve();
    }

    public float EvaluateCumulativeRatio(float normalizedTime)
    {
        if (!_enabled)
        {
            return 0f;
        }

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
        return Mathf.Clamp01(_distributionCurve.Evaluate(t));
    }

    private void NormalizeDistributionCurve()
    {
        if (_distributionCurve == null || _distributionCurve.length < 2)
        {
            _distributionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            _distributionCurve.preWrapMode = WrapMode.ClampForever;
            _distributionCurve.postWrapMode = WrapMode.ClampForever;
            return;
        }

        Keyframe[] keys = _distributionCurve.keys;
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

        _distributionCurve.keys = keys;
        _distributionCurve.preWrapMode = WrapMode.ClampForever;
        _distributionCurve.postWrapMode = WrapMode.ClampForever;
    }
}

[CreateAssetMenu(fileName = "AdditionalRootmotion", menuName = "Scriptable Objects/AdditionalRootmotion")]
public class AdditionalRootmotion : ScriptableObject
{
    [Header("Preview")]
    [SerializeField] private AnimationClip _previewAnimationClip;
    [SerializeField] private GameObject _previewModelPrefab;

    [Header("Settings")]
    [SerializeField] private AdditionalRootmotionSpace _space = AdditionalRootmotionSpace.Local;
    [SerializeField] private bool _constrainToGroundPlane = true;
    [SerializeField] private AdditionalRootmotionSegment[] _segments = Array.Empty<AdditionalRootmotionSegment>();

    [FormerlySerializedAs("TargetDeltaPosition")]
    [SerializeField] [HideInInspector] private Vector3 _legacyTargetDeltaPosition;
    [FormerlySerializedAs("TargetStartNormalTime")]
    [SerializeField] [HideInInspector] private float _legacyTargetStartNormalizedTime;
    [FormerlySerializedAs("TargetEndNormalTime")]
    [SerializeField] [HideInInspector] private float _legacyTargetEndNormalizedTime;
    [FormerlySerializedAs("AdditionalCurve")]
    [SerializeField] [HideInInspector] private AnimationCurve _legacyDistributionCurve;
    [SerializeField] [HideInInspector] private bool _legacyDataUpgraded;

    public AnimationClip PreviewAnimationClip => _previewAnimationClip;
    public GameObject PreviewModelPrefab => _previewModelPrefab;
    public AdditionalRootmotionSpace Space => _space;
    public bool ConstrainToGroundPlane => _constrainToGroundPlane;
    public AdditionalRootmotionSegment[] Segments => _segments;

    public Vector3 EvaluateCumulativeDelta(float normalizedTime)
    {
        Vector3 result = Vector3.zero;
        if (_segments == null)
        {
            return result;
        }

        for (int i = 0; i < _segments.Length; i++)
        {
            AdditionalRootmotionSegment segment = _segments[i];
            if (segment == null)
            {
                continue;
            }

            Vector3 delta = segment.TargetDeltaPosition * segment.EvaluateCumulativeRatio(normalizedTime);
            if (_constrainToGroundPlane)
            {
                delta.y = 0f;
            }

            result += delta;
        }

        return result;
    }

    public Vector3 EvaluateSegmentStartOffset(int segmentIndex)
    {
        Vector3 result = Vector3.zero;
        if (_segments == null)
        {
            return result;
        }

        int count = Mathf.Min(segmentIndex, _segments.Length);
        for (int i = 0; i < count; i++)
        {
            AdditionalRootmotionSegment segment = _segments[i];
            if (segment == null || !segment.Enabled)
            {
                continue;
            }

            Vector3 delta = segment.TargetDeltaPosition;
            if (_constrainToGroundPlane)
            {
                delta.y = 0f;
            }

            result += delta;
        }

        return result;
    }

    private void OnValidate()
    {
        UpgradeLegacyDataIfNeeded();

        if (_segments == null)
        {
            _segments = Array.Empty<AdditionalRootmotionSegment>();
            return;
        }

        for (int i = 0; i < _segments.Length; i++)
        {
            _segments[i] ??= new AdditionalRootmotionSegment();
            _segments[i].Validate();
        }
    }

    private void UpgradeLegacyDataIfNeeded()
    {
        if (_legacyDataUpgraded || (_segments != null && _segments.Length > 0))
        {
            return;
        }

        bool hasLegacyData =
            _legacyTargetDeltaPosition != Vector3.zero ||
            _legacyTargetStartNormalizedTime > 0f ||
            _legacyTargetEndNormalizedTime > 0f ||
            (_legacyDistributionCurve != null && _legacyDistributionCurve.length > 0);

        if (!hasLegacyData)
        {
            return;
        }

        AnimationCurve curveToUse = _legacyDistributionCurve != null && _legacyDistributionCurve.length > 0
            ? new AnimationCurve(_legacyDistributionCurve.keys)
            : AnimationCurve.Linear(0f, 0f, 1f, 1f);

        _segments = new[]
        {
            new AdditionalRootmotionSegment()
        };

        _segments[0].ImportLegacyData(
            "Legacy Segment",
            Mathf.Clamp01(_legacyTargetStartNormalizedTime),
            Mathf.Clamp01(Mathf.Max(_legacyTargetStartNormalizedTime, _legacyTargetEndNormalizedTime)),
            _legacyTargetDeltaPosition,
            curveToUse);

        _legacyDataUpgraded = true;
    }
}
