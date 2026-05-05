using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public static class EnemyStateAuthoringAssetOpener
{
#if UNITY_6000_4_OR_NEWER
    [OnOpenAsset]
    public static bool OpenAsset(EntityId instanceID)
    {
        Object openedObject = EditorUtility.EntityIdToObject(instanceID);
        return OpenIfAuthoringAsset(openedObject);
    }
#else
    [OnOpenAsset]
    public static bool OpenAsset(int instanceID)
    {
        Object openedObject = EditorUtility.InstanceIDToObject(instanceID);
        return OpenIfAuthoringAsset(openedObject);
    }
#endif

    private static bool OpenIfAuthoringAsset(Object openedObject)
    {
        if (openedObject is not EnemyStateAuthoringAsset authoringAsset)
        {
            return false;
        }

        EnemyStateAuthoringWindow.Open(authoringAsset);
        return true;
    }
}

public sealed class EnemyStateAuthoringWindow : EditorWindow
{
    private const string WindowTitle = "Enemy State Authoring";
    private const float InspectorWidth = 280f;
    private const float AnimationBarHeight = 48f;
    private const float ActionHeaderHeight = 24f;
    private const float ActionTrackHeight = 28f;
    private const float ActionTrackGap = 4f;
    private const float TimelinePadding = 12f;
    private const float TimelineHandleWidth = 8f;
    private const float MinCameraDistance = 0.25f;
    private const float MaxCameraDistance = 80f;

    private enum ActionBlockDragMode
    {
        None,
        Move,
        ResizeStart,
        ResizeEnd
    }

    [SerializeField] private EnemyStateAuthoringAsset _asset;
    [SerializeField] private string _animatorStateName;
    [SerializeField] private GameObject _modelAsset;
    [SerializeField] private AnimationClip _animationClip;
    [SerializeField] private bool _applyRootMotion;
    [SerializeField] private Color _backgroundColor = new(0.16f, 0.17f, 0.19f, 1f);
    [SerializeField] private bool _showGrid = true;
    [SerializeField] private List<EnemyStateAuthoringActionBlockPreviewData> _actionBlocks = new();
    [SerializeField] private int _selectedActionBlockIndex = -1;

    private PreviewRenderUtility _previewUtility;
    private GameObject _loadedModelAsset;
    private GameObject _previewModel;
    private GameObject _floorObject;
    private GameObject _gridObject;
    private Material _floorMaterial;
    private Material _gridMaterial;
    private Material _previewLineMaterial;
    private Mesh _floorMesh;
    private Mesh _gridMesh;
    private Vector3 _previewRootPosition;
    private Quaternion _previewRootRotation = Quaternion.identity;
    private Animator _previewAnimator;
    private Transform _previewAnimatorTransform;
    private Vector3 _previewAnimatorRootPosition;
    private Quaternion _previewAnimatorRootRotation = Quaternion.identity;
    private PlayableGraph _animationGraph;
    private AnimationClipPlayable _animationPlayable;

    private readonly HashSet<KeyCode> _pressedKeys = new();

    private Vector3 _cameraTarget = new(0f, 1f, 0f);
    private float _cameraDistance = 5f;
    private float _cameraYaw = 180f;
    private float _cameraPitch = 12f;
    private bool _isRightMouseHeld;
    private bool _isMiddleMouseHeld;
    private double _lastEditorTime;
    private float _animationNormalizedTime;
    private bool _isAnimationPlaying;
    private ActionBlockDragMode _actionBlockDragMode;
    private int _draggingActionBlockIndex = -1;
    private float _actionBlockDragOffset;

    public static void Open(EnemyStateAuthoringAsset asset)
    {
        EnemyStateAuthoringWindow window = GetWindow<EnemyStateAuthoringWindow>();
        window.titleContent = new GUIContent(asset.name);
        window.minSize = new Vector2(520f, 360f);
        window.SetAsset(asset);
        window.Show();
    }

    private void OnEnable()
    {
        wantsMouseMove = true;
        _lastEditorTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += OnEditorUpdate;
        AssemblyReloadEvents.beforeAssemblyReload += CleanupPreviewWorld;
        LoadAssetState();
    }

    private void OnDisable()
    {
        SaveAssetState();
        EditorApplication.update -= OnEditorUpdate;
        AssemblyReloadEvents.beforeAssemblyReload -= CleanupPreviewWorld;
        CleanupPreviewWorld();
    }

    private void OnGUI()
    {
        if (_asset == null)
        {
            DrawNoAssetState();
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawInspectorPanel();

            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                Rect previewRect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                EditorGUI.DrawRect(previewRect, _backgroundColor);

                HandleModelDragAndDrop(previewRect);
                HandlePreviewInput(previewRect);

                EnsurePreviewWorld();
                RebuildPreviewModelIfNeeded();
                SampleAnimationClip();
                UpdatePreviewCamera(previewRect);
                DrawPreview(previewRect);

                if (_modelAsset == null)
                {
                    DrawEmptyState(previewRect);
                }

                DrawSelectedActionBlockOverlay(previewRect);
                DrawAnimationControls();
            }
        }
    }

    private void SetAsset(EnemyStateAuthoringAsset asset)
    {
        if (_asset == asset)
        {
            return;
        }

        SaveAssetState();
        CleanupAnimationGraph();
        DestroyPreviewModel();

        _asset = asset;
        titleContent = new GUIContent(_asset == null ? WindowTitle : _asset.name);
        LoadAssetState();
        Repaint();
    }

    private void LoadAssetState()
    {
        if (_asset == null)
        {
            return;
        }

        _animatorStateName = _asset.EditorAnimatorStateName;
        _modelAsset = _asset.EditorModelAsset;
        _animationClip = _asset.EditorAnimationClip;
        _applyRootMotion = _asset.EditorApplyRootMotion;
        _backgroundColor = _asset.EditorBackgroundColor;
        _showGrid = _asset.EditorShowGrid;
        _cameraTarget = _asset.EditorCameraTarget;
        _cameraDistance = Mathf.Clamp(_asset.EditorCameraDistance, MinCameraDistance, MaxCameraDistance);
        _cameraYaw = _asset.EditorCameraYaw;
        _cameraPitch = Mathf.Clamp(_asset.EditorCameraPitch, -85f, 85f);
        _animationNormalizedTime = Mathf.Clamp01(_asset.EditorAnimationNormalizedTime);
        _isAnimationPlaying = _asset.EditorIsAnimationPlaying;
        _actionBlocks = _asset.EditorActionBlocks;
        _selectedActionBlockIndex = Mathf.Clamp(_asset.EditorSelectedActionBlockIndex, -1, _actionBlocks.Count - 1);
    }

    private void SaveAssetState()
    {
        if (_asset == null)
        {
            return;
        }

        _asset.EditorAnimatorStateName = _animatorStateName;
        _asset.EditorModelAsset = _modelAsset;
        _asset.EditorAnimationClip = _animationClip;
        _asset.EditorApplyRootMotion = _applyRootMotion;
        _asset.EditorBackgroundColor = _backgroundColor;
        _asset.EditorShowGrid = _showGrid;
        _asset.EditorCameraTarget = _cameraTarget;
        _asset.EditorCameraDistance = _cameraDistance;
        _asset.EditorCameraYaw = _cameraYaw;
        _asset.EditorCameraPitch = _cameraPitch;
        _asset.EditorAnimationNormalizedTime = _animationNormalizedTime;
        _asset.EditorIsAnimationPlaying = _isAnimationPlaying;
        _asset.EditorSelectedActionBlockIndex = _selectedActionBlockIndex;
        _asset.SyncActionBlocksFromEditor();
        EditorUtility.SetDirty(_asset);
    }

    private void DrawInspectorPanel()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(InspectorWidth), GUILayout.ExpandHeight(true)))
        {
            EditorGUILayout.LabelField(_asset.name, EditorStyles.boldLabel);
            EditorGUILayout.Space(6f);

            EditorGUI.BeginChangeCheck();
            string animatorStateName = EditorGUILayout.TextField("Animator State", _animatorStateName);
            if (EditorGUI.EndChangeCheck())
            {
                _animatorStateName = animatorStateName;
                SaveAssetState();
            }

            EditorGUILayout.Space(8f);

            EditorGUI.BeginChangeCheck();
            GameObject selectedModel = EditorGUILayout.ObjectField("Preview Model", _modelAsset, typeof(GameObject), false) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                SetModel(selectedModel);
            }

            EditorGUI.BeginChangeCheck();
            AnimationClip selectedClip = EditorGUILayout.ObjectField("Animation Clip", _animationClip, typeof(AnimationClip), false) as AnimationClip;
            if (EditorGUI.EndChangeCheck())
            {
                SetAnimationClip(selectedClip);
            }

            EditorGUILayout.Space(8f);

            EditorGUI.BeginChangeCheck();
            bool applyRootMotion = EditorGUILayout.Toggle("Apply Root Motion", _applyRootMotion);
            if (EditorGUI.EndChangeCheck())
            {
                _applyRootMotion = applyRootMotion;
                if (!_applyRootMotion)
                {
                    ResetPreviewRootTransforms();
                }

                SaveAssetState();
                Repaint();
            }

            EditorGUI.BeginChangeCheck();
            bool showGrid = EditorGUILayout.Toggle("Show Grid", _showGrid);
            if (EditorGUI.EndChangeCheck())
            {
                _showGrid = showGrid;
                SaveAssetState();
            }

            EditorGUI.BeginChangeCheck();
            Color backgroundColor = EditorGUILayout.ColorField("Background", _backgroundColor);
            if (EditorGUI.EndChangeCheck())
            {
                _backgroundColor = backgroundColor;
                if (_previewUtility != null)
                {
                    _previewUtility.camera.backgroundColor = _backgroundColor;
                }

                SaveAssetState();
            }

            EditorGUILayout.Space(8f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Frame"))
                {
                    FramePreviewModel();
                }

                if (GUILayout.Button("Reset Camera"))
                {
                    ResetCamera();
                }
            }

            GUILayout.FlexibleSpace();
        }
    }

    private void DrawNoAssetState()
    {
        Rect rect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(rect, new Color(0.16f, 0.17f, 0.19f, 1f));
        GUI.Label(
            new Rect(rect.x + 24f, rect.y + 24f, rect.width - 48f, 80f),
            "Open an Enemy State Authoring asset from the Project window.",
            EditorStyles.whiteLargeLabel);
    }

    private void DrawSelectedActionBlockOverlay(Rect previewRect)
    {
        if (_selectedActionBlockIndex < 0 || _selectedActionBlockIndex >= _actionBlocks.Count)
        {
            return;
        }

        EnemyStateAuthoringActionBlockPreviewData block = _actionBlocks[_selectedActionBlockIndex];
        float overlayHeight = block.Type switch
        {
            EnemyStateAuthoringActionBlockType.AdditionalRootmotion => 394f,
            EnemyStateAuthoringActionBlockType.DodgeTiming => 390f,
            _ => 254f
        };
        Rect overlayRect = new(12f, 12f, 300f, overlayHeight);

        GUI.BeginGroup(previewRect);
        EditorGUI.DrawRect(overlayRect, new Color(0.105f, 0.105f, 0.115f, 0.98f));
        GUI.Box(overlayRect, GUIContent.none, EditorStyles.helpBox);
        GUILayout.BeginArea(new Rect(overlayRect.x + 8f, overlayRect.y + 6f, overlayRect.width - 16f, overlayRect.height - 12f));
        using (new EditorGUILayout.VerticalScope())
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(GetActionBlockLabel(block.Type), EditorStyles.boldLabel);
                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(22f)))
                {
                    _selectedActionBlockIndex = -1;
                    SaveAssetState();
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUI.BeginChangeCheck();
            block.StartTime = Mathf.Clamp(EditorGUILayout.Slider("Start", block.StartTime, 0f, 1f), 0f, 0.97f);
            block.EndTime = Mathf.Clamp(EditorGUILayout.Slider("End", block.EndTime, 0f, 1f), block.StartTime + 0.03f, 1f);

            block.PreviewValue = EditorGUILayout.FloatField(GetPreviewValueLabel(block.Type), block.PreviewValue);
            block.Memo = EditorGUILayout.TextField("Memo", block.Memo);

            EditorGUILayout.Space(2f);
            DrawActionSpecificPreviewFields(block.Type);
            EditorGUILayout.Space(8f);

            if (GUILayout.Button("Delete Block", GUILayout.Height(24f)))
            {
                DeleteActionBlock(_selectedActionBlockIndex);
                GUIUtility.ExitGUI();
            }

            if (EditorGUI.EndChangeCheck())
            {
                SaveAssetState();
                Repaint();
            }
        }
        GUILayout.EndArea();
        GUI.EndGroup();
    }

    private void DrawActionSpecificPreviewFields(EnemyStateAuthoringActionBlockType type)
    {
        if (type == EnemyStateAuthoringActionBlockType.AdditionalRootmotion)
        {
            DrawAdditionalRootmotionPreviewFields();
            return;
        }

        if (type == EnemyStateAuthoringActionBlockType.DodgeTiming)
        {
            DrawDodgeTimingPreviewFields();
            return;
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("Attack ID", "Preview_Attack");
            EditorGUILayout.Toggle("Enable Hitbox", true);
        }
    }

    private void DrawDodgeTimingPreviewFields()
    {
        EnemyStateAuthoringActionBlockPreviewData block = _actionBlocks[_selectedActionBlockIndex];

        block.PreviewDodgeArea = EditorGUILayout.Toggle("Show Area In Preview", block.PreviewDodgeArea);
        block.DodgeAreaBindingMode = (EnemyStateAuthoringDodgeAreaBindingMode)EditorGUILayout.EnumPopup("Binding", block.DodgeAreaBindingMode);

        if (block.DodgeAreaBindingMode == EnemyStateAuthoringDodgeAreaBindingMode.AttachToTransform)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Transform");
                string label = string.IsNullOrEmpty(block.DodgeAttachTransformPath) ? "(Root)" : block.DodgeAttachTransformPath;
                if (GUILayout.Button(label, EditorStyles.popup))
                {
                    ShowTransformSelectionMenu(block);
                }
            }
        }

        block.DodgeAreaPositionOffset = EditorGUILayout.Vector3Field("Position Offset", block.DodgeAreaPositionOffset);
        block.DodgeAreaRotationEuler = EditorGUILayout.Vector3Field("Rotation", block.DodgeAreaRotationEuler);
        block.DodgeAreaSize = ClampDodgeAreaSize(EditorGUILayout.Vector3Field("Size", block.DodgeAreaSize));
    }

    private void DrawAdditionalRootmotionPreviewFields()
    {
        EnemyStateAuthoringActionBlockPreviewData block = _actionBlocks[_selectedActionBlockIndex];

        block.PreviewRootmotion = EditorGUILayout.Toggle("Apply In Preview", block.PreviewRootmotion);
        block.RootmotionSpace = (EnemyStateAuthoringRootmotionSpace)EditorGUILayout.EnumPopup("Space", block.RootmotionSpace);
        block.ConstrainRootmotionToGroundPlane = EditorGUILayout.Toggle("Ground Plane", block.ConstrainRootmotionToGroundPlane);
        block.RootmotionDirection = EditorGUILayout.Vector3Field("Direction", block.RootmotionDirection);
        block.RootmotionDistance = Mathf.Max(0f, EditorGUILayout.FloatField("Distance", block.RootmotionDistance));
        block.RootmotionCurve = EditorGUILayout.CurveField("Speed Curve", block.RootmotionCurve);
    }

    private void ShowTransformSelectionMenu(EnemyStateAuthoringActionBlockPreviewData block)
    {
        GenericMenu menu = new();
        menu.AddItem(new GUIContent("(Root)"), string.IsNullOrEmpty(block.DodgeAttachTransformPath), () =>
        {
            block.DodgeAttachTransformPath = string.Empty;
            SaveAssetState();
            Repaint();
        });

        if (_previewModel == null)
        {
            menu.AddDisabledItem(new GUIContent("No preview model"));
            menu.ShowAsContext();
            return;
        }

        Transform[] transforms = _previewModel.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] == _previewModel.transform)
            {
                continue;
            }

            string path = BuildTransformPath(_previewModel.transform, transforms[i]);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            bool selected = block.DodgeAttachTransformPath == path;
            menu.AddItem(new GUIContent(path), selected, () =>
            {
                block.DodgeAttachTransformPath = path;
                SaveAssetState();
                Repaint();
            });
        }

        menu.ShowAsContext();
    }

    private static Vector3 ClampDodgeAreaSize(Vector3 size)
    {
        return new Vector3(
            Mathf.Max(0.01f, size.x),
            Mathf.Max(0.01f, size.y),
            Mathf.Max(0.01f, size.z));
    }

    private string GetPreviewValueLabel(EnemyStateAuthoringActionBlockType type)
    {
        return type switch
        {
            EnemyStateAuthoringActionBlockType.AttackTiming => "Power Preview",
            EnemyStateAuthoringActionBlockType.DodgeTiming => "Window Preview",
            _ => "Weight Preview"
        };
    }

    private void DrawAnimationControls()
    {
        Rect totalRect = GUILayoutUtility.GetRect(1f, GetAnimationAreaHeight(), GUILayout.ExpandWidth(true));
        Rect labelRect = new(totalRect.x + 8f, totalRect.y + 3f, 120f, 18f);
        Rect playButtonRect = new(labelRect.xMax + 4f, totalRect.y + 3f, 56f, 18f);
        Rect resetButtonRect = new(playButtonRect.xMax + 2f, totalRect.y + 3f, 52f, 18f);
        Rect timeLabelRect = new(totalRect.xMax - 128f, totalRect.y + 3f, 120f, 18f);
        Rect timelineRect = new(
            totalRect.x + TimelinePadding,
            totalRect.y + 28f,
            totalRect.width - (TimelinePadding * 2f),
            10f);

        EditorGUI.DrawRect(totalRect, new Color(0.18f, 0.18f, 0.18f, 1f));
        GUI.Label(labelRect, "Timeline", EditorStyles.miniBoldLabel);

        using (new EditorGUI.DisabledScope(_animationClip == null))
        {
            if (GUI.Button(playButtonRect, _isAnimationPlaying ? "Pause" : "Play", EditorStyles.miniButton))
            {
                _isAnimationPlaying = !_isAnimationPlaying;
                _lastEditorTime = EditorApplication.timeSinceStartup;
                SaveAssetState();
            }

            if (GUI.Button(resetButtonRect, "Reset", EditorStyles.miniButton))
            {
                _isAnimationPlaying = false;
                _animationNormalizedTime = 0f;
                SaveAssetState();
                Repaint();
            }
        }

        GUI.Label(timeLabelRect, GetAnimationTimeLabel(), EditorStyles.miniLabel);
        DrawAnimationTimeline(timelineRect);
        DrawActionBlockTimeline(totalRect);
    }

    private float GetAnimationAreaHeight()
    {
        int visibleTrackCount = Mathf.Max(1, _actionBlocks.Count);
        return AnimationBarHeight + ActionHeaderHeight + 8f + (visibleTrackCount * (ActionTrackHeight + ActionTrackGap));
    }

    private void DrawAnimationTimeline(Rect timelineRect)
    {
        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        HandleAnimationTimelineInput(Event.current, controlId, timelineRect);

        EditorGUI.DrawRect(timelineRect, new Color(0.08f, 0.08f, 0.08f, 1f));

        float currentX = Mathf.Lerp(timelineRect.xMin, timelineRect.xMax, _animationNormalizedTime);
        Rect playedRect = new(timelineRect.xMin, timelineRect.y, Mathf.Max(0f, currentX - timelineRect.xMin), timelineRect.height);
        Rect handleRect = GetAnimationTimelineHandleRect(timelineRect);

        EditorGUI.DrawRect(playedRect, new Color(0.32f, 0.54f, 0.84f, 1f));
        EditorGUI.DrawRect(handleRect, new Color(0.92f, 0.95f, 1f, 1f));

        if (_animationClip == null)
        {
            GUI.Label(new Rect(timelineRect.x + 4f, timelineRect.y - 2f, timelineRect.width - 8f, 14f), "Animation Clip", EditorStyles.centeredGreyMiniLabel);
        }
    }

    private void DrawActionBlockTimeline(Rect totalRect)
    {
        Rect headerRect = new(totalRect.x, totalRect.y + AnimationBarHeight, totalRect.width, ActionHeaderHeight);
        Rect addAttackRect = new(headerRect.x + 76f, headerRect.y + 3f, 104f, 18f);
        Rect addDodgeRect = new(addAttackRect.xMax + 4f, headerRect.y + 3f, 112f, 18f);
        Rect addRootmotionRect = new(addDodgeRect.xMax + 4f, headerRect.y + 3f, 142f, 18f);
        Rect trackAreaRect = new(
            totalRect.x + TimelinePadding,
            headerRect.yMax + 4f,
            totalRect.width - (TimelinePadding * 2f),
            totalRect.yMax - headerRect.yMax - 6f);

        EditorGUI.DrawRect(headerRect, new Color(0.16f, 0.16f, 0.16f, 1f));
        GUI.Label(new Rect(headerRect.x + 8f, headerRect.y + 4f, 64f, 16f), "Actions", EditorStyles.miniBoldLabel);

        if (GUI.Button(addAttackRect, "Attack Timing", EditorStyles.miniButton))
        {
            AddActionBlock(EnemyStateAuthoringActionBlockType.AttackTiming);
        }

        if (GUI.Button(addDodgeRect, "Dodge Timing", EditorStyles.miniButton))
        {
            AddActionBlock(EnemyStateAuthoringActionBlockType.DodgeTiming);
        }

        if (GUI.Button(addRootmotionRect, "Additional Rootmotion", EditorStyles.miniButton))
        {
            AddActionBlock(EnemyStateAuthoringActionBlockType.AdditionalRootmotion);
        }

        if (_actionBlocks.Count == 0)
        {
            Rect placeholderRect = new(trackAreaRect.x, trackAreaRect.y, trackAreaRect.width, ActionTrackHeight);
            EditorGUI.DrawRect(placeholderRect, new Color(0.11f, 0.11f, 0.115f, 1f));
            GUI.Label(placeholderRect, "Add an action block to create a new layer.", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        for (int i = 0; i < _actionBlocks.Count; i++)
        {
            Rect trackRect = new(
                trackAreaRect.x,
                trackAreaRect.y + (i * (ActionTrackHeight + ActionTrackGap)),
                trackAreaRect.width,
                ActionTrackHeight);

            DrawActionBlockTrack(i, trackRect);
        }

        float playheadX = Mathf.Lerp(trackAreaRect.xMin, trackAreaRect.xMax, _animationNormalizedTime);
        EditorGUI.DrawRect(new Rect(playheadX - 1f, trackAreaRect.y, 2f, trackAreaRect.height), new Color(0.9f, 0.95f, 1f, 0.75f));
    }

    private void DrawActionBlockTrack(int index, Rect trackRect)
    {
        EnemyStateAuthoringActionBlockPreviewData block = _actionBlocks[index];
        int controlId = GUIUtility.GetControlID(FocusType.Passive, trackRect);
        Rect blockRect = GetActionBlockRect(trackRect, block);
        Rect leftHandleRect = new(blockRect.x, blockRect.y, TimelineHandleWidth, blockRect.height);
        Rect rightHandleRect = new(blockRect.xMax - TimelineHandleWidth, blockRect.y, TimelineHandleWidth, blockRect.height);

        HandleActionBlockInput(Event.current, controlId, index, trackRect, blockRect, leftHandleRect, rightHandleRect);

        EditorGUI.DrawRect(trackRect, new Color(0.105f, 0.105f, 0.11f, 1f));
        Color blockColor = GetActionBlockColor(block.Type);
        if (index == _selectedActionBlockIndex)
        {
            blockColor = Color.Lerp(blockColor, Color.white, 0.18f);
        }

        EditorGUI.DrawRect(blockRect, blockColor);
        EditorGUI.DrawRect(leftHandleRect, new Color(1f, 1f, 1f, 0.28f));
        EditorGUI.DrawRect(rightHandleRect, new Color(1f, 1f, 1f, 0.28f));
        GUI.Label(new Rect(blockRect.x + 8f, blockRect.y + 5f, blockRect.width - 16f, 16f), GetActionBlockLabel(block.Type), EditorStyles.whiteMiniLabel);

        if (Event.current.type == EventType.Repaint)
        {
            EditorGUIUtility.AddCursorRect(leftHandleRect, MouseCursor.ResizeHorizontal, controlId);
            EditorGUIUtility.AddCursorRect(rightHandleRect, MouseCursor.ResizeHorizontal, controlId);
            EditorGUIUtility.AddCursorRect(blockRect, MouseCursor.MoveArrow, controlId);
        }
    }

    private void HandleActionBlockInput(
        Event evt,
        int controlId,
        int index,
        Rect trackRect,
        Rect blockRect,
        Rect leftHandleRect,
        Rect rightHandleRect)
    {
        switch (evt.GetTypeForControl(controlId))
        {
            case EventType.MouseDown:
                if (evt.button != 0)
                {
                    return;
                }

                if (leftHandleRect.Contains(evt.mousePosition))
                {
                    BeginActionBlockDrag(controlId, index, ActionBlockDragMode.ResizeStart, trackRect);
                    evt.Use();
                }
                else if (rightHandleRect.Contains(evt.mousePosition))
                {
                    BeginActionBlockDrag(controlId, index, ActionBlockDragMode.ResizeEnd, trackRect);
                    evt.Use();
                }
                else if (blockRect.Contains(evt.mousePosition))
                {
                    BeginActionBlockDrag(controlId, index, ActionBlockDragMode.Move, trackRect);
                    evt.Use();
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl != controlId || _draggingActionBlockIndex != index)
                {
                    return;
                }

                UpdateActionBlockDrag(evt.mousePosition, trackRect);
                evt.Use();
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl != controlId || _draggingActionBlockIndex != index)
                {
                    return;
                }

                GUIUtility.hotControl = 0;
                _draggingActionBlockIndex = -1;
                _actionBlockDragMode = ActionBlockDragMode.None;
                evt.Use();
                break;
        }
    }

    private void BeginActionBlockDrag(int controlId, int index, ActionBlockDragMode dragMode, Rect trackRect)
    {
        GUIUtility.hotControl = controlId;
        _selectedActionBlockIndex = index;
        _draggingActionBlockIndex = index;
        _actionBlockDragMode = dragMode;
        float mouseTime = GetNormalizedTimeFromTrack(Event.current.mousePosition, trackRect);
        _actionBlockDragOffset = mouseTime - _actionBlocks[index].StartTime;
        SaveAssetState();
        Repaint();
    }

    private void UpdateActionBlockDrag(Vector2 mousePosition, Rect trackRect)
    {
        if (_draggingActionBlockIndex < 0 || _draggingActionBlockIndex >= _actionBlocks.Count)
        {
            return;
        }

        EnemyStateAuthoringActionBlockPreviewData block = _actionBlocks[_draggingActionBlockIndex];
        float mouseTime = GetNormalizedTimeFromTrack(mousePosition, trackRect);
        float duration = Mathf.Max(0.03f, block.EndTime - block.StartTime);

        switch (_actionBlockDragMode)
        {
            case ActionBlockDragMode.ResizeStart:
                block.StartTime = Mathf.Clamp(mouseTime, 0f, block.EndTime - 0.03f);
                break;

            case ActionBlockDragMode.ResizeEnd:
                block.EndTime = Mathf.Clamp(mouseTime, block.StartTime + 0.03f, 1f);
                break;

            case ActionBlockDragMode.Move:
                block.StartTime = Mathf.Clamp(mouseTime - _actionBlockDragOffset, 0f, 1f - duration);
                block.EndTime = block.StartTime + duration;
                break;
        }

        SaveAssetState();
        Repaint();
    }

    private Rect GetActionBlockRect(Rect trackRect, EnemyStateAuthoringActionBlockPreviewData block)
    {
        float xMin = Mathf.Lerp(trackRect.xMin, trackRect.xMax, Mathf.Clamp01(block.StartTime));
        float xMax = Mathf.Lerp(trackRect.xMin, trackRect.xMax, Mathf.Clamp01(block.EndTime));
        return new Rect(xMin, trackRect.y + 3f, Mathf.Max(14f, xMax - xMin), trackRect.height - 6f);
    }

    private float GetNormalizedTimeFromTrack(Vector2 mousePosition, Rect trackRect)
    {
        return Mathf.Clamp01(Mathf.InverseLerp(trackRect.xMin, trackRect.xMax, mousePosition.x));
    }

    private void AddActionBlock(EnemyStateAuthoringActionBlockType type)
    {
        float startTime = Mathf.Clamp01(_animationNormalizedTime);
        float endTime = Mathf.Clamp01(startTime + 0.18f);
        if (endTime - startTime < 0.03f)
        {
            startTime = Mathf.Max(0f, endTime - 0.18f);
        }

        _actionBlocks.Add(new EnemyStateAuthoringActionBlockPreviewData
        {
            Type = type,
            StartTime = startTime,
            EndTime = endTime,
            Memo = string.Empty,
            PreviewRootmotion = true,
            RootmotionDirection = Vector3.forward,
            RootmotionDistance = 1f,
            RootmotionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f),
            PreviewDodgeArea = true,
            DodgeAreaBindingMode = EnemyStateAuthoringDodgeAreaBindingMode.World,
            DodgeAttachTransformPath = string.Empty,
            DodgeAreaPositionOffset = new Vector3(0f, 1f, 1.5f),
            DodgeAreaRotationEuler = Vector3.zero,
            DodgeAreaSize = new Vector3(1.5f, 1f, 1.5f)
        });

        _selectedActionBlockIndex = _actionBlocks.Count - 1;
        SaveAssetState();
        Repaint();
    }

    private void DeleteActionBlock(int index)
    {
        if (index < 0 || index >= _actionBlocks.Count)
        {
            return;
        }

        _actionBlocks.RemoveAt(index);
        _selectedActionBlockIndex = Mathf.Min(index, _actionBlocks.Count - 1);
        _draggingActionBlockIndex = -1;
        _actionBlockDragMode = ActionBlockDragMode.None;
        GUIUtility.hotControl = 0;
        SaveAssetState();
        Repaint();
    }

    private string GetActionBlockLabel(EnemyStateAuthoringActionBlockType type)
    {
        return type switch
        {
            EnemyStateAuthoringActionBlockType.AttackTiming => "Attack Timing",
            EnemyStateAuthoringActionBlockType.DodgeTiming => "Dodge Timing",
            _ => "Additional Rootmotion"
        };
    }

    private Color GetActionBlockColor(EnemyStateAuthoringActionBlockType type)
    {
        return type switch
        {
            EnemyStateAuthoringActionBlockType.AttackTiming => new Color(0.86f, 0.28f, 0.22f, 1f),
            EnemyStateAuthoringActionBlockType.DodgeTiming => new Color(0.26f, 0.55f, 0.95f, 1f),
            _ => new Color(0.34f, 0.78f, 0.42f, 1f)
        };
    }

    private void HandleAnimationTimelineInput(Event evt, int controlId, Rect timelineRect)
    {
        switch (evt.GetTypeForControl(controlId))
        {
            case EventType.MouseDown:
                if (_animationClip == null || evt.button != 0 || !timelineRect.Contains(evt.mousePosition))
                {
                    return;
                }

                GUIUtility.hotControl = controlId;
                _isAnimationPlaying = false;
                SetAnimationTimeFromMouse(evt.mousePosition, timelineRect);
                evt.Use();
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl != controlId)
                {
                    return;
                }

                SetAnimationTimeFromMouse(evt.mousePosition, timelineRect);
                evt.Use();
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl != controlId)
                {
                    return;
                }

                GUIUtility.hotControl = 0;
                evt.Use();
                break;

            case EventType.Repaint:
                EditorGUIUtility.AddCursorRect(timelineRect, MouseCursor.SlideArrow, controlId);
                EditorGUIUtility.AddCursorRect(GetAnimationTimelineHandleRect(timelineRect), MouseCursor.ResizeHorizontal, controlId);
                break;
        }
    }

    private void SetAnimationTimeFromMouse(Vector2 mousePosition, Rect timelineRect)
    {
        _animationNormalizedTime = Mathf.Clamp01(Mathf.InverseLerp(timelineRect.xMin, timelineRect.xMax, mousePosition.x));
        SaveAssetState();
        Repaint();
    }

    private Rect GetAnimationTimelineHandleRect(Rect timelineRect)
    {
        float x = Mathf.Lerp(timelineRect.xMin, timelineRect.xMax, _animationNormalizedTime) - (TimelineHandleWidth * 0.5f);
        return new Rect(x, timelineRect.y - 4f, TimelineHandleWidth, timelineRect.height + 8f);
    }

    private string GetAnimationTimeLabel()
    {
        if (_animationClip == null)
        {
            return "0.000s / 0.000s";
        }

        float currentTime = GetCurrentAnimationTime();
        return $"{currentTime:F3}s / {_animationClip.length:F3}s";
    }

    private void EnsurePreviewWorld()
    {
        if (_previewUtility != null)
        {
            return;
        }

        _previewUtility = new PreviewRenderUtility
        {
            camera =
            {
                nearClipPlane = 0.01f,
                farClipPlane = 250f,
                fieldOfView = 35f,
                clearFlags = CameraClearFlags.SolidColor,
                backgroundColor = _backgroundColor,
                allowHDR = true,
                allowMSAA = true
            },
            ambientColor = new Color(0.28f, 0.29f, 0.31f, 1f)
        };

        ConfigureLights();
        CreatePreviewFloor();
    }

    private void SetAnimationClip(AnimationClip clip)
    {
        if (_animationClip == clip)
        {
            return;
        }

        _animationClip = clip;
        _animationNormalizedTime = 0f;
        _isAnimationPlaying = false;
        CleanupAnimationGraph();
        SaveAssetState();

        Repaint();
    }

    private void SampleAnimationClip()
    {
        if (_animationClip == null || _previewModel == null)
        {
            return;
        }

        if (EnsureAnimationGraph())
        {
            _previewAnimator.applyRootMotion = _applyRootMotion;
            _animationPlayable.SetTime(GetCurrentAnimationTime());
            _animationGraph.Evaluate();

            if (!_applyRootMotion)
            {
                ResetPreviewRootTransforms();
            }

            ApplyAdditionalRootmotionPreview();

            return;
        }

        if (!_applyRootMotion)
        {
            ResetPreviewRootTransforms();
        }

        ApplyAdditionalRootmotionPreview();
    }

    private void ApplyAdditionalRootmotionPreview()
    {
        if (_previewModel == null || _actionBlocks == null)
        {
            return;
        }

        Vector3 cumulativeDelta = EvaluatePreviewAdditionalRootmotionDelta(_animationNormalizedTime);
        _previewModel.transform.position += cumulativeDelta;
    }

    private Vector3 EvaluatePreviewAdditionalRootmotionDelta(float normalizedTime)
    {
        Vector3 result = Vector3.zero;
        if (_actionBlocks == null)
        {
            return result;
        }

        for (int i = 0; i < _actionBlocks.Count; i++)
        {
            EnemyStateAuthoringActionBlockPreviewData block = _actionBlocks[i];
            if (block == null ||
                block.Type != EnemyStateAuthoringActionBlockType.AdditionalRootmotion ||
                !block.PreviewRootmotion)
            {
                continue;
            }

            Vector3 delta = EvaluatePreviewAdditionalRootmotionBlock(block, normalizedTime);
            result += delta;
        }

        return result;
    }

    private Vector3 EvaluatePreviewAdditionalRootmotionBlock(EnemyStateAuthoringActionBlockPreviewData block, float normalizedTime)
    {
        float ratio = EvaluateActionBlockCurveRatio(block, normalizedTime);
        Vector3 direction = block.RootmotionDirection == Vector3.zero ? Vector3.forward : block.RootmotionDirection.normalized;
        Vector3 delta = direction * Mathf.Max(0f, block.RootmotionDistance) * ratio;

        if (block.ConstrainRootmotionToGroundPlane)
        {
            delta.y = 0f;
        }

        return block.RootmotionSpace == EnemyStateAuthoringRootmotionSpace.Local
            ? _previewRootRotation * delta
            : delta;
    }

    private float EvaluateActionBlockCurveRatio(EnemyStateAuthoringActionBlockPreviewData block, float normalizedTime)
    {
        if (block.EndTime <= block.StartTime)
        {
            return normalizedTime >= block.EndTime ? 1f : 0f;
        }

        if (normalizedTime <= block.StartTime)
        {
            return 0f;
        }

        if (normalizedTime >= block.EndTime)
        {
            return 1f;
        }

        AnimationCurve curve = block.RootmotionCurve;
        if (curve == null || curve.length == 0)
        {
            return Mathf.InverseLerp(block.StartTime, block.EndTime, normalizedTime);
        }

        float t = Mathf.InverseLerp(block.StartTime, block.EndTime, normalizedTime);
        return Mathf.Clamp01(curve.Evaluate(t));
    }

    private bool EnsureAnimationGraph()
    {
        if (_animationClip == null || _previewModel == null)
        {
            return false;
        }

        if (_previewAnimator == null)
        {
            InitializePreviewAnimator();
        }

        if (_previewAnimator == null)
        {
            return false;
        }

        if (_animationGraph.IsValid() && _animationPlayable.IsValid())
        {
            return true;
        }

        CleanupAnimationGraph();

        _animationGraph = PlayableGraph.Create("Enemy State Authoring Animation Preview");
        _animationGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        _animationPlayable = AnimationClipPlayable.Create(_animationGraph, _animationClip);
        AnimationPlayableOutput animationOutput = AnimationPlayableOutput.Create(_animationGraph, "Animation", _previewAnimator);
        animationOutput.SetSourcePlayable(_animationPlayable);
        _animationGraph.Play();
        return true;
    }

    private void InitializePreviewAnimator()
    {
        if (_previewModel == null)
        {
            return;
        }

        _previewAnimator = _previewModel.GetComponentInChildren<Animator>(true);
        if (_previewAnimator == null)
        {
            _previewAnimator = _previewModel.AddComponent<Animator>();
        }

        _previewAnimator.runtimeAnimatorController = null;
        _previewAnimator.applyRootMotion = _applyRootMotion;
        _previewAnimator.stabilizeFeet = false;
        _previewAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        _previewAnimator.enabled = true;

        _previewAnimatorTransform = _previewAnimator.transform;
        _previewAnimatorRootPosition = _previewAnimatorTransform.position;
        _previewAnimatorRootRotation = _previewAnimatorTransform.rotation;
    }

    private void ResetPreviewRootTransforms()
    {
        if (_previewModel != null)
        {
            _previewModel.transform.SetPositionAndRotation(_previewRootPosition, _previewRootRotation);
        }

        if (_previewAnimatorTransform != null)
        {
            _previewAnimatorTransform.SetPositionAndRotation(_previewAnimatorRootPosition, _previewAnimatorRootRotation);
        }
    }

    private void CleanupAnimationGraph()
    {
        if (_animationGraph.IsValid())
        {
            _animationGraph.Destroy();
        }

        _animationPlayable = default;
    }

    private float GetCurrentAnimationTime()
    {
        if (_animationClip == null || _animationClip.length <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(_animationNormalizedTime) * _animationClip.length;
    }

    private void ConfigureLights()
    {
        Light[] lights = _previewUtility.lights;
        if (lights == null || lights.Length == 0)
        {
            return;
        }

        lights[0].enabled = true;
        lights[0].type = LightType.Directional;
        lights[0].intensity = 1.2f;
        lights[0].transform.rotation = Quaternion.Euler(40f, 135f, 0f);

        if (lights.Length < 2)
        {
            return;
        }

        lights[1].enabled = true;
        lights[1].type = LightType.Directional;
        lights[1].intensity = 0.45f;
        lights[1].color = new Color(0.72f, 0.8f, 1f, 1f);
        lights[1].transform.rotation = Quaternion.Euler(330f, 225f, 0f);
    }

    private void CreatePreviewFloor()
    {
        _floorMesh = CreateFloorMesh(20f);
        _gridMesh = CreateGridMesh(10, 1f);
        _floorMaterial = CreateColorMaterial(new Color(0.22f, 0.23f, 0.24f, 1f));
        _gridMaterial = CreateColorMaterial(new Color(0.43f, 0.45f, 0.48f, 1f));

        _floorObject = CreateMeshObject("Enemy State Authoring Floor", _floorMesh, _floorMaterial);
        _gridObject = CreateMeshObject("Enemy State Authoring Grid", _gridMesh, _gridMaterial);

        _previewUtility.AddSingleGO(_floorObject);
        _previewUtility.AddSingleGO(_gridObject);
    }

    private GameObject CreateMeshObject(string objectName, Mesh mesh, Material material)
    {
        GameObject go = new(objectName)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        MeshFilter meshFilter = go.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;
        return go;
    }

    private void RebuildPreviewModelIfNeeded()
    {
        if (_modelAsset == null)
        {
            DestroyPreviewModel();
            return;
        }

        if (_previewModel != null && _loadedModelAsset == _modelAsset)
        {
            return;
        }

        DestroyPreviewModel();
        _previewModel = _previewUtility.InstantiatePrefabInScene(_modelAsset);
        if (_previewModel == null)
        {
            return;
        }

        _previewModel.hideFlags = HideFlags.HideAndDontSave;
        _loadedModelAsset = _modelAsset;
        _previewModel.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        _previewRootPosition = _previewModel.transform.position;
        _previewRootRotation = _previewModel.transform.rotation;

        foreach (Camera camera in _previewModel.GetComponentsInChildren<Camera>(true))
        {
            camera.enabled = false;
        }

        foreach (AudioListener audioListener in _previewModel.GetComponentsInChildren<AudioListener>(true))
        {
            audioListener.enabled = false;
        }

        foreach (SkinnedMeshRenderer skinnedMeshRenderer in _previewModel.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            skinnedMeshRenderer.updateWhenOffscreen = true;
        }

        FramePreviewModel();
    }

    private void SetModel(GameObject model)
    {
        if (_modelAsset == model)
        {
            return;
        }

        _modelAsset = model;
        DestroyPreviewModel();
        SaveAssetState();
        Repaint();
    }

    private void DestroyPreviewModel()
    {
        if (_previewModel == null)
        {
            return;
        }

        CleanupAnimationGraph();
        DestroyImmediate(_previewModel);
        _previewModel = null;
        _loadedModelAsset = null;
        _previewAnimator = null;
        _previewAnimatorTransform = null;
    }

    private void FramePreviewModel()
    {
        if (_previewModel == null)
        {
            ResetCamera();
            return;
        }

        Bounds bounds = CalculatePreviewBounds(_previewModel);
        float radius = Mathf.Max(0.5f, bounds.extents.magnitude);

        _cameraTarget = bounds.center;
        _cameraDistance = Mathf.Clamp(radius * 2.4f, 2f, MaxCameraDistance);
        _cameraYaw = 180f;
        _cameraPitch = 12f;
        SaveAssetState();
        Repaint();
    }

    private void ResetCamera()
    {
        _cameraTarget = new Vector3(0f, 1f, 0f);
        _cameraDistance = 5f;
        _cameraYaw = 180f;
        _cameraPitch = 12f;
        _pressedKeys.Clear();
        _isRightMouseHeld = false;
        _isMiddleMouseHeld = false;
        SaveAssetState();
        Repaint();
    }

    private Bounds CalculatePreviewBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(root.transform.position + Vector3.up, Vector3.one * 2f);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private void UpdatePreviewCamera(Rect previewRect)
    {
        if (_previewUtility == null)
        {
            return;
        }

        Camera camera = _previewUtility.camera;
        camera.backgroundColor = _backgroundColor;
        camera.aspect = Mathf.Max(1f, previewRect.width) / Mathf.Max(1f, previewRect.height);

        Quaternion cameraRotation = Quaternion.Euler(_cameraPitch, _cameraYaw, 0f);
        Vector3 cameraPosition = _cameraTarget - (cameraRotation * Vector3.forward * _cameraDistance);
        camera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        camera.transform.LookAt(_cameraTarget, Vector3.up);
    }

    private void DrawPreview(Rect previewRect)
    {
        if (_previewUtility == null || Event.current.type != EventType.Repaint)
        {
            return;
        }

        SetPreviewWorldVisibility();

        _previewUtility.BeginPreview(previewRect, GUIStyle.none);
        _previewUtility.camera.Render();
        DrawDodgeTimingPreviewAreas(_previewUtility.camera);
        Texture previewTexture = _previewUtility.EndPreview();
        GUI.DrawTexture(previewRect, previewTexture, ScaleMode.StretchToFill, false);
    }

    private void DrawDodgeTimingPreviewAreas(Camera camera)
    {
        if (camera == null || _previewModel == null || _actionBlocks == null)
        {
            return;
        }

        EnsurePreviewLineMaterial();
        if (_previewLineMaterial == null)
        {
            return;
        }

        _previewLineMaterial.SetPass(0);
        GL.PushMatrix();
        GL.LoadProjectionMatrix(camera.projectionMatrix);
        GL.modelview = camera.worldToCameraMatrix;
        GL.Begin(GL.LINES);

        for (int i = 0; i < _actionBlocks.Count; i++)
        {
            EnemyStateAuthoringActionBlockPreviewData block = _actionBlocks[i];
            if (block == null ||
                block.Type != EnemyStateAuthoringActionBlockType.DodgeTiming ||
                !block.PreviewDodgeArea)
            {
                continue;
            }

            bool isSelected = i == _selectedActionBlockIndex;
            bool isActive = IsAnimationTimeInsideBlock(block);
            if (!isSelected && !isActive)
            {
                continue;
            }

            Color color = isActive
                ? new Color(0.2f, 0.72f, 1f, 1f)
                : new Color(0.2f, 0.72f, 1f, 0.42f);
            if (isSelected)
            {
                color = Color.Lerp(color, Color.white, 0.22f);
            }

            DrawDodgeAreaBox(block, color);
        }

        GL.End();
        GL.PopMatrix();
    }

    private void DrawDodgeAreaBox(EnemyStateAuthoringActionBlockPreviewData block, Color color)
    {
        if (!TryGetDodgeAreaMatrix(block, out Matrix4x4 matrix))
        {
            return;
        }

        Vector3 half = ClampDodgeAreaSize(block.DodgeAreaSize) * 0.5f;
        Vector3 p000 = matrix.MultiplyPoint3x4(new Vector3(-half.x, -half.y, -half.z));
        Vector3 p001 = matrix.MultiplyPoint3x4(new Vector3(-half.x, -half.y, half.z));
        Vector3 p010 = matrix.MultiplyPoint3x4(new Vector3(-half.x, half.y, -half.z));
        Vector3 p011 = matrix.MultiplyPoint3x4(new Vector3(-half.x, half.y, half.z));
        Vector3 p100 = matrix.MultiplyPoint3x4(new Vector3(half.x, -half.y, -half.z));
        Vector3 p101 = matrix.MultiplyPoint3x4(new Vector3(half.x, -half.y, half.z));
        Vector3 p110 = matrix.MultiplyPoint3x4(new Vector3(half.x, half.y, -half.z));
        Vector3 p111 = matrix.MultiplyPoint3x4(new Vector3(half.x, half.y, half.z));

        GL.Color(color);
        DrawPreviewLine(p000, p001);
        DrawPreviewLine(p001, p011);
        DrawPreviewLine(p011, p010);
        DrawPreviewLine(p010, p000);
        DrawPreviewLine(p100, p101);
        DrawPreviewLine(p101, p111);
        DrawPreviewLine(p111, p110);
        DrawPreviewLine(p110, p100);
        DrawPreviewLine(p000, p100);
        DrawPreviewLine(p001, p101);
        DrawPreviewLine(p010, p110);
        DrawPreviewLine(p011, p111);
    }

    private bool TryGetDodgeAreaMatrix(EnemyStateAuthoringActionBlockPreviewData block, out Matrix4x4 matrix)
    {
        matrix = Matrix4x4.identity;
        Quaternion localRotation = Quaternion.Euler(block.DodgeAreaRotationEuler);

        if (block.DodgeAreaBindingMode == EnemyStateAuthoringDodgeAreaBindingMode.AttachToTransform)
        {
            Transform attachTransform = FindTransformByPath(_previewModel.transform, block.DodgeAttachTransformPath);
            if (attachTransform == null)
            {
                return false;
            }

            matrix = Matrix4x4.TRS(
                attachTransform.TransformPoint(block.DodgeAreaPositionOffset),
                attachTransform.rotation * localRotation,
                Vector3.one);
            return true;
        }

        matrix = Matrix4x4.TRS(block.DodgeAreaPositionOffset, localRotation, Vector3.one);
        return true;
    }

    private bool IsAnimationTimeInsideBlock(EnemyStateAuthoringActionBlockPreviewData block)
    {
        float time = Mathf.Clamp01(_animationNormalizedTime);
        return time >= block.StartTime && time <= block.EndTime;
    }

    private static void DrawPreviewLine(Vector3 from, Vector3 to)
    {
        GL.Vertex(from);
        GL.Vertex(to);
    }

    private void SetPreviewWorldVisibility()
    {
        if (_floorObject != null)
        {
            _floorObject.SetActive(_showGrid);
        }

        if (_gridObject != null)
        {
            _gridObject.SetActive(_showGrid);
        }
    }

    private void DrawEmptyState(Rect previewRect)
    {
        Rect labelRect = new(previewRect.x + 24f, previewRect.y + 24f, previewRect.width - 48f, 80f);
        GUI.Label(labelRect, "Drop a model prefab or FBX here, or assign one from the left panel.", EditorStyles.whiteLargeLabel);
    }

    private void HandleModelDragAndDrop(Rect previewRect)
    {
        Event evt = Event.current;
        if (!previewRect.Contains(evt.mousePosition))
        {
            return;
        }

        if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
        {
            return;
        }

        GameObject draggedModel = GetDraggedModel();
        if (draggedModel == null)
        {
            return;
        }

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            SetModel(draggedModel);
        }

        evt.Use();
    }

    private GameObject GetDraggedModel()
    {
        foreach (Object draggedObject in DragAndDrop.objectReferences)
        {
            if (draggedObject is GameObject gameObject)
            {
                return gameObject;
            }
        }

        return null;
    }

    private void HandlePreviewInput(Rect previewRect)
    {
        Event evt = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive, previewRect);

        switch (evt.GetTypeForControl(controlId))
        {
            case EventType.MouseDown:
                if (!previewRect.Contains(evt.mousePosition))
                {
                    return;
                }

                if (evt.button == 1)
                {
                    GUIUtility.hotControl = controlId;
                    _isRightMouseHeld = true;
                    evt.Use();
                }
                else if (evt.button == 2)
                {
                    GUIUtility.hotControl = controlId;
                    _isMiddleMouseHeld = true;
                    evt.Use();
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl != controlId)
                {
                    return;
                }

                if (_isRightMouseHeld)
                {
                    _cameraYaw += evt.delta.x * 0.22f;
                    _cameraPitch += evt.delta.y * 0.22f;
                    _cameraPitch = Mathf.Clamp(_cameraPitch, -85f, 85f);
                    SaveAssetState();
                    Repaint();
                    evt.Use();
                }
                else if (_isMiddleMouseHeld)
                {
                    PanCamera(evt.delta);
                    SaveAssetState();
                    Repaint();
                    evt.Use();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl != controlId)
                {
                    return;
                }

                if (evt.button == 1)
                {
                    _isRightMouseHeld = false;
                    _pressedKeys.Clear();
                    GUIUtility.hotControl = 0;
                    evt.Use();
                }
                else if (evt.button == 2)
                {
                    _isMiddleMouseHeld = false;
                    GUIUtility.hotControl = 0;
                    evt.Use();
                }
                break;

            case EventType.ScrollWheel:
                if (!previewRect.Contains(evt.mousePosition))
                {
                    return;
                }

                _cameraDistance = Mathf.Clamp(_cameraDistance * (1f + evt.delta.y * 0.05f), MinCameraDistance, MaxCameraDistance);
                SaveAssetState();
                Repaint();
                evt.Use();
                break;

            case EventType.KeyDown:
                if (_isRightMouseHeld && IsCameraMovementKey(evt.keyCode))
                {
                    _pressedKeys.Add(evt.keyCode);
                    evt.Use();
                }
                break;

            case EventType.KeyUp:
                if (IsCameraMovementKey(evt.keyCode))
                {
                    _pressedKeys.Remove(evt.keyCode);
                    evt.Use();
                }
                break;
        }
    }

    private void PanCamera(Vector2 delta)
    {
        if (_previewUtility == null)
        {
            return;
        }

        Transform cameraTransform = _previewUtility.camera.transform;
        float panScale = Mathf.Max(0.15f, _cameraDistance) * 0.0025f;
        _cameraTarget += (-cameraTransform.right * delta.x + cameraTransform.up * delta.y) * panScale;
    }

    private void OnEditorUpdate()
    {
        double now = EditorApplication.timeSinceStartup;
        float deltaTime = Mathf.Clamp((float)(now - _lastEditorTime), 0f, 0.05f);
        _lastEditorTime = now;

        bool needsRepaint = AdvanceAnimation(deltaTime);

        if (_isRightMouseHeld && _pressedKeys.Count > 0 && _previewUtility != null)
        {
            needsRepaint |= MoveCamera(deltaTime);
        }

        if (needsRepaint)
        {
            Repaint();
        }
    }

    private bool AdvanceAnimation(float deltaTime)
    {
        if (!_isAnimationPlaying)
        {
            return false;
        }

        if (_animationClip == null || _animationClip.length <= 0f)
        {
            _isAnimationPlaying = false;
            return true;
        }

        _animationNormalizedTime += deltaTime / _animationClip.length;
        if (_animationNormalizedTime > 1f)
        {
            _animationNormalizedTime -= Mathf.Floor(_animationNormalizedTime);
        }

        SaveAssetState();
        return true;
    }

    private bool MoveCamera(float deltaTime)
    {
        if (_previewUtility == null)
        {
            return false;
        }

        Quaternion cameraRotation = Quaternion.Euler(_cameraPitch, _cameraYaw, 0f);
        Vector3 movement = Vector3.zero;

        if (_pressedKeys.Contains(KeyCode.W))
        {
            movement += cameraRotation * Vector3.forward;
        }

        if (_pressedKeys.Contains(KeyCode.S))
        {
            movement += cameraRotation * Vector3.back;
        }

        if (_pressedKeys.Contains(KeyCode.A))
        {
            movement += cameraRotation * Vector3.left;
        }

        if (_pressedKeys.Contains(KeyCode.D))
        {
            movement += cameraRotation * Vector3.right;
        }

        if (_pressedKeys.Contains(KeyCode.E) || _pressedKeys.Contains(KeyCode.Space))
        {
            movement += Vector3.up;
        }

        if (_pressedKeys.Contains(KeyCode.Q) || _pressedKeys.Contains(KeyCode.LeftControl))
        {
            movement += Vector3.down;
        }

        if (movement == Vector3.zero)
        {
            return false;
        }

        float speed = Mathf.Max(1f, _cameraDistance * 0.6f);
        _cameraTarget += movement.normalized * speed * deltaTime;
        SaveAssetState();
        return true;
    }

    private static bool IsCameraMovementKey(KeyCode keyCode)
    {
        return keyCode == KeyCode.W ||
            keyCode == KeyCode.A ||
            keyCode == KeyCode.S ||
            keyCode == KeyCode.D ||
            keyCode == KeyCode.Q ||
            keyCode == KeyCode.E ||
            keyCode == KeyCode.Space ||
            keyCode == KeyCode.LeftControl;
    }

    private static Mesh CreateFloorMesh(float size)
    {
        float half = size * 0.5f;
        Mesh mesh = new()
        {
            name = "Enemy State Authoring Floor Mesh",
            hideFlags = HideFlags.HideAndDontSave,
            vertices = new[]
            {
                new Vector3(-half, 0f, -half),
                new Vector3(-half, 0f, half),
                new Vector3(half, 0f, half),
                new Vector3(half, 0f, -half)
            },
            triangles = new[] { 0, 1, 2, 0, 2, 3 },
            uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            }
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateGridMesh(int halfLineCount, float spacing)
    {
        List<Vector3> vertices = new();
        List<int> indices = new();
        float halfSize = halfLineCount * spacing;

        for (int i = -halfLineCount; i <= halfLineCount; i++)
        {
            float coordinate = i * spacing;
            AddLine(vertices, indices, new Vector3(-halfSize, 0.01f, coordinate), new Vector3(halfSize, 0.01f, coordinate));
            AddLine(vertices, indices, new Vector3(coordinate, 0.01f, -halfSize), new Vector3(coordinate, 0.01f, halfSize));
        }

        Mesh mesh = new()
        {
            name = "Enemy State Authoring Grid Mesh",
            hideFlags = HideFlags.HideAndDontSave
        };

        mesh.SetVertices(vertices);
        mesh.SetIndices(indices, MeshTopology.Lines, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddLine(List<Vector3> vertices, List<int> indices, Vector3 from, Vector3 to)
    {
        int index = vertices.Count;
        vertices.Add(from);
        vertices.Add(to);
        indices.Add(index);
        indices.Add(index + 1);
    }

    private void EnsurePreviewLineMaterial()
    {
        if (_previewLineMaterial != null)
        {
            return;
        }

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            return;
        }

        _previewLineMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        _previewLineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _previewLineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _previewLineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        _previewLineMaterial.SetInt("_ZWrite", 0);
    }

    private static Material CreateColorMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        return material;
    }

    private static string BuildTransformPath(Transform root, Transform target)
    {
        if (root == null || target == null)
        {
            return string.Empty;
        }

        if (root == target)
        {
            return root.name;
        }

        List<string> names = new();
        Transform current = target;
        while (current != null && current != root)
        {
            names.Add(current.name);
            current = current.parent;
        }

        if (current != root)
        {
            return target.name;
        }

        names.Reverse();
        return string.Join("/", names);
    }

    private static Transform FindTransformByPath(Transform root, string path)
    {
        if (root == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(path) || path == root.name)
        {
            return root;
        }

        Transform found = root.Find(path);
        if (found != null)
        {
            return found;
        }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (BuildTransformPath(root, transforms[i]) == path)
            {
                return transforms[i];
            }
        }

        return null;
    }

    private void CleanupPreviewWorld()
    {
        CleanupAnimationGraph();
        DestroyPreviewModel();

        DestroyPreviewObject(_floorObject);
        DestroyPreviewObject(_gridObject);
        DestroyPreviewObject(_floorMesh);
        DestroyPreviewObject(_gridMesh);
        DestroyPreviewObject(_floorMaterial);
        DestroyPreviewObject(_gridMaterial);
        DestroyPreviewObject(_previewLineMaterial);

        _floorObject = null;
        _gridObject = null;
        _floorMesh = null;
        _gridMesh = null;
        _floorMaterial = null;
        _gridMaterial = null;
        _previewLineMaterial = null;

        _previewUtility?.Cleanup();
        _previewUtility = null;
        _pressedKeys.Clear();
    }

    private static void DestroyPreviewObject(Object previewObject)
    {
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
        }
    }
}
