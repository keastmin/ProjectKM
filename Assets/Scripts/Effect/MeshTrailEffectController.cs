using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

public class MeshTrailEffectController : MonoBehaviour
{
    [SerializeField] private float _meshRefreshRate = 0.15f;
    [SerializeField] private SkinnedMeshRenderer[] _skinnedMeshRenderers;
    [SerializeField] private Material _mat;
    [SerializeField] private float _meshDestroyDelay = 0.2f;
    [SerializeField] private string _shaderVarRef;
    [SerializeField] private float _shaderVarRate = 0.1f;
    [SerializeField] private float _shaderVarRefreshRate = 0.05f;

    private Coroutine _dodgeEffectCoroutine;

    public void PerfactDodgeMeshTrailEffectOn(float duration)
    {
        if (_dodgeEffectCoroutine != null)
        {
            StopCoroutine(_dodgeEffectCoroutine);
            _dodgeEffectCoroutine = null;
        }

        _dodgeEffectCoroutine = StartCoroutine(ActiveTrail(duration));
    }

    private IEnumerator ActiveTrail(float duration)
    {
        while(duration > 0)
        {
            duration -= _meshRefreshRate;

            if (_skinnedMeshRenderers == null)
                _skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach(var smr in _skinnedMeshRenderers)
            {
                GameObject gObj = new GameObject();
                gObj.transform.SetPositionAndRotation(transform.position, transform.rotation);

                MeshRenderer mr = gObj.AddComponent<MeshRenderer>();
                MeshFilter mf = gObj.AddComponent<MeshFilter>();

                Mesh mesh = new Mesh();
                smr.BakeMesh(mesh);

                mf.mesh = mesh;
                mr.material = _mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                StartCoroutine(AnimateMaterialFloat(mr.material, 0, _shaderVarRate, _shaderVarRefreshRate));

                Destroy(gObj, _meshDestroyDelay);
            }

            yield return new WaitForSeconds(_meshRefreshRate);
        }
    }

    private IEnumerator AnimateMaterialFloat(Material mat, float goal, float rate, float refreshRate)
    {
        float valueToAnimate = mat.GetFloat(_shaderVarRef);

        while(valueToAnimate > goal)
        {
            valueToAnimate -= rate;
            mat.SetFloat(_shaderVarRef, valueToAnimate);
            yield return new WaitForSeconds(refreshRate);
        }
    }
}
