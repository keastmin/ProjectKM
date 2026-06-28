using System.Collections.Generic;
using UnityEngine;

public class NodeMapView : MonoBehaviour
{
    [SerializeField] private SceneSwitchRequester _sceneSwitchRequester;
    [SerializeField] private Node _nodePrefab;
    [SerializeField] private Transform _nodesTransform;

    private HashSet<NodeInstance> _createdNode;

    private void Awake()
    {
        InactiveNodeView();
    }

    public void SetActiveNodeView(bool active)
    {
        _nodesTransform.gameObject.SetActive(active);
    }

    public void CreateNodeView(NodeInstance baseNode)
    {
        Debug.Log("노드 월드 배치 수행");
        _createdNode = new HashSet<NodeInstance>();

        if(_sceneSwitchRequester == null)
        {
            Debug.LogError("씬 전환 요청자가 없음");
        }

        if (baseNode.NextNodes != null)
        {
            foreach (var node in baseNode.NextNodes)
            {
                InstantiateNode(node, _sceneSwitchRequester);
            }
        }
    }

    private void InstantiateNode(NodeInstance node, SceneSwitchRequester sceneSwitchRequester)
    {
        if (_createdNode.Contains(node))
            return;

        Node nodeView = Instantiate(_nodePrefab, node.NodePosition, Quaternion.identity, _nodesTransform);
        nodeView.InitializeNode(node, sceneSwitchRequester);
        _createdNode.Add(node);

        if(node.NextNodes != null)
        {
            foreach(var nextNode in node.NextNodes)
            {
                InstantiateNode(nextNode, sceneSwitchRequester);
            }
        }
    }

    private void InactiveNodeView()
    {
        SetActiveNodeView(false);
    }
}