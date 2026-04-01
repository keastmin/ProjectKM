using System.IO;
using UnityEditor;
using UnityEngine;

namespace Player.Editor
{
    public class DitherFadeLitMaterialConverterWindow : EditorWindow
    {
        private const string SourceShaderName = "Universal Render Pipeline/Lit";
        private const string TargetShaderName = "ProjectKM/Dither Fade Lit";

        [SerializeField] private Material _sourceMaterial;

        [MenuItem("Tools/ProjectKM/Dither Fade Lit Converter")]
        private static void OpenWindow()
        {
            DitherFadeLitMaterialConverterWindow window = GetWindow<DitherFadeLitMaterialConverterWindow>("Dither Fade Lit");
            window.minSize = new Vector2(420f, 120f);
            window.SyncSelection();
        }

        [MenuItem("Assets/Create/ProjectKM/Dither Fade Lit Material", true)]
        private static bool ValidateCreateFromSelection()
        {
            Material selectedMaterial = GetSelectedMaterial();
            return selectedMaterial != null && selectedMaterial.shader != null && selectedMaterial.shader.name == SourceShaderName;
        }

        [MenuItem("Assets/Create/ProjectKM/Dither Fade Lit Material")]
        private static void CreateFromSelection()
        {
            Material sourceMaterial = GetSelectedMaterial();
            if (sourceMaterial != null)
            {
                CreateConvertedMaterial(sourceMaterial, true);
            }
        }

        private void OnSelectionChange()
        {
            SyncSelection();
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Creates a Dither Fade Lit copy from a URP Lit material.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            _sourceMaterial = (Material)EditorGUILayout.ObjectField("Source Material", _sourceMaterial, typeof(Material), false);

            if (Shader.Find(TargetShaderName) == null)
            {
                EditorGUILayout.HelpBox($"'{TargetShaderName}' shader was not found.", MessageType.Error);
                return;
            }

            if (_sourceMaterial == null)
            {
                EditorGUILayout.HelpBox("Select a source material.", MessageType.Info);
                return;
            }

            if (_sourceMaterial.shader == null || _sourceMaterial.shader.name != SourceShaderName)
            {
                EditorGUILayout.HelpBox($"The source material must use '{SourceShaderName}'.", MessageType.Warning);
                return;
            }

            EditorGUILayout.HelpBox("Creates a new material while preserving textures, colors, floats, keywords, render queue, and GI/instancing settings.", MessageType.None);

            if (GUILayout.Button("Create Dither Fade Copy"))
            {
                CreateConvertedMaterial(_sourceMaterial, true);
            }
        }

        private void SyncSelection()
        {
            Material selectedMaterial = GetSelectedMaterial();
            if (selectedMaterial != null)
            {
                _sourceMaterial = selectedMaterial;
            }
        }

        private static Material GetSelectedMaterial()
        {
            return Selection.activeObject as Material;
        }

        private static Material CreateConvertedMaterial(Material sourceMaterial, bool pingCreatedAsset)
        {
            Shader targetShader = Shader.Find(TargetShaderName);
            if (targetShader == null)
            {
                Debug.LogError($"Shader not found: {TargetShaderName}");
                return null;
            }

            string sourcePath = AssetDatabase.GetAssetPath(sourceMaterial);
            string directory = string.IsNullOrEmpty(sourcePath) ? "Assets" : Path.GetDirectoryName(sourcePath);
            string fileName = string.IsNullOrEmpty(sourcePath) ? sourceMaterial.name : Path.GetFileNameWithoutExtension(sourcePath);
            string newPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directory ?? "Assets", $"{fileName} Dither Fade.mat"));

            Material newMaterial = new Material(targetShader);
            newMaterial.CopyPropertiesFromMaterial(sourceMaterial);
            newMaterial.name = $"{sourceMaterial.name} Dither Fade";
            newMaterial.renderQueue = sourceMaterial.renderQueue;
            newMaterial.enableInstancing = sourceMaterial.enableInstancing;
            newMaterial.doubleSidedGI = sourceMaterial.doubleSidedGI;
            newMaterial.globalIlluminationFlags = sourceMaterial.globalIlluminationFlags;

            if (newMaterial.HasProperty("_CameraFade"))
            {
                newMaterial.SetFloat("_CameraFade", 0f);
            }

            if (newMaterial.HasProperty("_FadePower"))
            {
                newMaterial.SetFloat("_FadePower", 1f);
            }

            if (newMaterial.HasProperty("_FullFadeThreshold"))
            {
                newMaterial.SetFloat("_FullFadeThreshold", 0.98f);
            }

            AssetDatabase.CreateAsset(newMaterial, newPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(newPath);

            if (pingCreatedAsset)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = newMaterial;
                EditorGUIUtility.PingObject(newMaterial);
            }

            Debug.Log($"Created Dither Fade Lit material: {newPath}", newMaterial);
            return newMaterial;
        }
    }
}
