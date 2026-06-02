using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageNode : MonoBehaviour
{
    [SerializeField] private string _stageName;
    [SerializeField] private Button _stageButton;

    private NodeState _nodeState;
    private List<StageNode> _nextNode;

    public NodeState NodeState
    {
        get
        {
            return _nodeState;
        }
        private set
        {
            _nodeState = value;
            ChangeNodeStateSequece(_nodeState);
        }
    }

    public void InitNode(string stageName, Vector3 pos)
    {
        NodeState = NodeState.Inactive;
        _stageName = stageName;
        transform.localPosition = pos;
        transform.localRotation = Quaternion.identity;
        _stageButton.onClick.AddListener(OnClickNode);
    }

    public void AddNextNode(StageNode node)
    {
        if (_nextNode == null)
            _nextNode = new List<StageNode>();

        if (_nextNode.Contains(node))
            return;

        _nextNode.Add(node);
    }

    public void ChangeNodeState(NodeState state)
    {
        NodeState = state;
    }

    public void OnClickNode()
    {
        LoadingController.LoadScene(_stageName);
    }

    private void ChangeNodeStateSequece(NodeState state)
    {
        if(state == NodeState.Inactive)
        {
            SetActiveButton(false);
        }
        else if(state == NodeState.Active)
        {
            SetActiveButton(true);
        }
        else if(state == NodeState.Clear)
        {
            SetActiveButton(false);
        }
    }

    private void SetActiveButton(bool active)
    {
        _stageButton.interactable = active;
    }
}
