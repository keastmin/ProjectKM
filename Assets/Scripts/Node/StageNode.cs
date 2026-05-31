using UnityEngine;
using UnityEngine.UI;

public class StageNode : MonoBehaviour
{
    [SerializeField] private string _stageName;
    [SerializeField] private Button _stageButton;

    public void InitNode(string stageName, Vector3 pos)
    {
        _stageName = stageName;
        transform.localPosition = pos;
        transform.localRotation = Quaternion.identity;
        _stageButton.onClick.AddListener(OnClickNode);
    }

    public void OnClickNode()
    {
        LoadingController.LoadScene(_stageName);
    }
}
