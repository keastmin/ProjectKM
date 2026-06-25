using UnityEngine;

public abstract class Bootstrapper : MonoBehaviour
{
    public abstract void InitializeScene(GameRunContext context);
}