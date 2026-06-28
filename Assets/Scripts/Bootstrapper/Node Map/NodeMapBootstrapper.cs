using UnityEngine;

public class NodeMapBootstrapper : Bootstrapper
{
    [SerializeField] private NodeMapSequenceDirector _nodeMapSequenceDirector;
    [SerializeField] private NodeMapCameraController _nodeMapCameraController;
    [SerializeField] private NodeMapInteractor _nodeMapInteractor;
    [SerializeField] private NodeMapGenerator _nodeMapGenerator;
    [SerializeField] private NodeMapView _nodeMapView;
    [SerializeField] private SceneSwitchRequester _sceneSwitchRequester;

    public override void InitializeScene(GameRunContext context)
    {
        _nodeMapSequenceDirector.InitializeNodeMapSequenceDirector(context);
        _nodeMapCameraController.InitializeNodeMapViewController(context.CinemachineBrain);
        _nodeMapInteractor.InitializeNodeMapInteractor(context);
        _sceneSwitchRequester.InitializeSceneSwitchRequester(context);

        _nodeMapGenerator.GenerateNodeMap();
        _nodeMapView.CreateNodeView(_nodeMapGenerator.BaseNode);
    }
}