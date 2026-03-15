using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ComboAttackData))]
public class ComboAttackDataEditor : Editor
{
    private const float PreviewHeight = 280f;
    private const float TimelineHeight = 44f;
    private const float TimelineHandleWidth = 10f;
    private const float TimelinePadding = 12f;
    private const float MinZoomFactor = 1.2f;
    private const float MaxZoomFactor = 8f;

    private enum TimelineDragMode
    {
        None,
        CurrentTime,
        StartHandle,
        EndHandle
    }

    private enum PreviewDragMode
    {
        None,
        Orbit
    }

    private SerializedProperty _animationClipProperty;
    private SerializedProperty _previewModelPrefabProperty;
    private SerializedProperty _comboInputStartNormalizedTimeProperty;
    private SerializedProperty _comboInputEndNormalizedTimeProperty;
    private GUIStyle _timelineCenterLabelStyle;
    private GUIStyle _timelineRightLabelStyle;

    private PreviewRenderUtility _previewRenderUtility;
    private GameObject _previewInstance;
    private GameObject _previewSource;
    private Bounds _previewBounds;
    private float _currentNormalizedTime;
    private float _lastEditorTime;
    private bool _isPlaying;
    private TimelineDragMode _dragMode;
    private PreviewDragMode _previewDragMode;
    private Vector2 _previewOrbit = new Vector2(18f, 180f);
    private float _previewZoomFactor = 3.6f;

    private void OnEnable()
    {
        _animationClipProperty = serializedObject.FindProperty("_animationClip");
        _previewModelPrefabProperty = serializedObject.FindProperty("_previewModelPrefab");
        _comboInputStartNormalizedTimeProperty = serializedObject.FindProperty("_comboInputStartNormalizedTime");
        _comboInputEndNormalizedTimeProperty = serializedObject.FindProperty("_comboInputEndNormalizedTime");

        _currentNormalizedTime = Mathf.Clamp01(_comboInputStartNormalizedTimeProperty.floatValue);
        _lastEditorTime = (float)EditorApplication.timeSinceStartup;
        _timelineCenterLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
        _timelineRightLabelStyle = new GUIStyle(EditorStyles.miniLabel);
        _timelineRightLabelStyle.alignment = TextAnchor.MiddleRight;
        _timelineRightLabelStyle.normal.textColor = EditorStyles.centeredGreyMiniLabel.normal.textColor;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        CleanupPreview();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_animationClipProperty);
        EditorGUILayout.PropertyField(_previewModelPrefabProperty);

        ClampWindowProperties();

        AnimationClip clip = _animationClipProperty.objectReferenceValue as AnimationClip;
        GameObject prefab = _previewModelPrefabProperty.objectReferenceValue as GameObject;

        EditorGUILayout.Space(8f);
        DrawTimingFields(clip);
        EditorGUILayout.Space(6f);

        if (clip == null || prefab == null)
        {
            EditorGUILayout.HelpBox("Assign an Animation Clip and a Preview Model Prefab to scrub the clip and define the combo input window below.", MessageType.Info);
        }
        else
        {
            DrawPreviewToolbar(clip);
            DrawPreviewArea(clip, prefab);
            DrawTimeline(clip);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawTimingFields(AnimationClip clip)
    {
        EditorGUILayout.LabelField("Combo Window", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        float start = _comboInputStartNormalizedTimeProperty.floatValue;
        float end = _comboInputEndNormalizedTimeProperty.floatValue;
        EditorGUILayout.MinMaxSlider(new GUIContent("Normalized Range"), ref start, ref end, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            _comboInputStartNormalizedTimeProperty.floatValue = start;
            _comboInputEndNormalizedTimeProperty.floatValue = Mathf.Max(start, end);
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(_comboInputStartNormalizedTimeProperty, new GUIContent("Start"));
        EditorGUILayout.PropertyField(_comboInputEndNormalizedTimeProperty, new GUIContent("End"));
        EditorGUILayout.EndHorizontal();

        ClampWindowProperties();

        if (clip != null)
        {
            float startSeconds = clip.length * _comboInputStartNormalizedTimeProperty.floatValue;
            float endSeconds = clip.length * _comboInputEndNormalizedTimeProperty.floatValue;
            EditorGUILayout.HelpBox(
                $"Clip Length: {clip.length:F3}s\n" +
                $"Combo Window: {startSeconds:F3}s - {endSeconds:F3}s",
                MessageType.None);
        }
    }

    private void DrawPreviewToolbar(AnimationClip clip)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button(_isPlaying ? "Pause" : "Play", EditorStyles.toolbarButton, GUILayout.Width(60f)))
        {
            _isPlaying = !_isPlaying;
            _lastEditorTime = (float)EditorApplication.timeSinceStartup;
        }

        if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(60f)))
        {
            _isPlaying = false;
            _currentNormalizedTime = 0f;
            Repaint();
        }

        if (GUILayout.Button("Frame", EditorStyles.toolbarButton, GUILayout.Width(60f)))
        {
            ResetPreviewCamera();
            Repaint();
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label($"Time {_currentNormalizedTime:F3}", EditorStyles.miniLabel);
        GUILayout.Space(8f);
        GUILayout.Label($"{_currentNormalizedTime * clip.length:F3}s", EditorStyles.miniLabel);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawPreviewArea(AnimationClip clip, GameObject prefab)
    {
        Rect previewRect = GUILayoutUtility.GetRect(10f, PreviewHeight, GUILayout.ExpandWidth(true));
        EnsurePreviewInstance(prefab);

        if (_previewRenderUtility == null || _previewInstance == null)
        {
            EditorGUI.HelpBox(previewRect, "Could not create a preview instance.", MessageType.Warning);
            return;
        }

        HandlePreviewInput(previewRect);
        SamplePreviewClip(clip, _currentNormalizedTime);
        DrawPreview(previewRect);

        GUI.Label(
            new Rect(previewRect.x + 8f, previewRect.y + 8f, previewRect.width - 16f, 18f),
            "Drag: orbit  Wheel: zoom  F: frame",
            EditorStyles.whiteMiniLabel);
    }

    private void DrawTimeline(AnimationClip clip)
    {
        Rect totalRect = GUILayoutUtility.GetRect(10f, TimelineHeight, GUILayout.ExpandWidth(true));
        Rect timelineRect = new Rect(
            totalRect.x + TimelinePadding,
            totalRect.y + 14f,
            totalRect.width - (TimelinePadding * 2f),
            12f);

        Event evt = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);

        float startTime = _comboInputStartNormalizedTimeProperty.floatValue;
        float endTime = _comboInputEndNormalizedTimeProperty.floatValue;

        Rect startHandleRect = GetHandleRect(timelineRect, startTime);
        Rect endHandleRect = GetHandleRect(timelineRect, endTime);
        Rect playheadRect = GetHandleRect(timelineRect, _currentNormalizedTime, 2f);

        HandleTimelineInput(evt, controlId, timelineRect, startHandleRect, endHandleRect);

        EditorGUI.DrawRect(timelineRect, new Color(0.15f, 0.15f, 0.15f));

        Rect comboWindowRect = new Rect(
            Mathf.Lerp(timelineRect.xMin, timelineRect.xMax, startTime),
            timelineRect.y,
            Mathf.Max(2f, Mathf.Lerp(timelineRect.xMin, timelineRect.xMax, endTime) - Mathf.Lerp(timelineRect.xMin, timelineRect.xMax, startTime)),
            timelineRect.height);
        EditorGUI.DrawRect(comboWindowRect, new Color(0.22f, 0.7f, 0.32f, 0.9f));

        EditorGUI.DrawRect(startHandleRect, new Color(0.95f, 0.75f, 0.2f));
        EditorGUI.DrawRect(endHandleRect, new Color(0.95f, 0.45f, 0.2f));
        EditorGUI.DrawRect(playheadRect, new Color(0.9f, 0.95f, 1f));

        GUI.Label(
            new Rect(totalRect.x + TimelinePadding, totalRect.y - 2f, totalRect.width - (TimelinePadding * 2f), 16f),
            "Timeline Scrub",
            EditorStyles.miniBoldLabel);

        GUI.Label(
            new Rect(timelineRect.xMin, timelineRect.yMax + 2f, 90f, 16f),
            "0.000",
            EditorStyles.miniLabel);
        GUI.Label(
            new Rect(timelineRect.center.x - 25f, timelineRect.yMax + 2f, 50f, 16f),
            "0.500",
            _timelineCenterLabelStyle);
        GUI.Label(
            new Rect(timelineRect.xMax - 50f, timelineRect.yMax + 2f, 50f, 16f),
            "1.000",
            _timelineRightLabelStyle);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Start To Current"))
        {
            _comboInputStartNormalizedTimeProperty.floatValue = Mathf.Min(_currentNormalizedTime, _comboInputEndNormalizedTimeProperty.floatValue);
        }

        if (GUILayout.Button("Set End To Current"))
        {
            _comboInputEndNormalizedTimeProperty.floatValue = Mathf.Max(_currentNormalizedTime, _comboInputStartNormalizedTimeProperty.floatValue);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            $"Current: {_currentNormalizedTime:F3} ({_currentNormalizedTime * clip.length:F3}s)\n" +
            $"Window: {_comboInputStartNormalizedTimeProperty.floatValue:F3} - {_comboInputEndNormalizedTimeProperty.floatValue:F3}",
            MessageType.None);
    }

    private void HandleTimelineInput(Event evt, int controlId, Rect timelineRect, Rect startHandleRect, Rect endHandleRect)
    {
        switch (evt.GetTypeForControl(controlId))
        {
            case EventType.MouseDown:
                if (evt.button != 0 || (!timelineRect.Contains(evt.mousePosition) && !startHandleRect.Contains(evt.mousePosition) && !endHandleRect.Contains(evt.mousePosition)))
                {
                    return;
                }

                GUIUtility.hotControl = controlId;
                _isPlaying = false;
                _dragMode = ResolveDragMode(evt.mousePosition, timelineRect, startHandleRect, endHandleRect);
                UpdateTimelineFromMouse(evt.mousePosition, timelineRect);
                evt.Use();
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl != controlId)
                {
                    return;
                }

                UpdateTimelineFromMouse(evt.mousePosition, timelineRect);
                evt.Use();
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl != controlId)
                {
                    return;
                }

                GUIUtility.hotControl = 0;
                _dragMode = TimelineDragMode.None;
                evt.Use();
                break;

            case EventType.Repaint:
                EditorGUIUtility.AddCursorRect(startHandleRect, MouseCursor.ResizeHorizontal, controlId);
                EditorGUIUtility.AddCursorRect(endHandleRect, MouseCursor.ResizeHorizontal, controlId);
                EditorGUIUtility.AddCursorRect(timelineRect, MouseCursor.SlideArrow, controlId);
                break;
        }
    }

    private TimelineDragMode ResolveDragMode(Vector2 mousePosition, Rect timelineRect, Rect startHandleRect, Rect endHandleRect)
    {
        if (startHandleRect.Contains(mousePosition))
        {
            return TimelineDragMode.StartHandle;
        }

        if (endHandleRect.Contains(mousePosition))
        {
            return TimelineDragMode.EndHandle;
        }

        return TimelineDragMode.CurrentTime;
    }

    private void UpdateTimelineFromMouse(Vector2 mousePosition, Rect timelineRect)
    {
        float normalized = Mathf.InverseLerp(timelineRect.xMin, timelineRect.xMax, mousePosition.x);
        normalized = Mathf.Clamp01(normalized);

        switch (_dragMode)
        {
            case TimelineDragMode.StartHandle:
                _comboInputStartNormalizedTimeProperty.floatValue = Mathf.Min(normalized, _comboInputEndNormalizedTimeProperty.floatValue);
                _currentNormalizedTime = _comboInputStartNormalizedTimeProperty.floatValue;
                serializedObject.ApplyModifiedProperties();
                break;

            case TimelineDragMode.EndHandle:
                _comboInputEndNormalizedTimeProperty.floatValue = Mathf.Max(normalized, _comboInputStartNormalizedTimeProperty.floatValue);
                _currentNormalizedTime = _comboInputEndNormalizedTimeProperty.floatValue;
                serializedObject.ApplyModifiedProperties();
                break;

            default:
                _currentNormalizedTime = normalized;
                break;
        }

        Repaint();
    }

    private Rect GetHandleRect(Rect timelineRect, float normalizedTime, float width = TimelineHandleWidth)
    {
        float x = Mathf.Lerp(timelineRect.xMin, timelineRect.xMax, normalizedTime) - (width * 0.5f);
        return new Rect(x, timelineRect.y - 4f, width, timelineRect.height + 8f);
    }

    private void EnsurePreviewInstance(GameObject prefab)
    {
        if (_previewRenderUtility == null)
        {
            _previewRenderUtility = new PreviewRenderUtility();
            _previewRenderUtility.camera.nearClipPlane = 0.01f;
            _previewRenderUtility.camera.farClipPlane = 1000f;
            _previewRenderUtility.lights[0].intensity = 1.1f;
            _previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);
            _previewRenderUtility.lights[1].intensity = 1f;
            _previewRenderUtility.lights[1].transform.rotation = Quaternion.Euler(340f, 218f, 177f);
            _previewRenderUtility.ambientColor = new Color(0.35f, 0.35f, 0.35f);
        }

        if (_previewInstance != null && _previewSource == prefab)
        {
            return;
        }

        DestroyPreviewInstance();

        _previewSource = prefab;
        _previewInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (_previewInstance == null)
        {
            _previewInstance = Instantiate(prefab);
        }

        if (_previewInstance == null)
        {
            return;
        }

        _previewInstance.hideFlags = HideFlags.HideAndDontSave;
        _previewRenderUtility.AddSingleGO(_previewInstance);
        _previewBounds = CalculateBounds(_previewInstance);
        ResetPreviewCamera();
    }

    private void DrawPreview(Rect rect)
    {
        _previewRenderUtility.BeginPreview(rect, GUIStyle.none);

        Camera camera = _previewRenderUtility.camera;
        PositionCamera(camera, _previewBounds);
        camera.Render();

        Texture texture = _previewRenderUtility.EndPreview();
        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
    }

    private void SamplePreviewClip(AnimationClip clip, float normalizedTime)
    {
        if (clip == null || _previewInstance == null)
        {
            return;
        }

        float clipTime = clip.length <= 0f ? 0f : Mathf.Clamp01(normalizedTime) * clip.length;
        clip.SampleAnimation(_previewInstance, clipTime);
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(root.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private void PositionCamera(Camera camera, Bounds bounds)
    {
        Vector3 center = bounds.center;
        float radius = Mathf.Max(bounds.extents.magnitude, 0.5f);
        Quaternion rotation = Quaternion.Euler(_previewOrbit.x, _previewOrbit.y, 0f);
        Vector3 direction = rotation * Vector3.forward;
        float distance = radius * _previewZoomFactor;

        camera.transform.position = center - (direction * distance) + new Vector3(0f, radius * 0.15f, 0f);
        camera.transform.rotation = rotation;
        camera.clearFlags = CameraClearFlags.Color;
        camera.backgroundColor = new Color(0.18f, 0.18f, 0.2f);
        camera.fieldOfView = 30f;
    }

    private void HandlePreviewInput(Rect previewRect)
    {
        Event evt = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);

        switch (evt.GetTypeForControl(controlId))
        {
            case EventType.MouseDown:
                if (evt.button != 0 || !previewRect.Contains(evt.mousePosition))
                {
                    return;
                }

                GUIUtility.hotControl = controlId;
                _previewDragMode = PreviewDragMode.Orbit;
                evt.Use();
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl != controlId || _previewDragMode != PreviewDragMode.Orbit)
                {
                    return;
                }

                _previewOrbit.y += evt.delta.x * 0.6f;
                _previewOrbit.x = Mathf.Clamp(_previewOrbit.x - (evt.delta.y * 0.5f), -80f, 80f);
                evt.Use();
                Repaint();
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl != controlId)
                {
                    return;
                }

                GUIUtility.hotControl = 0;
                _previewDragMode = PreviewDragMode.None;
                evt.Use();
                break;

            case EventType.ScrollWheel:
                if (!previewRect.Contains(evt.mousePosition))
                {
                    return;
                }

                ApplyZoom(evt.delta.y * 0.08f);
                evt.Use();
                Repaint();
                break;

            case EventType.KeyDown:
                if (evt.keyCode != KeyCode.F || !previewRect.Contains(evt.mousePosition))
                {
                    return;
                }

                ResetPreviewCamera();
                evt.Use();
                Repaint();
                break;

            case EventType.Repaint:
                EditorGUIUtility.AddCursorRect(previewRect, MouseCursor.Orbit, controlId);
                break;
        }
    }

    private void ApplyZoom(float delta)
    {
        _previewZoomFactor = Mathf.Clamp(_previewZoomFactor + delta, MinZoomFactor, MaxZoomFactor);
    }

    private void ResetPreviewCamera()
    {
        _previewOrbit = new Vector2(18f, 180f);
        _previewZoomFactor = 3.6f;
    }

    private void OnEditorUpdate()
    {
        if (!_isPlaying)
        {
            _lastEditorTime = (float)EditorApplication.timeSinceStartup;
            return;
        }

        AnimationClip clip = _animationClipProperty?.objectReferenceValue as AnimationClip;
        if (clip == null || clip.length <= 0f)
        {
            _isPlaying = false;
            return;
        }

        float now = (float)EditorApplication.timeSinceStartup;
        float deltaTime = now - _lastEditorTime;
        _lastEditorTime = now;

        _currentNormalizedTime += deltaTime / clip.length;
        if (_currentNormalizedTime > 1f)
        {
            _currentNormalizedTime -= Mathf.Floor(_currentNormalizedTime);
        }

        Repaint();
    }

    private void ClampWindowProperties()
    {
        _comboInputStartNormalizedTimeProperty.floatValue = Mathf.Clamp01(_comboInputStartNormalizedTimeProperty.floatValue);
        _comboInputEndNormalizedTimeProperty.floatValue = Mathf.Clamp01(_comboInputEndNormalizedTimeProperty.floatValue);

        if (_comboInputEndNormalizedTimeProperty.floatValue < _comboInputStartNormalizedTimeProperty.floatValue)
        {
            _comboInputEndNormalizedTimeProperty.floatValue = _comboInputStartNormalizedTimeProperty.floatValue;
        }
    }

    private void CleanupPreview()
    {
        DestroyPreviewInstance();

        if (_previewRenderUtility != null)
        {
            _previewRenderUtility.Cleanup();
            _previewRenderUtility = null;
        }
    }

    private void DestroyPreviewInstance()
    {
        if (_previewInstance != null)
        {
            DestroyImmediate(_previewInstance);
            _previewInstance = null;
        }
    }
}
