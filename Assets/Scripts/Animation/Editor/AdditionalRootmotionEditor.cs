using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AdditionalRootmotion))]
public class AdditionalRootmotionEditor : Editor
{
    private const float PreviewHeight = 280f;
    private const float TimelineHeight = 80f;
    private const float TimelineHandleWidth = 10f;
    private const float TimelinePadding = 12f;
    private const float MinZoomFactor = 1.2f;
    private const float MaxZoomFactor = 8f;
    private const float MovementCanvasHeight = 240f;
    private const float SegmentHandleRadius = 7f;
    private const float PreviewGridStep = 1f;
    private const float PreviewGridMinimumExtent = 4f;

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

    private SerializedProperty _previewAnimationClipProperty;
    private SerializedProperty _previewModelPrefabProperty;
    private SerializedProperty _spaceProperty;
    private SerializedProperty _constrainToGroundPlaneProperty;
    private SerializedProperty _segmentsProperty;

    private PreviewRenderUtility _previewRenderUtility;
    private Material _previewLineMaterial;
    private GameObject _previewInstance;
    private GameObject _previewSource;
    private Bounds _previewBounds;
    private Vector2 _previewOrbit = new Vector2(18f, 180f);
    private float _previewZoomFactor = 3.6f;
    private Vector3 _previewPivotOffset;
    private float _currentNormalizedTime;
    private float _lastEditorTime;
    private bool _isPlaying;
    private int _selectedSegmentIndex = -1;
    private TimelineDragMode _timelineDragMode;
    private PreviewDragMode _previewDragMode;
    private bool _isDraggingMovementHandle;

    private void OnEnable()
    {
        _previewAnimationClipProperty = serializedObject.FindProperty("_previewAnimationClip");
        _previewModelPrefabProperty = serializedObject.FindProperty("_previewModelPrefab");
        _spaceProperty = serializedObject.FindProperty("_space");
        _constrainToGroundPlaneProperty = serializedObject.FindProperty("_constrainToGroundPlane");
        _segmentsProperty = serializedObject.FindProperty("_segments");

        _lastEditorTime = (float)EditorApplication.timeSinceStartup;
        EditorApplication.update += OnEditorUpdate;
        ClampSelectedSegmentIndex();
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        CleanupPreview();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        ClampSelectedSegmentIndex();

        EditorGUILayout.PropertyField(_previewAnimationClipProperty);
        EditorGUILayout.PropertyField(_previewModelPrefabProperty);
        EditorGUILayout.PropertyField(_spaceProperty);
        EditorGUILayout.PropertyField(_constrainToGroundPlaneProperty);

        AnimationClip clip = _previewAnimationClipProperty.objectReferenceValue as AnimationClip;
        GameObject prefab = _previewModelPrefabProperty.objectReferenceValue as GameObject;

        EditorGUILayout.Space(6f);
        DrawAssetSummary(clip);
        EditorGUILayout.Space(6f);

        if (clip == null || prefab == null)
        {
            EditorGUILayout.HelpBox("Assign a Preview Animation Clip and a Preview Model Prefab to edit Additional Rootmotion data visually.", MessageType.Info);
        }
        else
        {
            DrawPreviewToolbar(clip);
            DrawPreviewArea(clip, prefab);
            EditorGUILayout.Space(6f);
            DrawTimeline(clip);
            EditorGUILayout.Space(6f);
            DrawMovementCanvas();
        }

        EditorGUILayout.Space(8f);
        DrawSegmentsInspector(clip);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawAssetSummary(AnimationClip clip)
    {
        Vector3 totalOffset = EvaluateCumulativeDeltaAtTime(1f);
        string clipInfo = clip == null ? "None" : $"{clip.name} ({clip.length:F3}s)";

        EditorGUILayout.HelpBox(
            $"Preview Clip: {clipInfo}\n" +
            $"Segments: {_segmentsProperty.arraySize}\n" +
            $"Current Time: {_currentNormalizedTime:F3}\n" +
            $"Total Offset Preview: {FormatVector(totalOffset)}",
            MessageType.None);
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

        Vector3 currentOffset = EvaluatePreviewWorldDelta(_currentNormalizedTime);
        Vector3 totalOffset = EvaluatePreviewWorldDelta(1f);
        GUI.Label(
            new Rect(previewRect.x + 8f, previewRect.yMax - 40f, previewRect.width - 16f, 34f),
            $"3D preview applies Additional Offset. Current {FormatVector(currentOffset)} / Total {FormatVector(totalOffset)}\nLocal space uses the preview root rotation. Grid step: {PreviewGridStep:F0} unit",
            EditorStyles.whiteMiniLabel);
    }

    private void DrawTimeline(AnimationClip clip)
    {
        Rect totalRect = GUILayoutUtility.GetRect(10f, TimelineHeight, GUILayout.ExpandWidth(true));
        Rect timelineRect = new Rect(
            totalRect.x + TimelinePadding,
            totalRect.y + 18f,
            totalRect.width - (TimelinePadding * 2f),
            14f);

        SerializedProperty selectedSegment = GetSelectedSegmentProperty();
        Rect startHandleRect = Rect.zero;
        Rect endHandleRect = Rect.zero;

        if (selectedSegment != null)
        {
            startHandleRect = GetHandleRect(timelineRect, GetSegmentStartProperty(selectedSegment).floatValue);
            endHandleRect = GetHandleRect(timelineRect, GetSegmentEndProperty(selectedSegment).floatValue);
        }

        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        HandleTimelineInput(Event.current, controlId, timelineRect, startHandleRect, endHandleRect);

        EditorGUI.DrawRect(timelineRect, new Color(0.14f, 0.14f, 0.14f));
        DrawTimelineAxis(timelineRect);
        DrawSegmentRangesOnTimeline(timelineRect);

        if (selectedSegment != null)
        {
            EditorGUI.DrawRect(startHandleRect, new Color(1f, 0.78f, 0.2f));
            EditorGUI.DrawRect(endHandleRect, new Color(1f, 0.42f, 0.2f));
        }

        Rect playheadRect = GetHandleRect(timelineRect, _currentNormalizedTime, 2f);
        EditorGUI.DrawRect(playheadRect, new Color(0.9f, 0.95f, 1f));

        GUI.Label(
            new Rect(totalRect.x + TimelinePadding, totalRect.y - 2f, totalRect.width - (TimelinePadding * 2f), 16f),
            "Segment Timeline",
            EditorStyles.miniBoldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Segment At Current"))
        {
            AddSegmentAtCurrentTime();
        }

        using (new EditorGUI.DisabledScope(selectedSegment == null))
        {
            if (GUILayout.Button("Set Segment Start To Current"))
            {
                GetSegmentStartProperty(selectedSegment).floatValue = Mathf.Min(_currentNormalizedTime, GetSegmentEndProperty(selectedSegment).floatValue);
            }

            if (GUILayout.Button("Set Segment End To Current"))
            {
                GetSegmentEndProperty(selectedSegment).floatValue = Mathf.Max(_currentNormalizedTime, GetSegmentStartProperty(selectedSegment).floatValue);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            $"Current: {_currentNormalizedTime:F3} ({_currentNormalizedTime * clip.length:F3}s)\n" +
            $"Selected Segment: {GetSelectedSegmentName()}\n" +
            $"Current Offset: {FormatVector(EvaluateCumulativeDeltaAtTime(_currentNormalizedTime))}",
            MessageType.None);
    }

    private void DrawMovementCanvas()
    {
        Rect canvasRect = GUILayoutUtility.GetRect(10f, MovementCanvasHeight, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(canvasRect, new Color(0.12f, 0.12f, 0.125f));
        DrawMovementGrid(canvasRect);

        Vector3[] points = BuildPathPoints();
        float scale = CalculateCanvasScale(points);
        DrawMovementAxes(canvasRect);
        DrawMovementPath(canvasRect, points, scale);
        DrawCurrentOffsetHandle(canvasRect, scale);
        HandleMovementCanvasInput(canvasRect, scale);

        GUI.Label(
            new Rect(canvasRect.x + 8f, canvasRect.y + 8f, canvasRect.width - 16f, 18f),
            "Top-Down Offset Canvas (X: right, Z: forward)",
            EditorStyles.whiteMiniLabel);
    }

    private void DrawSegmentsInspector(AnimationClip clip)
    {
        EditorGUILayout.LabelField("Segments", EditorStyles.boldLabel);

        if (_segmentsProperty.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No segment exists yet. Add one to define a rootmotion window, delta, and distribution curve.", MessageType.Info);
        }

        int removeIndex = -1;

        for (int i = 0; i < _segmentsProperty.arraySize; i++)
        {
            SerializedProperty segment = _segmentsProperty.GetArrayElementAtIndex(i);
            SerializedProperty nameProperty = segment.FindPropertyRelative("_name");
            SerializedProperty enabledProperty = segment.FindPropertyRelative("_enabled");
            SerializedProperty startProperty = segment.FindPropertyRelative("_startNormalizedTime");
            SerializedProperty endProperty = segment.FindPropertyRelative("_endNormalizedTime");
            SerializedProperty deltaProperty = segment.FindPropertyRelative("_targetDeltaPosition");
            SerializedProperty curveProperty = segment.FindPropertyRelative("_distributionCurve");
            SerializedProperty colorProperty = segment.FindPropertyRelative("_editorColor");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            bool isSelected = _selectedSegmentIndex == i;
            if (GUILayout.Toggle(isSelected, isSelected ? "Selected" : "Select", "Button", GUILayout.Width(70f)))
            {
                _selectedSegmentIndex = i;
            }

            enabledProperty.boolValue = EditorGUILayout.ToggleLeft($"Segment {i + 1}", enabledProperty.boolValue, GUILayout.Width(90f));
            EditorGUILayout.PropertyField(nameProperty, GUIContent.none);

            if (GUILayout.Button("Remove", GUILayout.Width(70f)))
            {
                removeIndex = i;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            float start = startProperty.floatValue;
            float end = endProperty.floatValue;
            EditorGUILayout.MinMaxSlider(new GUIContent("Normalized Range"), ref start, ref end, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                startProperty.floatValue = start;
                endProperty.floatValue = Mathf.Max(start, end);
            }

            EditorGUILayout.BeginHorizontal();
            startProperty.floatValue = EditorGUILayout.Slider("Start", startProperty.floatValue, 0f, 1f);
            endProperty.floatValue = EditorGUILayout.Slider("End", endProperty.floatValue, 0f, 1f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(deltaProperty, new GUIContent("Target Delta"));
            EditorGUILayout.PropertyField(colorProperty, new GUIContent("Editor Color"));
            EditorGUILayout.PropertyField(curveProperty, new GUIContent("Distribution Curve"));
            DrawCurvePresetButtons(curveProperty);
            DrawCurveMeaningHelp();

            if (clip != null)
            {
                EditorGUILayout.LabelField("Seconds", $"{(startProperty.floatValue * clip.length):F3}s - {(endProperty.floatValue * clip.length):F3}s");
            }

            Vector3 previewEndOffset = EvaluateSegmentEndOffset(i);
            EditorGUILayout.LabelField("Preview End Offset", FormatVector(previewEndOffset));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set Start To Current"))
            {
                startProperty.floatValue = Mathf.Min(_currentNormalizedTime, endProperty.floatValue);
                _selectedSegmentIndex = i;
            }

            if (GUILayout.Button("Set End To Current"))
            {
                endProperty.floatValue = Mathf.Max(_currentNormalizedTime, startProperty.floatValue);
                _selectedSegmentIndex = i;
            }

            if (GUILayout.Button("Snap Current"))
            {
                _selectedSegmentIndex = i;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        if (removeIndex >= 0)
        {
            _segmentsProperty.DeleteArrayElementAtIndex(removeIndex);
            if (_selectedSegmentIndex >= _segmentsProperty.arraySize)
            {
                _selectedSegmentIndex = _segmentsProperty.arraySize - 1;
            }

            serializedObject.ApplyModifiedProperties();
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("Add Segment"))
        {
            AddSegmentAtCurrentTime();
        }
    }

    private void DrawTimelineAxis(Rect timelineRect)
    {
        GUI.Label(new Rect(timelineRect.xMin, timelineRect.yMax + 18f, 50f, 16f), "0.000", EditorStyles.miniLabel);
        GUI.Label(new Rect(timelineRect.center.x - 25f, timelineRect.yMax + 18f, 50f, 16f), "0.500", EditorStyles.centeredGreyMiniLabel);
        GUI.Label(new Rect(timelineRect.xMax - 50f, timelineRect.yMax + 18f, 50f, 16f), "1.000", EditorStyles.miniLabel);
    }

    private void DrawCurvePresetButtons(SerializedProperty curveProperty)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Curve Presets", GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

        if (GUILayout.Button("Linear"))
        {
            curveProperty.animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }

        if (GUILayout.Button("Fast > Slow"))
        {
            curveProperty.animationCurveValue = CreateEaseOutCurve();
        }

        if (GUILayout.Button("Slow > Fast"))
        {
            curveProperty.animationCurveValue = CreateEaseInCurve();
        }

        if (GUILayout.Button("Soft In/Out"))
        {
            curveProperty.animationCurveValue = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawCurveMeaningHelp()
    {
        EditorGUILayout.HelpBox(
            "The curve is cumulative movement progress from 0 to 1. A steep part moves faster; a flat part moves slower. Fast > Slow starts with a steep slope and eases near the end.",
            MessageType.None);
    }

    private void DrawSegmentRangesOnTimeline(Rect timelineRect)
    {
        for (int i = 0; i < _segmentsProperty.arraySize; i++)
        {
            SerializedProperty segment = _segmentsProperty.GetArrayElementAtIndex(i);
            if (!GetSegmentEnabledProperty(segment).boolValue)
            {
                continue;
            }

            float start = GetSegmentStartProperty(segment).floatValue;
            float end = GetSegmentEndProperty(segment).floatValue;
            Color color = GetSegmentColorProperty(segment).colorValue;
            float xMin = Mathf.Lerp(timelineRect.xMin, timelineRect.xMax, start);
            float xMax = Mathf.Lerp(timelineRect.xMin, timelineRect.xMax, end);
            Rect rect = new Rect(xMin, timelineRect.y, Mathf.Max(2f, xMax - xMin), timelineRect.height);

            EditorGUI.DrawRect(rect, i == _selectedSegmentIndex ? color : new Color(color.r, color.g, color.b, 0.55f));

            string label = GetSegmentNameProperty(segment).stringValue;
            if (string.IsNullOrWhiteSpace(label))
            {
                label = $"Segment {i + 1}";
            }

            GUI.Label(new Rect(rect.xMin + 4f, rect.y - 16f, Mathf.Max(50f, rect.width), 14f), label, EditorStyles.whiteMiniLabel);
        }
    }

    private void HandleTimelineInput(Event evt, int controlId, Rect timelineRect, Rect startHandleRect, Rect endHandleRect)
    {
        switch (evt.GetTypeForControl(controlId))
        {
            case EventType.MouseDown:
                if (evt.button != 0)
                {
                    return;
                }

                if (!timelineRect.Contains(evt.mousePosition) &&
                    !startHandleRect.Contains(evt.mousePosition) &&
                    !endHandleRect.Contains(evt.mousePosition))
                {
                    return;
                }

                GUIUtility.hotControl = controlId;
                _isPlaying = false;
                _timelineDragMode = ResolveTimelineDragMode(evt.mousePosition, startHandleRect, endHandleRect);
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
                _timelineDragMode = TimelineDragMode.None;
                evt.Use();
                break;

            case EventType.Repaint:
                if (GetSelectedSegmentProperty() != null)
                {
                    EditorGUIUtility.AddCursorRect(startHandleRect, MouseCursor.ResizeHorizontal, controlId);
                    EditorGUIUtility.AddCursorRect(endHandleRect, MouseCursor.ResizeHorizontal, controlId);
                }

                EditorGUIUtility.AddCursorRect(timelineRect, MouseCursor.SlideArrow, controlId);
                break;
        }
    }

    private TimelineDragMode ResolveTimelineDragMode(Vector2 mousePosition, Rect startHandleRect, Rect endHandleRect)
    {
        if (GetSelectedSegmentProperty() != null)
        {
            if (startHandleRect.Contains(mousePosition))
            {
                return TimelineDragMode.StartHandle;
            }

            if (endHandleRect.Contains(mousePosition))
            {
                return TimelineDragMode.EndHandle;
            }
        }

        return TimelineDragMode.CurrentTime;
    }

    private void UpdateTimelineFromMouse(Vector2 mousePosition, Rect timelineRect)
    {
        float normalized = Mathf.Clamp01(Mathf.InverseLerp(timelineRect.xMin, timelineRect.xMax, mousePosition.x));
        SerializedProperty selectedSegment = GetSelectedSegmentProperty();

        switch (_timelineDragMode)
        {
            case TimelineDragMode.StartHandle:
                if (selectedSegment != null)
                {
                    GetSegmentStartProperty(selectedSegment).floatValue = Mathf.Min(normalized, GetSegmentEndProperty(selectedSegment).floatValue);
                }
                _currentNormalizedTime = normalized;
                serializedObject.ApplyModifiedProperties();
                break;

            case TimelineDragMode.EndHandle:
                if (selectedSegment != null)
                {
                    GetSegmentEndProperty(selectedSegment).floatValue = Mathf.Max(normalized, GetSegmentStartProperty(selectedSegment).floatValue);
                }
                _currentNormalizedTime = normalized;
                serializedObject.ApplyModifiedProperties();
                break;

            default:
                _currentNormalizedTime = normalized;
                break;
        }

        Repaint();
    }

    private void DrawMovementGrid(Rect rect)
    {
        const int divisions = 8;
        Color gridColor = new Color(1f, 1f, 1f, 0.08f);

        for (int i = 1; i < divisions; i++)
        {
            float t = i / (float)divisions;
            float x = Mathf.Lerp(rect.xMin, rect.xMax, t);
            float y = Mathf.Lerp(rect.yMin, rect.yMax, t);
            EditorGUI.DrawRect(new Rect(x, rect.yMin, 1f, rect.height), gridColor);
            EditorGUI.DrawRect(new Rect(rect.xMin, y, rect.width, 1f), gridColor);
        }
    }

    private void DrawMovementAxes(Rect rect)
    {
        float centerX = rect.center.x;
        float centerY = rect.center.y;
        EditorGUI.DrawRect(new Rect(centerX, rect.yMin, 1f, rect.height), new Color(0.8f, 0.2f, 0.2f, 0.45f));
        EditorGUI.DrawRect(new Rect(rect.xMin, centerY, rect.width, 1f), new Color(0.2f, 0.8f, 0.2f, 0.45f));
    }

    private void DrawMovementPath(Rect rect, Vector3[] points, float scale)
    {
        if (points.Length == 0)
        {
            return;
        }

        Handles.BeginGUI();
        Vector2 previous = CanvasPoint(rect, points[0], scale);

        for (int i = 1; i < points.Length; i++)
        {
            SerializedProperty colorSourceSegment = _segmentsProperty.GetArrayElementAtIndex(i - 1);
            Color color = GetSegmentColorProperty(colorSourceSegment).colorValue;
            Vector2 current = CanvasPoint(rect, points[i], scale);
            Handles.color = color;
            Handles.DrawAAPolyLine(3f, previous, current);
            Handles.DrawSolidDisc(new Vector3(current.x, current.y, 0f), Vector3.forward, 4f);

            if (i - 1 == _selectedSegmentIndex)
            {
                Handles.color = Color.white;
                Handles.DrawWireDisc(new Vector3(current.x, current.y, 0f), Vector3.forward, SegmentHandleRadius);
            }

            previous = current;
        }

        Handles.EndGUI();
    }

    private void DrawCurrentOffsetHandle(Rect rect, float scale)
    {
        Vector3 currentOffset = EvaluateCumulativeDeltaAtTime(_currentNormalizedTime);
        Vector2 point = CanvasPoint(rect, currentOffset, scale);

        Handles.BeginGUI();
        Handles.color = Color.white;
        Handles.DrawSolidDisc(new Vector3(point.x, point.y, 0f), Vector3.forward, 4f);
        Handles.EndGUI();

        GUI.Label(
            new Rect(rect.x + 8f, rect.yMax - 22f, rect.width - 16f, 18f),
            $"Current Offset {FormatVector(currentOffset)}",
            EditorStyles.whiteMiniLabel);
    }

    private void HandleMovementCanvasInput(Rect rect, float scale)
    {
        SerializedProperty selectedSegment = GetSelectedSegmentProperty();
        if (selectedSegment == null)
        {
            return;
        }

        Vector3 endOffset = EvaluateSegmentEndOffset(_selectedSegmentIndex);
        Vector2 handlePosition = CanvasPoint(rect, endOffset, scale);
        Rect handleRect = new Rect(
            handlePosition.x - SegmentHandleRadius,
            handlePosition.y - SegmentHandleRadius,
            SegmentHandleRadius * 2f,
            SegmentHandleRadius * 2f);

        Event evt = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);

        switch (evt.GetTypeForControl(controlId))
        {
            case EventType.MouseDown:
                if (evt.button != 0 || !handleRect.Contains(evt.mousePosition))
                {
                    return;
                }

                GUIUtility.hotControl = controlId;
                _isDraggingMovementHandle = true;
                evt.Use();
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl != controlId || !_isDraggingMovementHandle)
                {
                    return;
                }

                Vector3 absoluteOffset = WorldPointFromCanvas(rect, evt.mousePosition, scale);
                Vector3 startOffset = EvaluateSegmentStartOffset(_selectedSegmentIndex);
                Vector3 delta = absoluteOffset - startOffset;
                SerializedProperty deltaProperty = GetSegmentDeltaProperty(selectedSegment);
                Vector3 newDelta = deltaProperty.vector3Value;
                newDelta.x = delta.x;
                newDelta.z = delta.z;
                deltaProperty.vector3Value = newDelta;
                serializedObject.ApplyModifiedProperties();
                evt.Use();
                Repaint();
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl != controlId)
                {
                    return;
                }

                GUIUtility.hotControl = 0;
                _isDraggingMovementHandle = false;
                evt.Use();
                break;

            case EventType.Repaint:
                EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.MoveArrow, controlId);
                break;
        }
    }

    private void AddSegmentAtCurrentTime()
    {
        int newIndex = _segmentsProperty.arraySize;
        _segmentsProperty.InsertArrayElementAtIndex(newIndex);
        SerializedProperty segment = _segmentsProperty.GetArrayElementAtIndex(newIndex);

        GetSegmentNameProperty(segment).stringValue = $"Segment {newIndex + 1}";
        GetSegmentEnabledProperty(segment).boolValue = true;
        GetSegmentStartProperty(segment).floatValue = _currentNormalizedTime;
        GetSegmentEndProperty(segment).floatValue = Mathf.Clamp01(_currentNormalizedTime + 0.15f);
        GetSegmentDeltaProperty(segment).vector3Value = Vector3.forward;
        GetSegmentCurveProperty(segment).animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        GetSegmentColorProperty(segment).colorValue = GetColorForIndex(newIndex);

        _selectedSegmentIndex = newIndex;
        serializedObject.ApplyModifiedProperties();
    }

    private void ClampSelectedSegmentIndex()
    {
        if (_segmentsProperty == null || _segmentsProperty.arraySize == 0)
        {
            _selectedSegmentIndex = -1;
            return;
        }

        _selectedSegmentIndex = Mathf.Clamp(_selectedSegmentIndex, 0, _segmentsProperty.arraySize - 1);
    }

    private SerializedProperty GetSelectedSegmentProperty()
    {
        if (_selectedSegmentIndex < 0 || _selectedSegmentIndex >= _segmentsProperty.arraySize)
        {
            return null;
        }

        return _segmentsProperty.GetArrayElementAtIndex(_selectedSegmentIndex);
    }

    private string GetSelectedSegmentName()
    {
        SerializedProperty segment = GetSelectedSegmentProperty();
        if (segment == null)
        {
            return "None";
        }

        string name = GetSegmentNameProperty(segment).stringValue;
        return string.IsNullOrWhiteSpace(name) ? $"Segment {_selectedSegmentIndex + 1}" : name;
    }

    private Vector3[] BuildPathPoints()
    {
        Vector3[] points = new Vector3[_segmentsProperty.arraySize + 1];
        points[0] = Vector3.zero;
        Vector3 current = Vector3.zero;

        for (int i = 0; i < _segmentsProperty.arraySize; i++)
        {
            SerializedProperty segment = _segmentsProperty.GetArrayElementAtIndex(i);
            if (GetSegmentEnabledProperty(segment).boolValue)
            {
                current += GetGraphDelta(GetSegmentDeltaProperty(segment).vector3Value);
            }

            points[i + 1] = current;
        }

        return points;
    }

    private Vector3 EvaluateSegmentStartOffset(int segmentIndex)
    {
        Vector3 result = Vector3.zero;
        int count = Mathf.Min(segmentIndex, _segmentsProperty.arraySize);

        for (int i = 0; i < count; i++)
        {
            SerializedProperty segment = _segmentsProperty.GetArrayElementAtIndex(i);
            if (!GetSegmentEnabledProperty(segment).boolValue)
            {
                continue;
            }

            result += GetGraphDelta(GetSegmentDeltaProperty(segment).vector3Value);
        }

        return result;
    }

    private Vector3 EvaluateSegmentEndOffset(int segmentIndex)
    {
        Vector3 result = EvaluateSegmentStartOffset(segmentIndex);
        if (segmentIndex < 0 || segmentIndex >= _segmentsProperty.arraySize)
        {
            return result;
        }

        SerializedProperty segment = _segmentsProperty.GetArrayElementAtIndex(segmentIndex);
        if (GetSegmentEnabledProperty(segment).boolValue)
        {
            result += GetGraphDelta(GetSegmentDeltaProperty(segment).vector3Value);
        }

        return result;
    }

    private Vector3 EvaluateCumulativeDeltaAtTime(float normalizedTime)
    {
        Vector3 total = Vector3.zero;

        for (int i = 0; i < _segmentsProperty.arraySize; i++)
        {
            SerializedProperty segment = _segmentsProperty.GetArrayElementAtIndex(i);
            if (!GetSegmentEnabledProperty(segment).boolValue)
            {
                continue;
            }

            float ratio = EvaluateSegmentRatio(segment, normalizedTime);
            total += GetGraphDelta(GetSegmentDeltaProperty(segment).vector3Value) * ratio;
        }

        return total;
    }

    private Vector3 EvaluatePreviewWorldDelta(float normalizedTime)
    {
        return ConvertDeltaToPreviewWorld(EvaluateCumulativeDeltaAtTime(normalizedTime));
    }

    private Vector3[] BuildPreviewPathPoints()
    {
        Vector3[] points = BuildPathPoints();
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = ConvertDeltaToPreviewWorld(points[i]);
        }

        return points;
    }

    private Vector3 ConvertDeltaToPreviewWorld(Vector3 delta)
    {
        if (_spaceProperty.enumValueIndex == (int)AdditionalRootmotionSpace.Local && _previewInstance != null)
        {
            return _previewInstance.transform.rotation * delta;
        }

        return delta;
    }

    private float EvaluateSegmentRatio(SerializedProperty segment, float normalizedTime)
    {
        float start = GetSegmentStartProperty(segment).floatValue;
        float end = GetSegmentEndProperty(segment).floatValue;
        AnimationCurve curve = GetSegmentCurveProperty(segment).animationCurveValue;

        if (normalizedTime <= start)
        {
            return 0f;
        }

        if (end <= start || normalizedTime >= end)
        {
            return 1f;
        }

        if (curve == null || curve.length == 0)
        {
            return Mathf.InverseLerp(start, end, normalizedTime);
        }

        float t = Mathf.InverseLerp(start, end, normalizedTime);
        return Mathf.Clamp01(curve.Evaluate(t));
    }

    private float CalculateCanvasScale(Vector3[] points)
    {
        float maxAbs = 0.5f;
        for (int i = 0; i < points.Length; i++)
        {
            maxAbs = Mathf.Max(maxAbs, Mathf.Abs(points[i].x), Mathf.Abs(points[i].z));
        }

        Vector3 currentOffset = EvaluateCumulativeDeltaAtTime(_currentNormalizedTime);
        maxAbs = Mathf.Max(maxAbs, Mathf.Abs(currentOffset.x), Mathf.Abs(currentOffset.z));
        return maxAbs * 1.25f;
    }

    private static Vector2 CanvasPoint(Rect rect, Vector3 point, float scale)
    {
        float halfWidth = rect.width * 0.5f;
        float halfHeight = rect.height * 0.5f;
        float x = rect.center.x + ((point.x / scale) * halfWidth);
        float y = rect.center.y - ((point.z / scale) * halfHeight);
        return new Vector2(x, y);
    }

    private static Vector3 WorldPointFromCanvas(Rect rect, Vector2 point, float scale)
    {
        float localX = (point.x - rect.center.x) / (rect.width * 0.5f);
        float localZ = (rect.center.y - point.y) / (rect.height * 0.5f);
        return new Vector3(localX * scale, 0f, localZ * scale);
    }

    private Vector3 GetGraphDelta(Vector3 delta)
    {
        if (_constrainToGroundPlaneProperty.boolValue)
        {
            delta.y = 0f;
        }

        return delta;
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
        _previewInstance.transform.position = Vector3.zero;
        _previewRenderUtility.AddSingleGO(_previewInstance);
        _previewBounds = CalculateBounds(_previewInstance);
        ResetPreviewCamera();
    }

    private new void DrawPreview(Rect rect)
    {
        _previewRenderUtility.BeginPreview(rect, GUIStyle.none);

        Camera camera = _previewRenderUtility.camera;
        PositionCamera(camera, CalculatePreviewSceneBounds());
        camera.Render();
        DrawPreviewGroundGrid(camera);
        DrawPreviewAdditionalPath(camera);

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
        _previewInstance.transform.position = EvaluatePreviewWorldDelta(normalizedTime);
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

    private Bounds CalculatePreviewSceneBounds()
    {
        Bounds bounds = _previewBounds;
        Vector3[] pathPoints = BuildPreviewPathPoints();

        for (int i = 0; i < pathPoints.Length; i++)
        {
            bounds.Encapsulate(_previewBounds.center + pathPoints[i]);
        }

        bounds.Encapsulate(_previewBounds.center + EvaluatePreviewWorldDelta(_currentNormalizedTime));
        return bounds;
    }

    private void DrawPreviewGroundGrid(Camera camera)
    {
        if (!EnsurePreviewLineMaterial())
        {
            return;
        }

        float extent = CalculatePreviewGridExtent();
        int lineCount = Mathf.CeilToInt(extent / PreviewGridStep);

        _previewLineMaterial.SetPass(0);
        GL.PushMatrix();
        GL.LoadProjectionMatrix(camera.projectionMatrix);
        GL.modelview = camera.worldToCameraMatrix;
        GL.Begin(GL.LINES);

        for (int i = -lineCount; i <= lineCount; i++)
        {
            float value = i * PreviewGridStep;
            bool major = i == 0 || i % 5 == 0;
            GL.Color(major ? new Color(1f, 1f, 1f, 0.28f) : new Color(1f, 1f, 1f, 0.12f));
            GL.Vertex(new Vector3(value, 0f, -extent));
            GL.Vertex(new Vector3(value, 0f, extent));
            GL.Vertex(new Vector3(-extent, 0f, value));
            GL.Vertex(new Vector3(extent, 0f, value));
        }

        GL.Color(new Color(1f, 0.2f, 0.2f, 0.65f));
        GL.Vertex(new Vector3(-extent, 0.01f, 0f));
        GL.Vertex(new Vector3(extent, 0.01f, 0f));
        GL.Color(new Color(0.2f, 1f, 0.2f, 0.65f));
        GL.Vertex(new Vector3(0f, 0.01f, -extent));
        GL.Vertex(new Vector3(0f, 0.01f, extent));

        GL.End();
        GL.PopMatrix();
    }

    private void DrawPreviewAdditionalPath(Camera camera)
    {
        if (!EnsurePreviewLineMaterial())
        {
            return;
        }

        Vector3[] points = BuildPreviewPathPoints();
        if (points.Length == 0)
        {
            return;
        }

        _previewLineMaterial.SetPass(0);
        GL.PushMatrix();
        GL.LoadProjectionMatrix(camera.projectionMatrix);
        GL.modelview = camera.worldToCameraMatrix;
        GL.Begin(GL.LINES);

        Vector3 previous = points[0];
        DrawPreviewCross(previous, 0.16f, new Color(1f, 1f, 1f, 0.9f));

        for (int i = 1; i < points.Length; i++)
        {
            SerializedProperty segment = _segmentsProperty.GetArrayElementAtIndex(i - 1);
            Color color = GetSegmentEnabledProperty(segment).boolValue
                ? GetSegmentColorProperty(segment).colorValue
                : new Color(0.45f, 0.45f, 0.45f, 0.55f);
            color.a = 0.95f;

            GL.Color(color);
            GL.Vertex(previous + Vector3.up * 0.04f);
            GL.Vertex(points[i] + Vector3.up * 0.04f);
            DrawPreviewCross(points[i], 0.14f, color);
            previous = points[i];
        }

        Vector3 currentOffset = EvaluatePreviewWorldDelta(_currentNormalizedTime);
        DrawPreviewCross(currentOffset, 0.22f, Color.white);
        GL.Color(new Color(1f, 1f, 1f, 0.85f));
        GL.Vertex(currentOffset + Vector3.up * 0.02f);
        GL.Vertex(currentOffset + Vector3.up * Mathf.Max(_previewBounds.size.y, 0.5f));

        GL.End();
        GL.PopMatrix();
    }

    private float CalculatePreviewGridExtent()
    {
        Bounds bounds = CalculatePreviewSceneBounds();
        float maxAbs = Mathf.Max(
            PreviewGridMinimumExtent,
            Mathf.Abs(bounds.min.x),
            Mathf.Abs(bounds.max.x),
            Mathf.Abs(bounds.min.z),
            Mathf.Abs(bounds.max.z));

        return Mathf.Ceil(maxAbs + 1f);
    }

    private void DrawPreviewCross(Vector3 center, float size, Color color)
    {
        center.y += 0.06f;
        GL.Color(color);
        GL.Vertex(center + new Vector3(-size, 0f, 0f));
        GL.Vertex(center + new Vector3(size, 0f, 0f));
        GL.Vertex(center + new Vector3(0f, 0f, -size));
        GL.Vertex(center + new Vector3(0f, 0f, size));
    }

    private bool EnsurePreviewLineMaterial()
    {
        if (_previewLineMaterial != null)
        {
            return true;
        }

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null)
        {
            return false;
        }

        _previewLineMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        _previewLineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _previewLineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _previewLineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        _previewLineMaterial.SetInt("_ZWrite", 0);
        return true;
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

        AnimationClip clip = _previewAnimationClipProperty?.objectReferenceValue as AnimationClip;
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

    private void CleanupPreview()
    {
        DestroyPreviewInstance();

        if (_previewRenderUtility != null)
        {
            _previewRenderUtility.Cleanup();
            _previewRenderUtility = null;
        }

        if (_previewLineMaterial != null)
        {
            DestroyImmediate(_previewLineMaterial);
            _previewLineMaterial = null;
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

    private static string FormatVector(Vector3 value)
    {
        return $"({value.x:F2}, {value.y:F2}, {value.z:F2})";
    }

    private static Color GetColorForIndex(int index)
    {
        Color[] palette =
        {
            new Color(0.25f, 0.75f, 1f, 1f),
            new Color(1f, 0.55f, 0.3f, 1f),
            new Color(0.5f, 0.85f, 0.35f, 1f),
            new Color(0.95f, 0.75f, 0.2f, 1f),
            new Color(0.8f, 0.5f, 1f, 1f)
        };

        return palette[index % palette.Length];
    }

    private static AnimationCurve CreateEaseOutCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f, 2f, 2f),
            new Keyframe(1f, 1f, 0f, 0f));
    }

    private static AnimationCurve CreateEaseInCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(1f, 1f, 2f, 2f));
    }

    private static SerializedProperty GetSegmentNameProperty(SerializedProperty segment) => segment.FindPropertyRelative("_name");
    private static SerializedProperty GetSegmentEnabledProperty(SerializedProperty segment) => segment.FindPropertyRelative("_enabled");
    private static SerializedProperty GetSegmentStartProperty(SerializedProperty segment) => segment.FindPropertyRelative("_startNormalizedTime");
    private static SerializedProperty GetSegmentEndProperty(SerializedProperty segment) => segment.FindPropertyRelative("_endNormalizedTime");
    private static SerializedProperty GetSegmentDeltaProperty(SerializedProperty segment) => segment.FindPropertyRelative("_targetDeltaPosition");
    private static SerializedProperty GetSegmentCurveProperty(SerializedProperty segment) => segment.FindPropertyRelative("_distributionCurve");
    private static SerializedProperty GetSegmentColorProperty(SerializedProperty segment) => segment.FindPropertyRelative("_editorColor");
}
