using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class NodeMapGenerator : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private Collider _worldCollider;
    [SerializeField] private Vector3 _gridOffset = Vector3.zero;
    [SerializeField][Min(1)] private int _rowSize = 50; // 가로줄 행
    [SerializeField][Min(1)] private int _colSize = 50; // 세로줄 열

    [Header("Generate")]
    [SerializeField] private int _generateNodeCount = 52; 

    [Header("Preview")]
    [SerializeField] private bool _drawGrid = false;
    [SerializeField] private float _gizmoHeightAdditional = 0.1f;

    private Vector3 _minPosition;
    private Vector3 _maxPosition;
    private float _gridWidth;
    private float _gridHeight;
    private float _cellWidth;
    private float _cellHeight;
    private float _halfCellWidth => _cellWidth / 2f;
    private float _halfCellHeight => _cellHeight / 2f;
    private NodeInstance _baseNode;
    private NodeInstance[,] _nodeGrid;

    public NodeInstance BaseNode => _baseNode;

    private void OnValidate()
    {
        InitializeNodeMap();
    }

    private void Awake()
    {
        OnValidate();
    }

    private void InitializeNodeMap()
    {
        CacheVariable();
        _nodeGrid = new NodeInstance[_rowSize, _colSize];
    }

    private void CacheVariable()
    {
        _gridWidth = _worldCollider.bounds.size.x;
        _gridHeight = _worldCollider.bounds.size.z;
        _cellWidth = _gridWidth / _colSize;
        _cellHeight = _gridHeight / _rowSize;
        _minPosition = _worldCollider.bounds.min;
        _maxPosition = _worldCollider.bounds.max;
    }

    public void GenerateNodeMap()
    {
        Debug.Log("노드맵 생성 수행");
        Vector3 centerPos = GetCellPosition((int)(_rowSize / 2f), (int)(_colSize / 2f));
        int startX = (int)(_colSize / 2f);
        int startZ = (int)(_rowSize / 2f);

        // 내부적 시작노드 숨김
        _baseNode = new NodeInstance(NodeType.Start, NodeState.Clear, centerPos);
        NodeInstance prevNode = _baseNode;

        for (int i = startX + 1; i < _colSize; i++)
        {
            Vector3 pos = GetCellPosition(startZ, i);
            NodeInstance newNode = new NodeInstance(NodeType.NormalCombat, NodeState.Inactive, pos);
            _nodeGrid[startZ, i] = newNode;
            prevNode.AddNextNode(newNode);
            prevNode = newNode;
        }

        _baseNode.ClearThisNode();
    }

    private Vector3 GetCellPosition(int row, int col)
    {
        float xPos = _halfCellWidth + (col * _cellWidth);
        float zPos = _halfCellHeight + (row * _cellHeight);

        return _minPosition + new Vector3(xPos, 0f, zPos) + _gridOffset;
    }

    private void OnDrawGizmos()
    {
        if (_worldCollider == null)
        {
            return;
        }

        if (_drawGrid)
        {
            DrawGridGizmos();
        }
    }

    private void DrawGridGizmos()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.35f, 0.35f);
        float heightSpace = _gridHeight / _rowSize;
        float widthSpace = _gridWidth / _colSize;

        // 가로줄 긋기
        for (int i = 0; i < _rowSize + 1; i++)
        {
            Vector3 originPosition = _minPosition;
            originPosition.z += (i * heightSpace);

            Vector3 lineEndPosition = originPosition;
            lineEndPosition.x += _gridWidth;

            originPosition.y += _gizmoHeightAdditional; lineEndPosition.y += _gizmoHeightAdditional;
            Gizmos.DrawLine(originPosition, lineEndPosition);
        }

        // 세로줄 긋기
        for (int i = 0; i < _colSize + 1; i++)
        {
            Vector3 originPosition = _minPosition;
            originPosition.x += (i * widthSpace);

            Vector3 lineEndPosition = originPosition;
            lineEndPosition.z += _gridHeight;

            originPosition.y += _gizmoHeightAdditional; lineEndPosition.y += _gizmoHeightAdditional;
            Gizmos.DrawLine(originPosition, lineEndPosition);
        }
    }
}