using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Player.Editor
{
    public class DitherFadeLitShaderGUI : ShaderGUI
    {
        private const string InnerTypeName = "UnityEditor.Rendering.Universal.ShaderGUI.LitShader, Unity.RenderPipelines.Universal.Editor";

        private ShaderGUI _innerGui;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            EnsureInnerGui();

            if (_innerGui != null)
            {
                _innerGui.OnGUI(materialEditor, properties);
            }
            else
            {
                base.OnGUI(materialEditor, properties);
            }

            DrawDitherFadeSection(materialEditor, properties);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            EnsureInnerGui();

            if (_innerGui != null)
            {
                _innerGui.AssignNewShaderToMaterial(material, oldShader, newShader);
            }
            else
            {
                base.AssignNewShaderToMaterial(material, oldShader, newShader);
            }

            if (material.HasProperty("_CameraFade"))
            {
                material.SetFloat("_CameraFade", 0f);
            }

            if (material.HasProperty("_FadePower"))
            {
                material.SetFloat("_FadePower", 1f);
            }

            if (material.HasProperty("_FullFadeThreshold"))
            {
                material.SetFloat("_FullFadeThreshold", 0.98f);
            }
        }

        private void EnsureInnerGui()
        {
            if (_innerGui != null)
            {
                return;
            }

            Type innerType = Type.GetType(InnerTypeName);
            if (innerType == null)
            {
                return;
            }

            _innerGui = Activator.CreateInstance(innerType) as ShaderGUI;
        }

        private void DrawDitherFadeSection(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty cameraFade = FindProperty("_CameraFade", properties, false);
            MaterialProperty fadePower = FindProperty("_FadePower", properties, false);
            MaterialProperty fullFadeThreshold = FindProperty("_FullFadeThreshold", properties, false);

            if (cameraFade == null && fadePower == null && fullFadeThreshold == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dither Fade", EditorStyles.boldLabel);

            if (cameraFade != null)
            {
                materialEditor.ShaderProperty(cameraFade, cameraFade.displayName);
            }

            if (fadePower != null)
            {
                materialEditor.ShaderProperty(fadePower, fadePower.displayName);
            }

            if (fullFadeThreshold != null)
            {
                materialEditor.ShaderProperty(fullFadeThreshold, fullFadeThreshold.displayName);
            }

            EditorGUILayout.HelpBox("When Camera Fade reaches Full Fade Threshold, the object becomes fully invisible instead of leaving residual dither pixels.", MessageType.None);
        }
    }
}
