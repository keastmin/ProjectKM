using UnityEngine;

public class NodeGenerator : MonoBehaviour
{
    [SerializeField] private Transform _nodeAreaTransform;
    [SerializeField] private StageNode _nodePrefab;
    [SerializeField] private string _startNodeName = "PlayerScene";
    [SerializeField] private float _nodeFloatingHeight = 0.7f;

    private void Start()
    {
        GenerateNodes();
    }

    private void GenerateNodes()
    {
        Vector3 pos = new Vector3(0f, _nodeFloatingHeight, 0f);
        StageNode node = Instantiate(_nodePrefab, _nodeAreaTransform);
        node.InitNode(_startNodeName, pos);
    }
}
