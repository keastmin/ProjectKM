using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AttackDodgeTimingWindow))]
public class AttackDodgeTimingWindowEditor : Editor
{
    private const float TimelineHeight = 52f;
    private const float TimelinePadding = 12f;
    private const float TimelineHandleWidth = 10f;

    private enum TimelineDragMode
    {
        None,
        CurrentTime,
        OpenHandle,
        CloseHandle
    }

    private SerializedProperty _previewAnimationClipProperty;
    private SerializedProperty _dodgeDetectColliderIdProperty;
    private SerializedProperty _openDodgeNormalizedTimeProperty;
    private SerializedProperty _closeDodgeNormalizedTimeProperty;

    private GameObject _previewActor;
    private float _currentNormalizedTime;
    private double _lastEditorTime;
    private bool _isPlaying;
    private bool _ownsAnimationMode;
    private TimelineDragMode _dragMode;

    private void OnEnable()
    {
        _previewAnimationClipProperty = serializedObject.FindProperty("PreviewAnimationClip");
        _dodgeDetectColliderIdProperty = serializedObject.FindProperty("DodgeDetectColliderID");
        _openDodgeNormalizedTimeProperty = serializedObject.FindProperty("OpenDodgeNormalizedTime");
        _closeDodgeNormalizedTimeProperty = serializedObject.FindProperty("CloseDodgeNormalizedTime");

        _currentNormalizedTime = Mathf.Clamp01(_openDodgeNormalizedTimeProperty.floatValue);
        _lastEditorTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        StopPreviewSampling();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        ClampWindowProperties();

        DrawDefaultInspectorFields();
        EditorGUILayout.Space(8f);
        DrawPreviewInspector();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawDefaultInspectorFields()
    {
        EditorGUILayout.PropertyField(_previewAnimationClipProperty, new GUIContent("Preview Animation Clip"));
        EditorGUILayout.PropertyField(_dodgeDetectColliderIdProperty, true);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Dodge Window", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        float openTime = _openDodgeNormalizedTimeProperty.floatValue;
        float closeTime = _closeDodgeNormalizedTimeProperty.floatValue;
        EditorGUILayout.MinMaxSlider(new GUIContent("Normalized Range"), ref openTime, ref closeTime, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            _openDodgeNormalizedTimeProperty.floatValue = openTime;
            _closeDodgeNormalizedTimeProperty.floatValue = Mathf.Max(openTime, closeTime);
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(_openDodgeNormalizedTimeProperty, new GUIContent("Open"));
        EditorGUILayout.PropertyField(_closeDodgeNormalizedTimeProperty, new GUIContent("Close"));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPreviewInspector()
    {
        AnimationClip clip = _previewAnimationClipProperty.objectReferenceValue as AnimationClip;

        EditorGUILayout.LabelField("Scene Preview", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        _previewActor = EditorGUILayout.ObjectField(
            new GUIContent("Scene Model"),
            _previewActor,
            typeof(GameObject),
            true) as GameObject;
        if (EditorGUI.EndChangeCheck())
        {
            _isPlaying = false;
            SamplePreviewAtCurrentTime();
        }

        if (_previewActor != null && EditorUtility.IsPersistent(_previewActor))
        {
            EditorGUILayout.HelpBox("Scene Model must be a GameObject from the open scene, not a prefab asset.", MessageType.Warning);
            return;
        }

        if (clip == null)
        {
            EditorGUILayout.HelpBox("Assign a Preview Animation Clip to scrub the dodge timing window.", MessageType.Info);
            StopPreviewSampling();
            return;
        }

        if (_previewActor == null)
        {
            EditorGUILayout.HelpBox("Drag a GameObject from the scene into Scene Model to preview the animation in-place.", MessageType.Info);
            StopPreviewSampling();
            return;
        }

        DrawPreviewToolbar(clip);
        DrawTimeline(clip);
        DrawPreviewActions();
        DrawPreviewSummary(clip);

        if (Event.current.type == EventType.Repaint)
        {
            SamplePreviewAtCurrentTime();
        }
    }

    private void DrawPreviewToolbar(AnimationClip clip)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button(_isPlaying ? "Pause" : "Play", EditorStyles.toolbarButton, GUILayout.Width(60f)))
        {
            _isPlaying = !_isPlaying;
            _lastEditorTime = EditorApplication.timeSinceStartup;
            SamplePreviewAtCurrentTime();
        }

        if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(60f)))
        {
            _isPlaying = false;
            _currentNormalizedTime = 0f;
            SamplePreviewAtCurrentTime();
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label($"Normalized {_currentNormalizedTime:F3}", EditorStyles.miniLabel);
        GUILayout.Space(8f);
        GUILayout.Label($"{(_currentNormalizedTime * clip.length):F3}s", EditorStyles.miniLabel);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawTimeline(AnimationClip clip)
    {
        Rect totalRect = GUILayoutUtility.GetRect(10f, TimelineHeight, GUILayout.ExpandWidth(true));
        Rect timelineRect = new Rect(
            totalRect.x + TimelinePadding,
            totalRect.y + 14f,
            totalRect.width - (TimelinePadding * 2f),
            12f);

        float openTime = _openDodgeNormalizedTimeProperty.floatValue;
        float closeTime = _closeDodgeNormalizedTimeProperty.floatValue;

        Rect openHandleRect = GetHandleRect(timelineRect, openTime);
        Rect closeHandleRect = GetHandleRect(timelineRect, closeTime);
        Rect playheadRect = GetHandleRect(timelineRect, _currentNormalizedTime, 2f);

        HandleTimelineInput(Event.current, GUIUtility.GetControlID(FocusType.Passive), timelineRect, openHandleRect, closeHandleRect);

        EditorGUI.DrawRect(timelineRect, new Color(0.16f, 0.16f, 0.16f));

        float openX = Mathf.Lerp(timelineRect.xMin, timelineRect.xMax, openTime);
        float closeX = Mathf.Lerp(timelineRect.xMin, timelineRect.xMax, closeTime);
        Rect windowRect = new Rect(openX, timelineRect.y, Mathf.Max(2f, closeX - openX), timelineRect.height);
        EditorGUI.DrawRect(windowRect, new Color(0.2f, 0.75f, 0.35f, 0.9f));

        EditorGUI.DrawRect(openHandleRect, new Color(0.95f, 0.75f, 0.2f));
        EditorGUI.DrawRect(closeHandleRect, new Color(0.95f, 0.4f, 0.2f));
        EditorGUI.DrawRect(playheadRect, new Color(0.88f, 0.95f, 1f));

        GUI.Label(new Rect(totalRect.x + TimelinePadding, totalRect.y - 2f, 120f, 16f), "Timeline Scrub", EditorStyles.miniBoldLabel);
        GUI.Label(new Rect(timelineRect.xMin, timelineRect.yMax + 10f, 40f, 16f), "0.000", EditorStyles.miniLabel);
        GUI.Label(new Rect(timelineRect.center.x - 20f, timelineRect.yMax + 10f, 40f, 16f), "0.500", EditorStyles.centeredGreyMiniLabel);
        GUI.Label(new Rect(timelineRect.xMax - 40f, timelineRect.yMax + 10f, 40f, 16f), "1.000", EditorStyles.miniLabel);

        EditorGUI.BeginChangeCheck();
        float normalized = EditorGUILayout.Slider("Current Time", _currentNormalizedTime, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            _isPlaying = false;
            _currentNormalizedTime = normalized;
            SamplePreviewAtCurrentTime();
        }

        if (clip.length > 0f)
        {
            EditorGUILayout.LabelField("Current Seconds", $"{(_currentNormalizedTime * clip.length):F3}s");
        }
    }

    private void DrawPreviewActions()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Open To Current"))
        {
            _openDodgeNormalizedTimeProperty.floatValue = Mathf.Min(_currentNormalizedTime, _closeDodgeNormalizedTimeProperty.floatValue);
            serializedObject.ApplyModifiedProperties();
        }

        if (GUILayout.Button("Set Close To Current"))
        {
            _closeDodgeNormalizedTimeProperty.floatValue = Mathf.Max(_currentNormalizedTime, _openDodgeNormalizedTimeProperty.floatValue);
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPreviewSummary(AnimationClip clip)
    {
        float openTime = _openDodgeNormalizedTimeProperty.floatValue;
        float closeTime = _closeDodgeNormalizedTimeProperty.floatValue;

        EditorGUILayout.HelpBox(
            $"Clip Length: {clip.length:F3}s\n" +
            $"Open: {openTime:F3} ({(openTime * clip.length):F3}s)\n" +
            $"Close: {closeTime:F3} ({(closeTime * clip.length):F3}s)\n" +
            $"Current: {_currentNormalizedTime:F3} ({(_currentNormalizedTime * clip.length):F3}s)",
            MessageType.None);
    }

    private void HandleTimelineInput(Event evt, int controlId, Rect timelineRect, Rect openHandleRect, Rect closeHandleRect)
    {
        switch (evt.GetTypeForControl(controlId))
        {
            case EventType.MouseDown:
                if (evt.button != 0 || (!timelineRect.Contains(evt.mousePosition) && !openHandleRect.Contains(evt.mousePosition) && !closeHandleRect.Contains(evt.mousePosition)))
                {
                    return;
                }

                GUIUtility.hotControl = controlId;
                _isPlaying = false;
                _dragMode = ResolveDragMode(evt.mousePosition, openHandleRect, closeHandleRect);
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
                EditorGUIUtility.AddCursorRect(openHandleRect, MouseCursor.ResizeHorizontal, controlId);
                EditorGUIUtility.AddCursorRect(closeHandleRect, MouseCursor.ResizeHorizontal, controlId);
                EditorGUIUtility.AddCursorRect(timelineRect, MouseCursor.SlideArrow, controlId);
                break;
        }
    }

    private TimelineDragMode ResolveDragMode(Vector2 mousePosition, Rect openHandleRect, Rect closeHandleRect)
    {
        if (openHandleRect.Contains(mousePosition))
        {
            return TimelineDragMode.OpenHandle;
        }

        if (closeHandleRect.Contains(mousePosition))
        {
            return TimelineDragMode.CloseHandle;
        }

        return TimelineDragMode.CurrentTime;
    }

    private void UpdateTimelineFromMouse(Vector2 mousePosition, Rect timelineRect)
    {
        float normalized = Mathf.Clamp01(Mathf.InverseLerp(timelineRect.xMin, timelineRect.xMax, mousePosition.x));

        switch (_dragMode)
        {
            case TimelineDragMode.OpenHandle:
                _openDodgeNormalizedTimeProperty.floatValue = Mathf.Min(normalized, _closeDodgeNormalizedTimeProperty.floatValue);
                _currentNormalizedTime = _openDodgeNormalizedTimeProperty.floatValue;
                serializedObject.ApplyModifiedProperties();
                break;

            case TimelineDragMode.CloseHandle:
                _closeDodgeNormalizedTimeProperty.floatValue = Mathf.Max(normalized, _openDodgeNormalizedTimeProperty.floatValue);
                _currentNormalizedTime = _closeDodgeNormalizedTimeProperty.floatValue;
                serializedObject.ApplyModifiedProperties();
                break;

            default:
                _currentNormalizedTime = normalized;
                break;
        }

        SamplePreviewAtCurrentTime();
        Repaint();
    }

    private Rect GetHandleRect(Rect timelineRect, float normalizedTime, float width = TimelineHandleWidth)
    {
        float x = Mathf.Lerp(timelineRect.xMin, timelineRect.xMax, normalizedTime) - (width * 0.5f);
        return new Rect(x, timelineRect.y - 4f, width, timelineRect.height + 8f);
    }

    private void OnEditorUpdate()
    {
        if (!_isPlaying)
        {
            _lastEditorTime = EditorApplication.timeSinceStartup;
            return;
        }

        AnimationClip clip = _previewAnimationClipProperty?.objectReferenceValue as AnimationClip;
        if (clip == null || clip.length <= 0f || _previewActor == null)
        {
            _isPlaying = false;
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        double deltaTime = now - _lastEditorTime;
        _lastEditorTime = now;

        _currentNormalizedTime += (float)(deltaTime / clip.length);
        if (_currentNormalizedTime > 1f)
        {
            _currentNormalizedTime -= Mathf.Floor(_currentNormalizedTime);
        }

        SamplePreviewAtCurrentTime();
        Repaint();
    }

    private void SamplePreviewAtCurrentTime()
    {
        AnimationClip clip = _previewAnimationClipProperty?.objectReferenceValue as AnimationClip;
        if (clip == null || _previewActor == null || EditorUtility.IsPersistent(_previewActor))
        {
            StopPreviewSampling();
            return;
        }

        if (!_ownsAnimationMode && !AnimationMode.InAnimationMode())
        {
            AnimationMode.StartAnimationMode();
            _ownsAnimationMode = true;
        }

        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(_previewActor, clip, Mathf.Clamp01(_currentNormalizedTime) * clip.length);
        AnimationMode.EndSampling();

        SceneView.RepaintAll();
    }

    private void StopPreviewSampling()
    {
        if (_ownsAnimationMode && AnimationMode.InAnimationMode())
        {
            AnimationMode.StopAnimationMode();
        }

        _ownsAnimationMode = false;
    }

    private void ClampWindowProperties()
    {
        _openDodgeNormalizedTimeProperty.floatValue = Mathf.Clamp01(_openDodgeNormalizedTimeProperty.floatValue);
        _closeDodgeNormalizedTimeProperty.floatValue = Mathf.Clamp01(_closeDodgeNormalizedTimeProperty.floatValue);

        if (_closeDodgeNormalizedTimeProperty.floatValue < _openDodgeNormalizedTimeProperty.floatValue)
        {
            _closeDodgeNormalizedTimeProperty.floatValue = _openDodgeNormalizedTimeProperty.floatValue;
        }
    }
}
