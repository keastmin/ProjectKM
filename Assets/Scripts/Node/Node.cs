using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class Node : MonoBehaviour
{
    [SerializeField] private MeshRenderer _nodeObjectMeshRenderer;
    [SerializeField] private Material _bossMaterial;
    [SerializeField] private Material _eliteMaterial;
    [SerializeField] private Material _allyMaterial;
    [SerializeField] private Material _eventMaterial;
    [SerializeField] private Material _normalMaterial;
    [SerializeField] private Material _upgradeMaterial;
    [SerializeField] private Material _inactiveMaterial;
    [SerializeField] private Material _clearMaterial;

    private NodeInstance _instance;

    private SceneSwitchRequester _sceneSwitchRequester;

    private void OnEnable()
    {
        BindEvent();
    }

    private void OnDisable()
    {
        UnbindEvent();
    }

    public void InitializeNode(NodeInstance instance, SceneSwitchRequester sceneSwitchRequester)
    {
        _instance = instance;
        _sceneSwitchRequester = sceneSwitchRequester;
        BindEvent();
        SetNodeVisual(_instance.Type, _instance.State);
    }

    public void MouseClickOnThisNode()
    {
        RequestEnterStage();
    }

    public void MouseInToThisNode()
    {

    }

    public void MouseOutFromThisNode()
    {

    }

    private void BindEvent()
    {
        if (_instance == null)
            return;

        BindNodeDataChangeEvent();
    }

    private void UnbindEvent()
    {
        if (_instance == null)
            return;

        UnbindNodeDataChangeEvent();
    }

    private void BindNodeDataChangeEvent()
    {
        _instance.OnChangedNodeData -= SetNodeVisual;
        _instance.OnChangedNodeData += SetNodeVisual;
    }

    private void UnbindNodeDataChangeEvent()
    {
        _instance.OnChangedNodeData -= SetNodeVisual;
    }

    private void SetNodeVisual(NodeType type, NodeState state)
    {
        if (_nodeObjectMeshRenderer == null)
        {
            Debug.LogError("NodeInstance나 MeshRenderer가 없음");
            return;
        }

        Material mat = _normalMaterial;

        if (_instance.State == NodeState.Inactive)
        {
            mat = _inactiveMaterial;
        }
        else if (_instance.State == NodeState.Clear)
        {
            mat = _clearMaterial;
        }
        else if(_instance.State == NodeState.Active)
        {
            if (_instance.Type == NodeType.Boss) mat = _bossMaterial;
            else if (_instance.Type == NodeType.EliteCombat) mat = _eliteMaterial;
            else if (_instance.Type == NodeType.NormalCombat) mat = _normalMaterial;
            else if (_instance.Type == NodeType.Event) mat = _eventMaterial;
            else if (_instance.Type == NodeType.AllyEncounter) mat = _allyMaterial;
            else if (_instance.Type == NodeType.Upgrade) mat = _upgradeMaterial;
        }

        _nodeObjectMeshRenderer.material = mat;
    }

    private void RequestEnterStage()
    {
        if (_instance.State != NodeState.Active)
            return;

        if(_instance.Type == NodeType.NormalCombat)
        {
            _sceneSwitchRequester.SwitchCombatScene();
        }
    }
}