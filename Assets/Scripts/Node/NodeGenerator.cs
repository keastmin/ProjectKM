using UnityEngine;

public class NodeGenerator : MonoBehaviour
{
    [SerializeField][Min(1)] private int _gridRow = 15; // 가로줄(행)
    [SerializeField][Min(1)] private int _gridCol = 7; // 세로줄(열)
    [SerializeField] private int _interate = 6; // 생성 반복 횟수
    [SerializeField] private float _xSpace = 1.3f;
    [SerializeField] private float _zSpace = 2f;
    [SerializeField] private Transform _nodeAreaTransform;
    [SerializeField] private StageNode _nodePrefab;
    [SerializeField] private string _startNodeName = "NormalCombatScene";
    [SerializeField] private float _nodeFloatingHeight = 0.7f;

    private StageNode[,] _nodeGrid;

    private int _firstSelectIndex = -1;

    private void Start()
    {
        GenerateGrid();
        ActiveFirstNode();
    }

    private void GenerateGrid()
    {
        if (_nodeGrid == null)
            _nodeGrid = new StageNode[_gridRow, _gridCol];

        for(int i = 0; i < _interate; i++)
        {
            int prevIndex = 0;
            if (i == 0)
                prevIndex = SelectFirstStartIndex();
            else if (i == 1)
                prevIndex = SelectSecondStartIndex(_firstSelectIndex);
            else
                prevIndex = SelectStartIndex();

            StageNode prevNode = GenerateNode(0, prevIndex);
            _nodeGrid[0, prevIndex] = prevNode;

            for(int j = 1; j < _gridRow; j++)
            {
                int currIndex = SelectIndex(prevIndex);
                StageNode currNode = GenerateNode(j, currIndex);
                _nodeGrid[j, currIndex] = currNode;
                prevNode.AddNextNode(currNode);
                prevNode = currNode;
            }
        }
    }

    private int SelectFirstStartIndex()
    {
        _firstSelectIndex = Random.Range(0, _gridCol);
        return _firstSelectIndex;
    }

    private int SelectSecondStartIndex(int firstStartIndex)
    {
        int index = Random.Range(0, _gridCol);
        while (index == firstStartIndex)
        {
            index = Random.Range(0, _gridCol);
        }
        return index;
    }

    private int SelectStartIndex()
    {
        return Random.Range(0, _gridCol);
    }

    private int SelectIndex(int prevIndex)
    {
        int minRandValue = Mathf.Max(0, prevIndex - 1);
        int maxRandValue = Mathf.Min(prevIndex + 2, _gridCol);
        int index = Random.Range(minRandValue, maxRandValue);
        return index;
    }

    private StageNode GenerateNode(int row, int col)
    {
        if (_nodeGrid[row, col] != null)
            return _nodeGrid[row, col];

        Vector3 pos = GetLocalPosition(row, col);
        StageNode node = Instantiate(_nodePrefab, _nodeAreaTransform);
        node.InitNode(_startNodeName, pos);
        return node;
    }

    private Vector3 GetLocalPosition(int row, int col)
    {
        int middleColIndex = (int)(_gridCol / 2f);
        float xPos = col * _xSpace;
        xPos -= (_gridCol % 2 == 0) ? (((middleColIndex - 1) * _xSpace) + (_xSpace / 2f)) : (middleColIndex * _xSpace);
        float zPos = row * _zSpace;
        return new Vector3(xPos, _nodeFloatingHeight, zPos);
    }

    private void ActiveFirstNode()
    {
        for(int i = 0; i < _gridCol; i++)
        {
            if (_nodeGrid[0, i] != null)
                _nodeGrid[0, i].ChangeNodeState(NodeState.Active);
        }
    }
}
