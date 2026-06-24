using System;
using System.Collections.Generic;
using UnityEngine;

public class NodeInstance
{
    private NodeType _type;
    private NodeState _state;

    public NodeType Type
    {
        get
        {
            return _type;
        }
        set
        {
            _type = value;
            OnChangedNodeData?.Invoke(_type, _state);
        }
    }
    public NodeState State
    {
        get
        {
            return _state;
        }
        set
        {
            _state = value;
            OnChangedNodeData?.Invoke(_type, _state);
        }
    }
    public Vector3 NodePosition;
    public List<NodeInstance> NextNodes;

    public event Action<NodeType, NodeState> OnChangedNodeData;

    public NodeInstance(NodeType type, NodeState state, Vector3 nodePosition)
    {
        this.Type = type;
        this.State = state;
        this.NodePosition = nodePosition;
    }

    public void AddNextNode(NodeInstance nextNode)
    {
        if (NextNodes == null)
            NextNodes = new List<NodeInstance>();

        if (!NextNodes.Contains(nextNode))
            NextNodes.Add(nextNode);
    }

    public void ActiveThisNode()
    {
        State = NodeState.Active;
    }

    public void ClearThisNode()
    {
        State = NodeState.Clear;

        foreach(var node in NextNodes)
        {
            node.ActiveThisNode();
        }
    }
}