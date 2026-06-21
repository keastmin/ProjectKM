using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class NodeMapGenerator : MonoBehaviour
{
    private enum PreviewNodeType
    {
        Basecamp,
        Combat,
        EliteCombat,
        RestAndUpgrade,
        Event,
        AllyEncounter,
        Boss
    }

    private sealed class PreviewNode
    {
        public Vector2Int Cell;
        public Vector3 Position;
        public PreviewNodeType Type;
        public int Layer;
        public bool IsBossApproach;
    }

    private readonly struct PreviewEdge
    {
        public readonly int StartIndex;
        public readonly int EndIndex;

        public PreviewEdge(int startIndex, int endIndex)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }

    private readonly struct EdgeCandidate
    {
        public readonly int StartIndex;
        public readonly int EndIndex;
        public readonly float SqrDistance;

        public EdgeCandidate(int startIndex, int endIndex, float sqrDistance)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
            SqrDistance = sqrDistance;
        }
    }

    [Header("Grid")]
    [SerializeField] private Collider _worldCollider;
    [SerializeField][Min(1)] private int _rowSize = 50; // 가로줄 행
    [SerializeField][Min(1)] private int _colSize = 50; // 세로줄 열
    [SerializeField] private float _gizmoHeightAdditional = 0.1f;

    [Header("Node")]
    [SerializeField] private Node _nodePrefab;
    [SerializeField] private Material _normalCombatMaterial;
    [SerializeField] private Material _eliteCombatMaterial;
    [SerializeField] private Material _eventMaterial;
    [SerializeField] private Material _upgradeMaterial;
    [SerializeField] private Material _helperEncounterMaterial;
    [SerializeField] private Material _bossMaterial;

    [Header("Procedural Preview")]
    [SerializeField] private bool _drawGrid = true;
    [SerializeField] private bool _drawGeneratedMap = true;
    [SerializeField] private bool _drawNodeLabels;
    [SerializeField] private int _previewSeed = 1207;
    [SerializeField][Min(8)] private int _previewNodeCount = 36;
    [SerializeField][Range(4, 12)] private int _angularSectorCount = 8;
    [SerializeField][Range(0f, 0.9f)] private float _cellJitterRatio = 0.55f;
    [SerializeField][Range(0.05f, 0.45f)] private float _nodeGizmoScale = 0.22f;
    [SerializeField][Range(0f, 1f)] private float _extraEdgeRatio = 0.3f;
    [SerializeField][Range(2, 8)] private int _basecampConnectionCount = 5;

    [Header("Preview Colors")]
    [SerializeField] private Color _basecampColor = new(0.15f, 0.95f, 1f, 1f);
    [SerializeField] private Color _combatColor = new(0.95f, 0.25f, 0.2f, 1f);
    [SerializeField] private Color _eliteCombatColor = new(0.65f, 0.25f, 1f, 1f);
    [SerializeField] private Color _restAndUpgradeColor = new(0.2f, 0.9f, 0.35f, 1f);
    [SerializeField] private Color _eventColor = new(1f, 0.75f, 0.15f, 1f);
    [SerializeField] private Color _allyEncounterColor = new(0.2f, 0.55f, 1f, 1f);
    [SerializeField] private Color _bossColor = new(1f, 0.05f, 0.45f, 1f);
    [SerializeField] private Color _edgeColor = new(0.8f, 0.85f, 0.9f, 0.8f);

    private Vector3 _minPosition;
    private Vector3 _maxPosition;
    private float _width;
    private float _height;
    private Bounds _cachedBounds;
    private readonly List<PreviewNode> _previewNodes = new();
    private readonly List<PreviewEdge> _previewEdges = new();

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
        if (_worldCollider == null)
        {
            _previewNodes.Clear();
            _previewEdges.Clear();
            return;
        }

        CacheWidthSize();
        CacheHeightSize();
        CacheMinPosition();
        CacheMaxPosition();
        _cachedBounds = _worldCollider.bounds;
        GeneratePreviewMap();
    }

    private void CacheWidthSize()
    {
        _width = _worldCollider.bounds.size.x;
    }

    private void CacheHeightSize()
    {
        _height = _worldCollider.bounds.size.z;
    }

    private void CacheMinPosition()
    {
        _minPosition = _worldCollider.bounds.min;
    }

    private void CacheMaxPosition()
    {
        _maxPosition = _worldCollider.bounds.max;
    }

    private void OnDrawGizmos()
    {
        if (_worldCollider == null)
        {
            return;
        }

        if (_previewNodes.Count == 0 || _cachedBounds != _worldCollider.bounds)
        {
            InitializeNodeMap();
        }

        if (_drawGrid)
        {
            DrawGridGizmos();
        }

        if (_drawGeneratedMap)
        {
            DrawPreviewMapGizmos();
        }
    }

    private void DrawGridGizmos()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.35f, 0.35f);
        float heightSpace = _height / _rowSize;
        float widthSpace = _width / _colSize;

        // 가로줄 긋기
        for(int i = 0; i < _rowSize + 1; i++)
        {
            Vector3 originPosition = _minPosition;
            originPosition.z += (i * heightSpace);

            Vector3 lineEndPosition = originPosition;
            lineEndPosition.x += _width;

            originPosition.y += _gizmoHeightAdditional; lineEndPosition.y += _gizmoHeightAdditional;
            Gizmos.DrawLine(originPosition, lineEndPosition);
        }

        // 세로줄 긋기
        for (int i = 0; i < _colSize + 1; i++)
        {
            Vector3 originPosition = _minPosition;
            originPosition.x += (i * widthSpace);

            Vector3 lineEndPosition = originPosition;
            lineEndPosition.z += _height;

            originPosition.y += _gizmoHeightAdditional; lineEndPosition.y += _gizmoHeightAdditional;
            Gizmos.DrawLine(originPosition, lineEndPosition);
        }
    }

    private void GeneratePreviewMap()
    {
        _previewNodes.Clear();
        _previewEdges.Clear();

        int rowCount = Mathf.Max(1, _rowSize);
        int colCount = Mathf.Max(1, _colSize);
        int totalCellCount = rowCount * colCount;
        int targetNodeCount = Mathf.Clamp(_previewNodeCount, Mathf.Min(3, totalCellCount), totalCellCount);
        int layerCount = Mathf.Max(2, Mathf.CeilToInt(Mathf.Min(rowCount, colCount) * 0.5f));
        int sectorCount = Mathf.Clamp(_angularSectorCount, 4, 12);
        var random = new System.Random(_previewSeed);

        Vector2Int centerCell = new(colCount / 2, rowCount / 2);
        Vector2Int bossCell = SelectBossCell(random, centerCell, rowCount, colCount);
        Vector2Int bossApproachCell = SelectBossApproachCell(bossCell, centerCell, rowCount, colCount);
        var selectedCells = new List<Vector2Int>(targetNodeCount) { centerCell };
        if (bossApproachCell != centerCell && bossApproachCell != bossCell)
        {
            selectedCells.Add(bossApproachCell);
        }

        if (bossCell != centerCell)
        {
            selectedCells.Add(bossCell);
        }

        var candidates = new List<Vector2Int>(Mathf.Max(0, totalCellCount - selectedCells.Count));
        for (int row = 0; row < rowCount; row++)
        {
            for (int col = 0; col < colCount; col++)
            {
                Vector2Int cell = new(col, row);
                if (!selectedCells.Contains(cell))
                {
                    candidates.Add(cell);
                }
            }
        }

        int[] sectorPopulation = new int[sectorCount];
        int[] layerPopulation = new int[layerCount];
        RegisterCellDistribution(bossCell, centerCell, rowCount, colCount, sectorPopulation, layerPopulation);

        while (selectedCells.Count < targetNodeCount && candidates.Count > 0)
        {
            int bestIndex = 0;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < candidates.Count; i++)
            {
                Vector2Int cell = candidates[i];
                int sector = GetSector(cell, centerCell, sectorCount);
                int layer = GetLayer(cell, centerCell, rowCount, colCount, layerCount);
                float nearestSqrDistance = GetNearestCellSqrDistance(cell, selectedCells);
                float score = nearestSqrDistance * 1.75f;
                score -= sectorPopulation[sector] * 5f;
                score -= layerPopulation[layer] * 2f;
                score += (float)random.NextDouble() * 2f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            Vector2Int selectedCell = candidates[bestIndex];
            candidates.RemoveAt(bestIndex);
            selectedCells.Add(selectedCell);
            RegisterCellDistribution(selectedCell, centerCell, rowCount, colCount, sectorPopulation, layerPopulation);
        }

        float cellWidth = _width / colCount;
        float cellHeight = _height / rowCount;
        for (int i = 0; i < selectedCells.Count; i++)
        {
            Vector2Int cell = selectedCells[i];
            int layer = GetLayer(cell, centerCell, rowCount, colCount, layerCount);
            PreviewNodeType type = GetPreviewNodeType(random, layer, layerCount);

            if (cell == centerCell)
            {
                type = PreviewNodeType.Basecamp;
            }
            else if (cell == bossCell)
            {
                type = PreviewNodeType.Boss;
            }

            _previewNodes.Add(new PreviewNode
            {
                Cell = cell,
                Position = GetNodePosition(random, cell, centerCell, cellWidth, cellHeight),
                Type = type,
                Layer = layer,
                IsBossApproach = cell == bossApproachCell
            });
        }

        GeneratePreviewEdges(cellWidth, cellHeight);
    }

    private Vector2Int SelectBossCell(System.Random random, Vector2Int centerCell, int rowCount, int colCount)
    {
        var boundaryCells = new List<Vector2Int>();
        for (int row = 0; row < rowCount; row++)
        {
            for (int col = 0; col < colCount; col++)
            {
                if (row == 0 || row == rowCount - 1 || col == 0 || col == colCount - 1)
                {
                    Vector2Int cell = new(col, row);
                    if (cell != centerCell)
                    {
                        boundaryCells.Add(cell);
                    }
                }
            }
        }

        return boundaryCells.Count > 0 ? boundaryCells[random.Next(boundaryCells.Count)] : centerCell;
    }

    private Vector2Int SelectBossApproachCell(
        Vector2Int bossCell,
        Vector2Int centerCell,
        int rowCount,
        int colCount)
    {
        Vector2Int bestCell = bossCell;
        float bestSqrDistance = float.PositiveInfinity;

        for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
        {
            for (int colOffset = -1; colOffset <= 1; colOffset++)
            {
                if (rowOffset == 0 && colOffset == 0)
                {
                    continue;
                }

                Vector2Int cell = bossCell + new Vector2Int(colOffset, rowOffset);
                if (cell.x < 0 || cell.x >= colCount || cell.y < 0 || cell.y >= rowCount || cell == centerCell)
                {
                    continue;
                }

                float sqrDistance = (cell - centerCell).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    bestCell = cell;
                }
            }
        }

        return bestCell;
    }

    private void RegisterCellDistribution(
        Vector2Int cell,
        Vector2Int centerCell,
        int rowCount,
        int colCount,
        int[] sectorPopulation,
        int[] layerPopulation)
    {
        if (cell == centerCell)
        {
            return;
        }

        int sector = GetSector(cell, centerCell, sectorPopulation.Length);
        int layer = GetLayer(cell, centerCell, rowCount, colCount, layerPopulation.Length);
        sectorPopulation[sector]++;
        layerPopulation[layer]++;
    }

    private int GetSector(Vector2Int cell, Vector2Int centerCell, int sectorCount)
    {
        float angle = Mathf.Atan2(cell.y - centerCell.y, cell.x - centerCell.x);
        float normalizedAngle = Mathf.Repeat(angle + Mathf.PI, Mathf.PI * 2f) / (Mathf.PI * 2f);
        return Mathf.Min(sectorCount - 1, Mathf.FloorToInt(normalizedAngle * sectorCount));
    }

    private int GetLayer(
        Vector2Int cell,
        Vector2Int centerCell,
        int rowCount,
        int colCount,
        int layerCount)
    {
        float halfWidth = Mathf.Max(centerCell.x, colCount - 1 - centerCell.x);
        float halfHeight = Mathf.Max(centerCell.y, rowCount - 1 - centerCell.y);
        float normalizedX = Mathf.Abs(cell.x - centerCell.x) / Mathf.Max(1f, halfWidth);
        float normalizedY = Mathf.Abs(cell.y - centerCell.y) / Mathf.Max(1f, halfHeight);
        float normalizedDistance = Mathf.Max(normalizedX, normalizedY);
        return Mathf.Clamp(Mathf.CeilToInt(normalizedDistance * layerCount) - 1, 0, layerCount - 1);
    }

    private float GetNearestCellSqrDistance(Vector2Int cell, List<Vector2Int> selectedCells)
    {
        float nearestSqrDistance = float.PositiveInfinity;
        for (int i = 0; i < selectedCells.Count; i++)
        {
            nearestSqrDistance = Mathf.Min(nearestSqrDistance, (cell - selectedCells[i]).sqrMagnitude);
        }

        return nearestSqrDistance;
    }

    private Vector3 GetNodePosition(
        System.Random random,
        Vector2Int cell,
        Vector2Int centerCell,
        float cellWidth,
        float cellHeight)
    {
        float jitterX = ((float)random.NextDouble() * 2f - 1f) * cellWidth * 0.5f * _cellJitterRatio;
        float jitterZ = ((float)random.NextDouble() * 2f - 1f) * cellHeight * 0.5f * _cellJitterRatio;

        if (cell == centerCell)
        {
            jitterX = 0f;
            jitterZ = 0f;
        }

        return new Vector3(
            _minPosition.x + (cell.x + 0.5f) * cellWidth + jitterX,
            _minPosition.y + _gizmoHeightAdditional,
            _minPosition.z + (cell.y + 0.5f) * cellHeight + jitterZ);
    }

    private PreviewNodeType GetPreviewNodeType(System.Random random, int layer, int layerCount)
    {
        float progress = layerCount <= 1 ? 0f : layer / (float)(layerCount - 1);
        float roll = (float)random.NextDouble();

        if (progress > 0.45f && roll < Mathf.Lerp(0.04f, 0.18f, progress))
        {
            return PreviewNodeType.EliteCombat;
        }

        if (roll < 0.58f)
        {
            return PreviewNodeType.Combat;
        }

        if (roll < 0.72f)
        {
            return PreviewNodeType.RestAndUpgrade;
        }

        if (roll < 0.87f)
        {
            return PreviewNodeType.Event;
        }

        return PreviewNodeType.AllyEncounter;
    }

    private void GeneratePreviewEdges(float cellWidth, float cellHeight)
    {
        var candidates = new List<EdgeCandidate>();
        for (int startIndex = 0; startIndex < _previewNodes.Count - 1; startIndex++)
        {
            for (int endIndex = startIndex + 1; endIndex < _previewNodes.Count; endIndex++)
            {
                if (IsReservedBossBranchNode(startIndex) || IsReservedBossBranchNode(endIndex))
                {
                    continue;
                }

                Vector3 offset = _previewNodes[endIndex].Position - _previewNodes[startIndex].Position;
                candidates.Add(new EdgeCandidate(startIndex, endIndex, offset.x * offset.x + offset.z * offset.z));
            }
        }

        candidates.Sort((left, right) => left.SqrDistance.CompareTo(right.SqrDistance));
        int[] parents = new int[_previewNodes.Count];
        int[] degree = new int[_previewNodes.Count];
        int regularNodeCount = 0;
        for (int i = 0; i < parents.Length; i++)
        {
            parents[i] = i;
            if (!IsReservedBossBranchNode(i))
            {
                regularNodeCount++;
            }
        }

        int bossIndex = _previewNodes.FindIndex(node => node.Type == PreviewNodeType.Boss);
        int bossApproachIndex = _previewNodes.FindIndex(node => node.IsBossApproach);
        int bossBranchAnchorIndex = FindBossBranchAnchorIndex(bossApproachIndex);

        for (int i = 0; i < candidates.Count && _previewEdges.Count < regularNodeCount - 1; i++)
        {
            EdgeCandidate candidate = candidates[i];
            if (FindRoot(parents, candidate.StartIndex) == FindRoot(parents, candidate.EndIndex) ||
                IntersectsReservedBossBranch(candidate.StartIndex, candidate.EndIndex, bossIndex, bossApproachIndex, bossBranchAnchorIndex) ||
                IntersectsExistingEdge(candidate.StartIndex, candidate.EndIndex))
            {
                continue;
            }

            AddEdge(candidate.StartIndex, candidate.EndIndex, degree);
            Union(parents, candidate.StartIndex, candidate.EndIndex);
        }

        if (bossBranchAnchorIndex >= 0 && bossApproachIndex >= 0)
        {
            AddEdge(bossBranchAnchorIndex, bossApproachIndex, degree);
        }

        if (bossApproachIndex >= 0 && bossIndex >= 0)
        {
            AddEdge(bossApproachIndex, bossIndex, degree);
        }

        AddBasecampConnections(candidates, degree);

        float maxExtraEdgeLength = Mathf.Sqrt(cellWidth * cellWidth + cellHeight * cellHeight) * 2.2f;
        float maxExtraEdgeSqrLength = maxExtraEdgeLength * maxExtraEdgeLength;
        int targetExtraEdgeCount = Mathf.RoundToInt(_previewNodes.Count * _extraEdgeRatio);
        int extraEdgeCount = 0;

        for (int i = 0; i < candidates.Count && extraEdgeCount < targetExtraEdgeCount; i++)
        {
            EdgeCandidate candidate = candidates[i];
            if (candidate.SqrDistance > maxExtraEdgeSqrLength ||
                degree[candidate.StartIndex] >= 4 ||
                degree[candidate.EndIndex] >= 4 ||
                Mathf.Abs(_previewNodes[candidate.StartIndex].Layer - _previewNodes[candidate.EndIndex].Layer) > 1 ||
                ContainsEdge(candidate.StartIndex, candidate.EndIndex) ||
                IntersectsReservedBossBranch(candidate.StartIndex, candidate.EndIndex, bossIndex, bossApproachIndex, bossBranchAnchorIndex) ||
                IntersectsExistingEdge(candidate.StartIndex, candidate.EndIndex))
            {
                continue;
            }

            AddEdge(candidate.StartIndex, candidate.EndIndex, degree);
            extraEdgeCount++;
        }

    }

    private bool IsReservedBossBranchNode(int nodeIndex)
    {
        PreviewNode node = _previewNodes[nodeIndex];
        return node.Type == PreviewNodeType.Boss || node.IsBossApproach;
    }

    private int FindBossBranchAnchorIndex(int bossApproachIndex)
    {
        if (bossApproachIndex < 0)
        {
            return -1;
        }

        int bestIndex = -1;
        float bestSqrDistance = float.PositiveInfinity;
        PreviewNode bossApproachNode = _previewNodes[bossApproachIndex];

        for (int i = 0; i < _previewNodes.Count; i++)
        {
            PreviewNode node = _previewNodes[i];
            if (i == bossApproachIndex || IsReservedBossBranchNode(i) || node.Layer > bossApproachNode.Layer)
            {
                continue;
            }

            Vector3 offset = node.Position - bossApproachNode.Position;
            float sqrDistance = offset.x * offset.x + offset.z * offset.z;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private bool IntersectsReservedBossBranch(
        int startIndex,
        int endIndex,
        int bossIndex,
        int bossApproachIndex,
        int bossBranchAnchorIndex)
    {
        if (bossIndex < 0 || bossApproachIndex < 0 || bossBranchAnchorIndex < 0)
        {
            return false;
        }

        if (IntersectsEdgePair(startIndex, endIndex, bossApproachIndex, bossIndex))
        {
            return true;
        }

        return IntersectsEdgePair(startIndex, endIndex, bossBranchAnchorIndex, bossApproachIndex);
    }

    private bool IntersectsEdgePair(int firstStartIndex, int firstEndIndex, int secondStartIndex, int secondEndIndex)
    {
        if (firstStartIndex == secondStartIndex || firstStartIndex == secondEndIndex ||
            firstEndIndex == secondStartIndex || firstEndIndex == secondEndIndex)
        {
            return false;
        }

        return SegmentsIntersect(
            ToXZ(_previewNodes[firstStartIndex].Position),
            ToXZ(_previewNodes[firstEndIndex].Position),
            ToXZ(_previewNodes[secondStartIndex].Position),
            ToXZ(_previewNodes[secondEndIndex].Position));
    }

    private void AddBasecampConnections(List<EdgeCandidate> candidates, int[] degree)
    {
        int basecampIndex = _previewNodes.FindIndex(node => node.Type == PreviewNodeType.Basecamp);
        if (basecampIndex < 0)
        {
            return;
        }

        for (int i = 0; i < candidates.Count && degree[basecampIndex] < _basecampConnectionCount; i++)
        {
            EdgeCandidate candidate = candidates[i];
            if (candidate.StartIndex != basecampIndex && candidate.EndIndex != basecampIndex)
            {
                continue;
            }

            int otherIndex = candidate.StartIndex == basecampIndex ? candidate.EndIndex : candidate.StartIndex;
            if (_previewNodes[otherIndex].Type == PreviewNodeType.Boss ||
                _previewNodes[otherIndex].Layer > 1 ||
                degree[otherIndex] >= 4 ||
                ContainsEdge(basecampIndex, otherIndex) ||
                IntersectsExistingEdge(basecampIndex, otherIndex))
            {
                continue;
            }

            AddEdge(basecampIndex, otherIndex, degree);
        }
    }

    private int FindRoot(int[] parents, int index)
    {
        while (parents[index] != index)
        {
            parents[index] = parents[parents[index]];
            index = parents[index];
        }

        return index;
    }

    private void Union(int[] parents, int firstIndex, int secondIndex)
    {
        int firstRoot = FindRoot(parents, firstIndex);
        int secondRoot = FindRoot(parents, secondIndex);
        parents[secondRoot] = firstRoot;
    }

    private void AddEdge(int startIndex, int endIndex, int[] degree)
    {
        _previewEdges.Add(new PreviewEdge(startIndex, endIndex));
        degree[startIndex]++;
        degree[endIndex]++;
    }

    private bool ContainsEdge(int startIndex, int endIndex)
    {
        for (int i = 0; i < _previewEdges.Count; i++)
        {
            PreviewEdge edge = _previewEdges[i];
            if ((edge.StartIndex == startIndex && edge.EndIndex == endIndex) ||
                (edge.StartIndex == endIndex && edge.EndIndex == startIndex))
            {
                return true;
            }
        }

        return false;
    }

    private bool IntersectsExistingEdge(int startIndex, int endIndex)
    {
        Vector2 start = ToXZ(_previewNodes[startIndex].Position);
        Vector2 end = ToXZ(_previewNodes[endIndex].Position);

        for (int i = 0; i < _previewEdges.Count; i++)
        {
            PreviewEdge edge = _previewEdges[i];
            if (edge.StartIndex == startIndex || edge.StartIndex == endIndex ||
                edge.EndIndex == startIndex || edge.EndIndex == endIndex)
            {
                continue;
            }

            Vector2 otherStart = ToXZ(_previewNodes[edge.StartIndex].Position);
            Vector2 otherEnd = ToXZ(_previewNodes[edge.EndIndex].Position);
            if (SegmentsIntersect(start, end, otherStart, otherEnd))
            {
                return true;
            }
        }

        return false;
    }

    private bool SegmentsIntersect(Vector2 firstStart, Vector2 firstEnd, Vector2 secondStart, Vector2 secondEnd)
    {
        float firstSideA = Cross(firstEnd - firstStart, secondStart - firstStart);
        float firstSideB = Cross(firstEnd - firstStart, secondEnd - firstStart);
        float secondSideA = Cross(secondEnd - secondStart, firstStart - secondStart);
        float secondSideB = Cross(secondEnd - secondStart, firstEnd - secondStart);
        const float epsilon = 0.0001f;

        return firstSideA * firstSideB < -epsilon && secondSideA * secondSideB < -epsilon;
    }

    private float Cross(Vector2 first, Vector2 second)
    {
        return first.x * second.y - first.y * second.x;
    }

    private Vector2 ToXZ(Vector3 position)
    {
        return new Vector2(position.x, position.z);
    }

    private void DrawPreviewMapGizmos()
    {
        Gizmos.color = _edgeColor;
        for (int i = 0; i < _previewEdges.Count; i++)
        {
            PreviewEdge edge = _previewEdges[i];
            Gizmos.DrawLine(_previewNodes[edge.StartIndex].Position, _previewNodes[edge.EndIndex].Position);
        }

        float cellWidth = _width / Mathf.Max(1, _colSize);
        float cellHeight = _height / Mathf.Max(1, _rowSize);
        float nodeRadius = Mathf.Min(cellWidth, cellHeight) * _nodeGizmoScale;

        for (int i = 0; i < _previewNodes.Count; i++)
        {
            PreviewNode node = _previewNodes[i];
            Gizmos.color = GetNodeColor(node.Type);

            if (node.Type == PreviewNodeType.Basecamp)
            {
                Gizmos.DrawCube(node.Position, Vector3.one * nodeRadius * 2.2f);
            }
            else
            {
                float radius = node.Type == PreviewNodeType.Boss ? nodeRadius * 1.5f : nodeRadius;
                Gizmos.DrawSphere(node.Position, radius);
                if (node.Type == PreviewNodeType.Boss)
                {
                    Gizmos.DrawWireCube(node.Position, Vector3.one * radius * 2.4f);
                }
            }

#if UNITY_EDITOR
            if (_drawNodeLabels)
            {
                Handles.Label(node.Position + Vector3.up * nodeRadius, GetNodeLabel(node.Type));
            }
#endif
        }
    }

    private Color GetNodeColor(PreviewNodeType type)
    {
        return type switch
        {
            PreviewNodeType.Basecamp => _basecampColor,
            PreviewNodeType.Combat => _combatColor,
            PreviewNodeType.EliteCombat => _eliteCombatColor,
            PreviewNodeType.RestAndUpgrade => _restAndUpgradeColor,
            PreviewNodeType.Event => _eventColor,
            PreviewNodeType.AllyEncounter => _allyEncounterColor,
            PreviewNodeType.Boss => _bossColor,
            _ => Color.white
        };
    }

    private string GetNodeLabel(PreviewNodeType type)
    {
        return type switch
        {
            PreviewNodeType.Basecamp => "Basecamp",
            PreviewNodeType.Combat => "Combat",
            PreviewNodeType.EliteCombat => "Elite",
            PreviewNodeType.RestAndUpgrade => "Rest / Upgrade",
            PreviewNodeType.Event => "Event",
            PreviewNodeType.AllyEncounter => "Ally",
            PreviewNodeType.Boss => "Boss",
            _ => string.Empty
        };
    }
}
