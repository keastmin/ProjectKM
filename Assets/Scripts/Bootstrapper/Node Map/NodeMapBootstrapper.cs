using UnityEngine;

public class NodeMapBootstrapper : Bootstrapper
{
    [SerializeField] private NodeMapSequenceDirector _nodeMapSequenceDirector;
    [SerializeField] private NodeMapCameraController _nodeMapCameraController;
    [SerializeField] private NodeMapInteractor _nodeMapInteractor;

    public override void InitializeScene(GameRunContext context)
    {
        _nodeMapSequenceDirector.InitializeNodeMapSequenceDirector(context);
        _nodeMapCameraController.InitializeNodeMapViewController(context.CinemachineBrain);
        _nodeMapInteractor.InitializeNodeMapInteractor(context);
    }
}