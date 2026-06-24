using System.Collections.Generic;
using UnityEngine;

public class NodeMapView : MonoBehaviour
{
    [SerializeField] private Node _nodePrefab;
    [SerializeField] private Transform _nodesTransform;

    private HashSet<NodeInstance> _createdNode;

    public void CreateNodeView(NodeInstance baseNode)
    {
        _createdNode = new HashSet<NodeInstance>();

        if (baseNode.NextNodes != null)
        {
            foreach (var node in baseNode.NextNodes)
            {
                InstantiateNode(node);
            }
        }
    }

    private void InstantiateNode(NodeInstance node)
    {
        if (_createdNode.Contains(node))
            return;

        Node nodeView = Instantiate(_nodePrefab, node.NodePosition, Quaternion.identity);
        nodeView.InitializeNode(node);
        _createdNode.Add(node);

        if(node.NextNodes != null)
        {
            foreach(var nextNode in node.NextNodes)
            {
                InstantiateNode(nextNode);
            }
        }
    }
}