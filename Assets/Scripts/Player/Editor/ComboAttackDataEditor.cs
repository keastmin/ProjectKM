using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ComboAttackData))]
public class ComboAttackDataEditor : Editor
{
    private const float PreviewHeight = 280f;
    private const float TimelineHeight = 64f;
    private const float TimelineHandleWidth = 10f;
    private const float TimelinePadding = 12f;
    private const float MinZoomFactor = 1.2f;
    private const float MaxZoomFactor = 8f;
    private const float AttackMarkerWidth = 2f;
    private const float EffectMarkerHeight = 8f;

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
        Orbit,
        Pan
    }

    private SerializedProperty _basicComboAttackIdProperty;
    private SerializedProperty _animationClipProperty;
    private SerializedProperty _previewModelPrefabProperty;
    private SerializedProperty _comboInputStartNormalizedTimeProperty;
    private SerializedProperty _comboInputEndNormalizedTimeProperty;
    private SerializedProperty _attackTimingsProperty;
    private SerializedProperty _attackEffectTimingsProperty;
    private GUIStyle _timelineCenterLabelStyle;
    private GUIStyle _timelineRightLabelStyle;
    private GUIStyle _attackMarkerLabelStyle;
    private GUIStyle _effectMarkerLabelStyle;

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
    private Vector3 _previewPivotOffset;

    private void OnEnable()
    {
        _basicComboAttackIdProperty = serializedObject.FindProperty("_basicComboAttackId");
        _animationClipProperty = serializedObject.FindProperty("_animationClip");
        _previewModelPrefabProperty = serializedObject.FindProperty("_previewModelPrefab");
        _comboInputStartNormalizedTimeProperty = serializedObject.FindProperty("_comboInputStartNormalizedTime");
        _comboInputEndNormalizedTimeProperty = serializedObject.FindProperty("_comboInputEndNormalizedTime");
        _attackTimingsProperty = serializedObject.FindProperty("_attackTimings");
        _attackEffectTimingsProperty = serializedObject.FindProperty("_attackEffectTimings");

        _currentNormalizedTime = Mathf.Clamp01(_comboInputStartNormalizedTimeProperty.floatValue);
        _lastEditorTime = (float)EditorApplication.timeSinceStartup;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        CleanupPreview();
    }

    public override void OnInspectorGUI()
    {
        EnsureTimelineStyles();

        serializedObject.Update();

        DrawBasicComboAttackLinkField();
        EditorGUILayout.Space(6f);
        EditorGUILayout.PropertyField(_animationClipProperty);
        EditorGUILayout.PropertyField(_previewModelPrefabProperty);

        ClampWindowProperties();

        AnimationClip clip = _animationClipProperty.objectReferenceValue as AnimationClip;
        GameObject prefab = _previewModelPrefabProperty.objectReferenceValue as GameObject;

        EditorGUILayout.Space(8f);
        DrawTimingFields(clip);
        EditorGUILayout.Space(6f);
        DrawAttackTimingFields(clip);
        EditorGUILayout.Space(6f);
        DrawAttackEffectTimingFields(clip);
        EditorGUILayout.Space(6f);

        if (clip == null || prefab == null)
        {
            EditorGUILayout.HelpBox("Assign an Animation Clip and a Preview Model Prefab to scrub the clip and define timing data below.", MessageType.Info);
        }
        else
        {
            DrawPreviewToolbar(clip);
            DrawPreviewArea(clip, prefab);
            DrawTimeline(clip);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void EnsureTimelineStyles()
    {
        if (_timelineCenterLabelStyle == null)
        {
            _timelineCenterLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
        }

        if (_timelineRightLabelStyle == null)
        {
            _timelineRightLabelStyle = new GUIStyle(EditorStyles.miniLabel);
            _timelineRightLabelStyle.alignment = TextAnchor.MiddleRight;
            _timelineRightLabelStyle.normal.textColor = EditorStyles.centeredGreyMiniLabel.normal.textColor;
        }

        if (_attackMarkerLabelStyle == null)
        {
            _attackMarkerLabelStyle = new GUIStyle(EditorStyles.miniLabel);
            _attackMarkerLabelStyle.alignment = TextAnchor.MiddleCenter;
            _attackMarkerLabelStyle.normal.textColor = new Color(1f, 0.7f, 0.45f);
        }

        if (_effectMarkerLabelStyle == null)
        {
            _effectMarkerLabelStyle = new GUIStyle(EditorStyles.miniLabel);
            _effectMarkerLabelStyle.alignment = TextAnchor.MiddleCenter;
            _effectMarkerLabelStyle.normal.textColor = new Color(0.45f, 0.85f, 1f);
        }
    }

    private void DrawBasicComboAttackLinkField()
    {
        EditorGUILayout.LabelField("Basic Combo Attack Link", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_basicComboAttackIdProperty, new GUIContent("Basic Combo Attack ID"));

        AttackData linkedAttackData = FindAttackDataById(_basicComboAttackIdProperty.stringValue);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Resolved Data", linkedAttackData, typeof(AttackData), false);
        }

        if (string.IsNullOrWhiteSpace(_basicComboAttackIdProperty.stringValue))
        {
            EditorGUILayout.HelpBox("Enter the ID of an existing AttackData asset.", MessageType.Info);
            return;
        }

        if (linkedAttackData == null)
        {
            EditorGUILayout.HelpBox("Could not find an AttackData asset with the given ID.", MessageType.Warning);
        }
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

    private void DrawAttackTimingFields(AnimationClip clip)
    {
        EditorGUILayout.LabelField("Attack Timings", EditorStyles.boldLabel);

        if (_attackTimingsProperty.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Add attack timing entries to define hit timings and their string IDs.", MessageType.None);
        }

        int removeIndex = -1;

        for (int i = 0; i < _attackTimingsProperty.arraySize; i++)
        {
            SerializedProperty element = _attackTimingsProperty.GetArrayElementAtIndex(i);
            SerializedProperty idProperty = element.FindPropertyRelative("_id");
            SerializedProperty timeProperty = element.FindPropertyRelative("_normalizedTime");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Attack Timing {i + 1}", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(idProperty, new GUIContent("ID"));
            timeProperty.floatValue = EditorGUILayout.Slider("Normalized Time", timeProperty.floatValue, 0f, 1f);

            if (clip != null)
            {
                EditorGUILayout.LabelField("Seconds", $"{(timeProperty.floatValue * clip.length):F3}s");
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set To Current"))
            {
                timeProperty.floatValue = _currentNormalizedTime;
            }

            if (GUILayout.Button("Remove"))
            {
                removeIndex = i;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        if (removeIndex >= 0)
        {
            _attackTimingsProperty.DeleteArrayElementAtIndex(removeIndex);
            ClampAttackTimingProperties();
            serializedObject.ApplyModifiedProperties();
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("Add Attack Timing"))
        {
            int newIndex = _attackTimingsProperty.arraySize;
            _attackTimingsProperty.InsertArrayElementAtIndex(newIndex);

            SerializedProperty newElement = _attackTimingsProperty.GetArrayElementAtIndex(newIndex);
            newElement.FindPropertyRelative("_id").stringValue = string.Empty;
            newElement.FindPropertyRelative("_normalizedTime").floatValue = _currentNormalizedTime;
            ClampAttackTimingProperties();
        }
    }

    private void DrawAttackEffectTimingFields(AnimationClip clip)
    {
        EditorGUILayout.LabelField("Attack Effect Timings", EditorStyles.boldLabel);

        if (_attackEffectTimingsProperty.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Add attack effect timing entries to define effect IDs and their start timings.", MessageType.None);
        }

        int removeIndex = -1;

        for (int i = 0; i < _attackEffectTimingsProperty.arraySize; i++)
        {
            SerializedProperty element = _attackEffectTimingsProperty.GetArrayElementAtIndex(i);
            SerializedProperty idProperty = element.FindPropertyRelative("_id");
            SerializedProperty timeProperty = element.FindPropertyRelative("_normalizedTime");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Attack Effect Timing {i + 1}", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(idProperty, new GUIContent("ID"));
            timeProperty.floatValue = EditorGUILayout.Slider("Normalized Time", timeProperty.floatValue, 0f, 1f);

            if (clip != null)
            {
                EditorGUILayout.LabelField("Seconds", $"{(timeProperty.floatValue * clip.length):F3}s");
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set To Current"))
            {
                timeProperty.floatValue = _currentNormalizedTime;
            }

            if (GUILayout.Button("Remove"))
            {
                removeIndex = i;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        if (removeIndex >= 0)
        {
            _attackEffectTimingsProperty.DeleteArrayElementAtIndex(removeIndex);
            ClampAttackEffectTimingProperties();
            serializedObject.ApplyModifiedProperties();
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("Add Attack Effect Timing"))
        {
            int newIndex = _attackEffectTimingsProperty.arraySize;
            _attackEffectTimingsProperty.InsertArrayElementAtIndex(newIndex);

            SerializedProperty newElement = _attackEffectTimingsProperty.GetArrayElementAtIndex(newIndex);
            newElement.FindPropertyRelative("_id").stringValue = string.Empty;
            newElement.FindPropertyRelative("_normalizedTime").floatValue = _currentNormalizedTime;
            ClampAttackEffectTimingProperties();
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
            "LMB: orbit  RMB: pan  Wheel: zoom  F: frame",
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

        DrawAttackTimingMarkers(timelineRect);
        DrawAttackEffectTimingMarkers(timelineRect);

        EditorGUI.DrawRect(startHandleRect, new Color(0.95f, 0.75f, 0.2f));
        EditorGUI.DrawRect(endHandleRect, new Color(0.95f, 0.45f, 0.2f));
        EditorGUI.DrawRect(playheadRect, new Color(0.9f, 0.95f, 1f));

        GUI.Label(
            new Rect(totalRect.x + TimelinePadding, totalRect.y - 2f, totalRect.width - (TimelinePadding * 2f), 16f),
            "Timeline Scrub",
            EditorStyles.miniBoldLabel);

        GUI.Label(
            new Rect(timelineRect.xMin, timelineRect.yMax + 18f, 90f, 16f),
            "0.000",
            EditorStyles.miniLabel);
        GUI.Label(
            new Rect(timelineRect.center.x - 25f, timelineRect.yMax + 18f, 50f, 16f),
            "0.500",
            _timelineCenterLabelStyle);
        GUI.Label(
            new Rect(timelineRect.xMax - 50f, timelineRect.yMax + 18f, 50f, 16f),
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

        if (GUILayout.Button("Add Attack Timing At Current"))
        {
            int newIndex = _attackTimingsProperty.arraySize;
            _attackTimingsProperty.InsertArrayElementAtIndex(newIndex);

            SerializedProperty newElement = _attackTimingsProperty.GetArrayElementAtIndex(newIndex);
            newElement.FindPropertyRelative("_id").stringValue = string.Empty;
            newElement.FindPropertyRelative("_normalizedTime").floatValue = _currentNormalizedTime;
        }

        if (GUILayout.Button("Add Attack Effect At Current"))
        {
            int newIndex = _attackEffectTimingsProperty.arraySize;
            _attackEffectTimingsProperty.InsertArrayElementAtIndex(newIndex);

            SerializedProperty newElement = _attackEffectTimingsProperty.GetArrayElementAtIndex(newIndex);
            newElement.FindPropertyRelative("_id").stringValue = string.Empty;
            newElement.FindPropertyRelative("_normalizedTime").floatValue = _currentNormalizedTime;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            $"Current: {_currentNormalizedTime:F3} ({_currentNormalizedTime * clip.length:F3}s)\n" +
            $"Combo Window: {_comboInputStartNormalizedTimeProperty.floatValue:F3} - {_comboInputEndNormalizedTimeProperty.floatValue:F3}\n" +
            $"Attack Timings: {_attackTimingsProperty.arraySize}\n" +
            $"Attack Effect Timings: {_attackEffectTimingsProperty.arraySize}",
            MessageType.None);
    }

    private void DrawAttackTimingMarkers(Rect timelineRect)
    {
        for (int i = 0; i < _attackTimingsProperty.arraySize; i++)
        {
            SerializedProperty element = _attackTimingsProperty.GetArrayElementAtIndex(i);
            SerializedProperty idProperty = element.FindPropertyRelative("_id");
            SerializedProperty timeProperty = element.FindPropertyRelative("_normalizedTime");
            float markerTime = Mathf.Clamp01(timeProperty.floatValue);
            float x = Mathf.Lerp(timelineRect.xMin, timelineRect.xMax, markerTime);
            Rect markerRect = new Rect(x - (AttackMarkerWidth * 0.5f), timelineRect.y - 5f, AttackMarkerWidth, timelineRect.height + 10f);

            EditorGUI.DrawRect(markerRect, new Color(1f, 0.45f, 0.15f, 0.95f));

            string label = string.IsNullOrWhiteSpace(idProperty.stringValue) ? $"AT{i + 1}" : idProperty.stringValue;
            GUI.Label(new Rect(x - 32f, timelineRect.y - 20f, 64f, 14f), label, _attackMarkerLabelStyle);
        }
    }

    private void DrawAttackEffectTimingMarkers(Rect timelineRect)
    {
        for (int i = 0; i < _attackEffectTimingsProperty.arraySize; i++)
        {
            SerializedProperty element = _attackEffectTimingsProperty.GetArrayElementAtIndex(i);
            SerializedProperty idProperty = element.FindPropertyRelative("_id");
            SerializedProperty timeProperty = element.FindPropertyRelative("_normalizedTime");
            float time = Mathf.Clamp01(timeProperty.floatValue);
            float x = Mathf.Lerp(timelineRect.xMin, timelineRect.xMax, time);

            Rect rangeRect = new Rect(
                x - (AttackMarkerWidth * 0.5f),
                timelineRect.yMax + 2f,
                AttackMarkerWidth,
                EffectMarkerHeight);

            EditorGUI.DrawRect(rangeRect, new Color(0.2f, 0.7f, 1f, 0.9f));

            string label = string.IsNullOrWhiteSpace(idProperty.stringValue) ? $"FX{i + 1}" : idProperty.stringValue;
            GUI.Label(new Rect(x - 32f, rangeRect.yMax + 1f, 64f, 14f), label, _effectMarkerLabelStyle);
        }
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
                _dragMode = ResolveDragMode(evt.mousePosition, startHandleRect, endHandleRect);
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

    private TimelineDragMode ResolveDragMode(Vector2 mousePosition, Rect startHandleRect, Rect endHandleRect)
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
        Vector3 center = bounds.center + _previewPivotOffset;
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
                if (!previewRect.Contains(evt.mousePosition) || (evt.button != 0 && evt.button != 1))
                {
                    return;
                }

                GUIUtility.hotControl = controlId;
                _previewDragMode = evt.button == 0 ? PreviewDragMode.Orbit : PreviewDragMode.Pan;
                evt.Use();
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl != controlId || _previewDragMode == PreviewDragMode.None)
                {
                    return;
                }

                if (_previewDragMode == PreviewDragMode.Orbit)
                {
                    _previewOrbit.y += evt.delta.x * 0.6f;
                    _previewOrbit.x = Mathf.Clamp(_previewOrbit.x + (evt.delta.y * 0.5f), -80f, 80f);
                }
                else if (_previewDragMode == PreviewDragMode.Pan)
                {
                    ApplyPan(evt.delta);
                }

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
                MouseCursor cursor = _previewDragMode == PreviewDragMode.Pan ? MouseCursor.Pan : MouseCursor.Orbit;
                EditorGUIUtility.AddCursorRect(previewRect, cursor, controlId);
                break;
        }
    }

    private void ApplyPan(Vector2 delta)
    {
        float radius = Mathf.Max(_previewBounds.extents.magnitude, 0.5f);
        float panScale = radius * _previewZoomFactor * 0.0025f;
        Quaternion rotation = Quaternion.Euler(_previewOrbit.x, _previewOrbit.y, 0f);
        Vector3 right = rotation * Vector3.right;
        Vector3 up = rotation * Vector3.up;

        _previewPivotOffset += ((-right * delta.x) + (up * delta.y)) * panScale;
    }

    private void ApplyZoom(float delta)
    {
        _previewZoomFactor = Mathf.Clamp(_previewZoomFactor + delta, MinZoomFactor, MaxZoomFactor);
    }

    private void ResetPreviewCamera()
    {
        _previewOrbit = new Vector2(18f, 180f);
        _previewZoomFactor = 3.6f;
        _previewPivotOffset = Vector3.zero;
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

        ClampAttackTimingProperties();
        ClampAttackEffectTimingProperties();
    }

    private void ClampAttackTimingProperties()
    {
        if (_attackTimingsProperty == null)
        {
            return;
        }

        for (int i = 0; i < _attackTimingsProperty.arraySize; i++)
        {
            SerializedProperty element = _attackTimingsProperty.GetArrayElementAtIndex(i);
            SerializedProperty idProperty = element.FindPropertyRelative("_id");
            SerializedProperty timeProperty = element.FindPropertyRelative("_normalizedTime");

            if (idProperty.stringValue == null)
            {
                idProperty.stringValue = string.Empty;
            }

            timeProperty.floatValue = Mathf.Clamp01(timeProperty.floatValue);
        }
    }

    private void ClampAttackEffectTimingProperties()
    {
        if (_attackEffectTimingsProperty == null)
        {
            return;
        }

        for (int i = 0; i < _attackEffectTimingsProperty.arraySize; i++)
        {
            SerializedProperty element = _attackEffectTimingsProperty.GetArrayElementAtIndex(i);
            SerializedProperty idProperty = element.FindPropertyRelative("_id");
            SerializedProperty timeProperty = element.FindPropertyRelative("_normalizedTime");

            if (idProperty.stringValue == null)
            {
                idProperty.stringValue = string.Empty;
            }

            timeProperty.floatValue = Mathf.Clamp01(timeProperty.floatValue);
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

    private static AttackData FindAttackDataById(string comboAttackId)
    {
        if (string.IsNullOrWhiteSpace(comboAttackId))
        {
            return null;
        }

        string[] legacyGuids = AssetDatabase.FindAssets("t:BasicComboAttackData");
        for (int i = 0; i < legacyGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(legacyGuids[i]);
            AttackData attackData = AssetDatabase.LoadAssetAtPath<AttackData>(path);
            if (attackData != null && attackData.Id == comboAttackId)
            {
                return attackData;
            }
        }

        string[] generalGuids = AssetDatabase.FindAssets("t:GeneralAttackData");
        for (int i = 0; i < generalGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(generalGuids[i]);
            AttackData attackData = AssetDatabase.LoadAssetAtPath<AttackData>(path);
            if (attackData != null && attackData.Id == comboAttackId)
            {
                return attackData;
            }
        }

        return null;
    }
}
