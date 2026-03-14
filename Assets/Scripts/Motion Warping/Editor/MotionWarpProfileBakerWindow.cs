using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MotionWarpProfileBakerWindow : EditorWindow
{
    private AnimationClip _clip;
    private GameObject _sourcePrefab;
    private MotionWarpProfile _targetProfile;

    private int _samplesPerSecond = 60;
    private bool _planarDistanceOnly = true;

    private readonly List<string> _motionTransformPaths = new List<string>();
    private readonly List<string> _motionTransformDisplayNames = new List<string>();
    private int _selectedMotionTransformIndex = 0;
    private string _selectedMotionTransformPath = string.Empty;

    private BakeResult _lastResult;
    private Vector2 _scroll;

    [MenuItem("Tools/Motion Warp/Profile Baker")]
    public static void Open()
    {
        GetWindow<MotionWarpProfileBakerWindow>("Motion Warp Baker");
    }

    private void OnEnable()
    {
        RebuildMotionTransformOptions();
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _clip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", _clip, typeof(AnimationClip), false);
        if (EditorGUI.EndChangeCheck())
        {
            _lastResult = null;
        }

        EditorGUI.BeginChangeCheck();
        _sourcePrefab = (GameObject)EditorGUILayout.ObjectField("Source Prefab", _sourcePrefab, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck())
        {
            RebuildMotionTransformOptions();
            _lastResult = null;
        }

        EditorGUILayout.Space(4);
        _samplesPerSecond = EditorGUILayout.IntSlider("Samples / Second", Mathf.Max(2, _samplesPerSecond), 2, 240);
        _planarDistanceOnly = EditorGUILayout.Toggle("Planar Distance Only", _planarDistanceOnly);

        EditorGUILayout.Space(4);
        DrawMotionTransformPopup();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Asset Output", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _targetProfile = (MotionWarpProfile)EditorGUILayout.ObjectField("Target Profile", _targetProfile, typeof(MotionWarpProfile), false);
        if (EditorGUI.EndChangeCheck() && _targetProfile != null)
        {
            LoadFromProfile(_targetProfile);
        }

        EditorGUILayout.Space(10);

        using (new EditorGUI.DisabledScope(!CanBake()))
        {
            if (GUILayout.Button("Analyze"))
            {
                try
                {
                    _lastResult = BakeCurrentSettings();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    EditorUtility.DisplayDialog("Motion Warp Baker", ex.Message, "OK");
                }
            }

            if (GUILayout.Button(_targetProfile == null ? "Bake To New Profile Asset" : "Bake Into Target Profile"))
            {
                try
                {
                    _lastResult = BakeCurrentSettings();

                    if (_targetProfile == null)
                    {
                        CreateNewAssetFromLastResult();
                    }
                    else
                    {
                        OverwriteExistingProfile(_targetProfile, _lastResult);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    EditorUtility.DisplayDialog("Motion Warp Baker", ex.Message, "OK");
                }
            }
        }

        EditorGUILayout.Space(10);
        DrawLastResultInfo();

        EditorGUILayout.EndScrollView();
    }

    private bool CanBake()
    {
        return _clip != null && _sourcePrefab != null;
    }

    private void DrawMotionTransformPopup()
    {
        if (_motionTransformDisplayNames.Count == 0)
        {
            EditorGUILayout.HelpBox("Select a Source Prefab first.", MessageType.Info);
            return;
        }

        int newIndex = EditorGUILayout.Popup("Motion Transform", _selectedMotionTransformIndex, _motionTransformDisplayNames.ToArray());
        if (newIndex != _selectedMotionTransformIndex)
        {
            _selectedMotionTransformIndex = newIndex;
            _selectedMotionTransformPath = _motionTransformPaths[_selectedMotionTransformIndex];
            _lastResult = null;
        }

        EditorGUILayout.HelpBox(
            "Choose which transform's sampled motion should be baked. Empty path means the prefab root itself.",
            MessageType.None);
    }

    private void DrawLastResultInfo()
    {
        if (_lastResult == null)
            return;

        EditorGUILayout.LabelField("Last Analysis", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            $"Clip Length: {_lastResult.clipLength:F3}s\n" +
            $"Total Delta Position (start space): {_lastResult.totalDeltaPositionStartSpace}\n" +
            $"Total Path Length: {_lastResult.totalPathLength:F4}\n" +
            $"Total Yaw Degrees: {_lastResult.totalYawDegrees:F3}",
            MessageType.Info);
    }

    private void LoadFromProfile(MotionWarpProfile profile)
    {
        if (profile == null)
            return;

        _clip = profile.clip;
        _sourcePrefab = profile.sourcePrefab;
        _samplesPerSecond = Mathf.Max(2, profile.samplesPerSecond);
        _planarDistanceOnly = profile.planarDistanceOnly;
        _selectedMotionTransformPath = profile.motionTransformPath ?? string.Empty;

        RebuildMotionTransformOptions();

        int idx = _motionTransformPaths.IndexOf(_selectedMotionTransformPath);
        _selectedMotionTransformIndex = idx >= 0 ? idx : 0;
        _selectedMotionTransformPath = _motionTransformPaths[_selectedMotionTransformIndex];
    }

    private void RebuildMotionTransformOptions()
    {
        string previousPath = _selectedMotionTransformPath;

        _motionTransformPaths.Clear();
        _motionTransformDisplayNames.Clear();

        _motionTransformPaths.Add(string.Empty);
        _motionTransformDisplayNames.Add("Root (self)");

        if (_sourcePrefab != null)
        {
            Transform root = _sourcePrefab.transform;
            CollectChildPathsRecursive(root, root);
        }

        int idx = _motionTransformPaths.IndexOf(previousPath);
        _selectedMotionTransformIndex = idx >= 0 ? idx : 0;
        _selectedMotionTransformPath = _motionTransformPaths[_selectedMotionTransformIndex];
    }

    private void CollectChildPathsRecursive(Transform root, Transform current)
    {
        foreach (Transform child in current)
        {
            string path = AnimationUtility.CalculateTransformPath(child, root);
            _motionTransformPaths.Add(path);
            _motionTransformDisplayNames.Add(path);
            CollectChildPathsRecursive(root, child);
        }
    }

    private BakeResult BakeCurrentSettings()
    {
        if (_clip == null)
            throw new InvalidOperationException("Animation Clip is missing.");

        if (_sourcePrefab == null)
            throw new InvalidOperationException("Source Prefab is missing.");

        if (_samplesPerSecond < 2)
            throw new InvalidOperationException("Samples / Second must be 2 or greater.");

        GameObject container = null;
        GameObject instance = null;

        try
        {
            container = new GameObject("__MotionWarpBakeContainer__")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            instance = InstantiateHiddenPrefab(_sourcePrefab);
            instance.transform.SetParent(container.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            Transform motionTransform = string.IsNullOrEmpty(_selectedMotionTransformPath)
                ? instance.transform
                : instance.transform.Find(_selectedMotionTransformPath);

            if (motionTransform == null)
            {
                throw new InvalidOperationException(
                    $"Could not find transform path '{_selectedMotionTransformPath}' on the instantiated prefab.");
            }

            int sampleCount = Mathf.Max(2, Mathf.CeilToInt(_clip.length * _samplesPerSecond) + 1);

            float[] normalizedTimes = new float[sampleCount];
            Vector3[] deltaPositionsStartSpace = new Vector3[sampleCount];
            float[] cumulativePathLengths = new float[sampleCount];
            float[] yawFromStartDegrees = new float[sampleCount];

            Vector3 startWorldPos = Vector3.zero;
            Quaternion startWorldRot = Quaternion.identity;

            for (int i = 0; i < sampleCount; i++)
            {
                float normalizedTime = sampleCount == 1 ? 0f : (float)i / (sampleCount - 1);
                float time = _clip.length * normalizedTime;

                _clip.SampleAnimation(instance, time);

                Vector3 sampledWorldPos = motionTransform.position;
                Quaternion sampledWorldRot = motionTransform.rotation;

                if (i == 0)
                {
                    startWorldPos = sampledWorldPos;
                    startWorldRot = sampledWorldRot;
                }

                Vector3 deltaWorld = sampledWorldPos - startWorldPos;
                Vector3 deltaStartSpace = Quaternion.Inverse(startWorldRot) * deltaWorld;

                Quaternion relativeRot = Quaternion.Inverse(startWorldRot) * sampledWorldRot;
                Vector3 relativeForward = relativeRot * Vector3.forward;
                Vector3 flatRelativeForward = Vector3.ProjectOnPlane(relativeForward, Vector3.up);

                float yaw = 0f;
                if (flatRelativeForward.sqrMagnitude > 0.000001f)
                {
                    yaw = Vector3.SignedAngle(Vector3.forward, flatRelativeForward.normalized, Vector3.up);
                }
                else if (i > 0)
                {
                    yaw = yawFromStartDegrees[i - 1];
                }

                normalizedTimes[i] = normalizedTime;
                deltaPositionsStartSpace[i] = deltaStartSpace;
                yawFromStartDegrees[i] = yaw;

                if (i == 0)
                {
                    cumulativePathLengths[i] = 0f;
                }
                else
                {
                    Vector3 step = deltaPositionsStartSpace[i] - deltaPositionsStartSpace[i - 1];
                    float distance = _planarDistanceOnly
                        ? Vector3.ProjectOnPlane(step, Vector3.up).magnitude
                        : step.magnitude;

                    cumulativePathLengths[i] = cumulativePathLengths[i - 1] + distance;
                }
            }

            float totalPathLength = cumulativePathLengths[sampleCount - 1];
            float totalYawDegrees = yawFromStartDegrees[sampleCount - 1];
            Vector3 totalDeltaPositionStartSpace = deltaPositionsStartSpace[sampleCount - 1];

            Keyframe[] moveProgressKeys = new Keyframe[sampleCount];
            Keyframe[] yawProgressKeys = new Keyframe[sampleCount];
            Keyframe[] posXKeys = new Keyframe[sampleCount];
            Keyframe[] posYKeys = new Keyframe[sampleCount];
            Keyframe[] posZKeys = new Keyframe[sampleCount];
            Keyframe[] yawKeys = new Keyframe[sampleCount];

            float absTotalYaw = Mathf.Abs(totalYawDegrees);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = normalizedTimes[i];

                float moveProgress = totalPathLength > 0.000001f
                    ? cumulativePathLengths[i] / totalPathLength
                    : 0f;

                float yawProgress = absTotalYaw > 0.000001f
                    ? Mathf.Abs(yawFromStartDegrees[i]) / absTotalYaw
                    : 0f;

                Vector3 p = deltaPositionsStartSpace[i];

                moveProgressKeys[i] = new Keyframe(t, moveProgress);
                yawProgressKeys[i] = new Keyframe(t, yawProgress);
                posXKeys[i] = new Keyframe(t, p.x);
                posYKeys[i] = new Keyframe(t, p.y);
                posZKeys[i] = new Keyframe(t, p.z);
                yawKeys[i] = new Keyframe(t, yawFromStartDegrees[i]);
            }

            return new BakeResult
            {
                clip = _clip,
                sourcePrefab = _sourcePrefab,
                motionTransformPath = _selectedMotionTransformPath,
                samplesPerSecond = _samplesPerSecond,
                planarDistanceOnly = _planarDistanceOnly,

                clipLength = _clip.length,
                totalDeltaPositionStartSpace = totalDeltaPositionStartSpace,
                totalPathLength = totalPathLength,
                totalYawDegrees = totalYawDegrees,

                moveProgress = new AnimationCurve(moveProgressKeys),
                yawProgress = new AnimationCurve(yawProgressKeys),
                deltaPosX = new AnimationCurve(posXKeys),
                deltaPosY = new AnimationCurve(posYKeys),
                deltaPosZ = new AnimationCurve(posZKeys),
                yawDegrees = new AnimationCurve(yawKeys),
            };
        }
        finally
        {
            if (container != null)
            {
                DestroyImmediate(container);
            }
        }
    }

    private static GameObject InstantiateHiddenPrefab(GameObject sourcePrefab)
    {
        GameObject instance = null;

        if (PrefabUtility.IsPartOfPrefabAsset(sourcePrefab))
        {
            instance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
        }

        if (instance == null)
        {
            instance = Instantiate(sourcePrefab);
        }

        instance.hideFlags = HideFlags.HideAndDontSave;
        return instance;
    }

    private void CreateNewAssetFromLastResult()
    {
        if (_lastResult == null)
            throw new InvalidOperationException("There is no bake result to save.");

        string suggestedName = $"{_clip.name}_MotionWarpProfile.asset";
        string assetPath = EditorUtility.SaveFilePanelInProject(
            "Create Motion Warp Profile",
            suggestedName,
            "asset",
            "Choose a location for the MotionWarpProfile asset.");

        if (string.IsNullOrEmpty(assetPath))
            return;

        MotionWarpProfile profile = CreateInstance<MotionWarpProfile>();
        ApplyBakeResultToProfile(profile, _lastResult);

        AssetDatabase.CreateAsset(profile, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _targetProfile = profile;
        Selection.activeObject = profile;
        EditorGUIUtility.PingObject(profile);
    }

    private void OverwriteExistingProfile(MotionWarpProfile profile, BakeResult result)
    {
        if (profile == null)
            throw new InvalidOperationException("Target profile is missing.");

        Undo.RecordObject(profile, "Bake Motion Warp Profile");
        ApplyBakeResultToProfile(profile, result);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = profile;
        EditorGUIUtility.PingObject(profile);
    }

    private static void ApplyBakeResultToProfile(MotionWarpProfile profile, BakeResult result)
    {
        profile.clip = result.clip;
        profile.sourcePrefab = result.sourcePrefab;
        profile.motionTransformPath = result.motionTransformPath;
        profile.samplesPerSecond = result.samplesPerSecond;
        profile.planarDistanceOnly = result.planarDistanceOnly;

        profile.clipLength = result.clipLength;
        profile.totalDeltaPositionStartSpace = result.totalDeltaPositionStartSpace;
        profile.totalPathLength = result.totalPathLength;
        profile.totalYawDegrees = result.totalYawDegrees;

        profile.moveProgress = result.moveProgress;
        profile.yawProgress = result.yawProgress;
        profile.deltaPosX = result.deltaPosX;
        profile.deltaPosY = result.deltaPosY;
        profile.deltaPosZ = result.deltaPosZ;
        profile.yawDegrees = result.yawDegrees;
    }

    private sealed class BakeResult
    {
        public AnimationClip clip;
        public GameObject sourcePrefab;
        public string motionTransformPath;
        public int samplesPerSecond;
        public bool planarDistanceOnly;

        public float clipLength;
        public Vector3 totalDeltaPositionStartSpace;
        public float totalPathLength;
        public float totalYawDegrees;

        public AnimationCurve moveProgress;
        public AnimationCurve yawProgress;
        public AnimationCurve deltaPosX;
        public AnimationCurve deltaPosY;
        public AnimationCurve deltaPosZ;
        public AnimationCurve yawDegrees;
    }
}
