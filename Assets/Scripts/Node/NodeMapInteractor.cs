using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class NodeMapInteractor : MonoBehaviour
{
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private InputModeManager _inputModeManager;
    [SerializeField] private LayerMask _nodeLayer;
    [SerializeField] private float _rayDistance;

    private Node _detectedNode;

    private void Update()
    {
        if (_mainCamera == null || _inputModeManager == null)
            return;

        if (_inputModeManager.CurrentState != InputState.NodeMap)
            return;

        NodeInteractionSequence();
    }

    public void InitializeNodeMapInteractor(GameRunContext context)
    {
        _mainCamera = context.MainCamera;
        _inputModeManager = context.InputModeManager;
    }

    private void NodeInteractionSequence()
    {
        if (IsMouseOnUI())
        {
            NewNodeDetected(null);
            return;
        }

        Node thisFrameDetectedNode;
        if (IsMouseOnNode(out thisFrameDetectedNode))
        {
            NewNodeDetected(thisFrameDetectedNode);
            if (Input.GetMouseButtonDown(0))
            {
                _detectedNode.MouseClickOnThisNode();
            }
        }
        else
        {
            NewNodeDetected(thisFrameDetectedNode);
        }
    }

    private bool IsMouseOnUI()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return true;
        return false;
    }

    private bool IsMouseOnNode(out Node node)
    {
        node = null;

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, _rayDistance, _nodeLayer))
        {
            node = hit.collider.GetComponentInParent<Node>();
            if (node != null)
                return true;
        }

        return false;
    }

    private void NewNodeDetected(Node thisFrameDetectedNode)
    {
        if (_detectedNode == null)
        {
            if(thisFrameDetectedNode != null)
            {
                thisFrameDetectedNode.MouseInToThisNode();
                _detectedNode = thisFrameDetectedNode;
            }
        }
        else
        {
            if(thisFrameDetectedNode == null)
            {
                _detectedNode.MouseOutFromThisNode();
                _detectedNode = null;
            }
        }
    }
}
