#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DuckovCustomModel.Editor
{
    /// <summary>
    /// Editor tool for analyzing and creating ShaderVariantCollections from prefabs and materials.
    /// This tool is useful for standard Unity shaders and custom shaders that benefit from variant precompilation.
    /// 
    /// NOTE: This tool is NOT recommended for lilToon shaders, as lilToon has its own automatic
    /// build-time optimization mechanism. For lilToon, bundle shaders with your model instead of
    /// creating separate shader bundles.
    /// </summary>
    public class ShaderVariantTool : EditorWindow
    {
        private static readonly PassType[] CommonPassTypes = 
        {
            PassType.Normal,
            PassType.ForwardBase,
            PassType.ForwardAdd,
            PassType.ShadowCaster,
            PassType.Deferred
        };

        private GameObject[] selectedPrefabs = new GameObject[0];
        private Material[] selectedMaterials = new Material[0];
        private ShaderVariantCollection targetCollection;
        private Vector2 scrollPosition;
        private bool includeChildMaterials = true;
        private bool analyzeAllKeywords = true;

        [MenuItem("Tools/Duckov Custom Model/Shader Variant Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<ShaderVariantTool>("Shader Variant Tool");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Shader Variant Collection Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool analyzes selected prefabs and materials to create or update a ShaderVariantCollection.\n\n" +
                "⚠️ NOT recommended for lilToon shaders - lilToon handles optimization automatically during build.\n" +
                "For lilToon, bundle shaders with your models instead of creating separate shader bundles.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Target Collection
            EditorGUILayout.LabelField("Target Collection", EditorStyles.boldLabel);
            targetCollection = (ShaderVariantCollection)EditorGUILayout.ObjectField(
                "Shader Variant Collection",
                targetCollection,
                typeof(ShaderVariantCollection),
                false);

            if (targetCollection == null)
            {
                EditorGUILayout.HelpBox(
                    "Create a new ShaderVariantCollection: Right-click in Project > Create > Shader Variant Collection",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            // Options
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            includeChildMaterials = EditorGUILayout.Toggle("Include Child Materials", includeChildMaterials);
            analyzeAllKeywords = EditorGUILayout.Toggle("Analyze All Keywords", analyzeAllKeywords);

            EditorGUILayout.Space(10);

            // Selection Info
            EditorGUILayout.LabelField("Selected Assets", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Refresh Selection"))
            {
                RefreshSelection();
            }

            EditorGUILayout.LabelField($"Prefabs: {selectedPrefabs.Length}");
            EditorGUILayout.LabelField($"Materials: {selectedMaterials.Length}");

            EditorGUILayout.Space(10);

            // Actions
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            GUI.enabled = targetCollection != null && (selectedPrefabs.Length > 0 || selectedMaterials.Length > 0);
            if (GUILayout.Button("Analyze and Add Variants", GUILayout.Height(30)))
            {
                AnalyzeAndAddVariants();
            }
            GUI.enabled = true;

            if (GUILayout.Button("Clear Selection"))
            {
                selectedPrefabs = new GameObject[0];
                selectedMaterials = new Material[0];
            }

            EditorGUILayout.Space(10);

            // Info
            EditorGUILayout.LabelField("Usage Instructions", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Create or select a ShaderVariantCollection asset\n" +
                "2. Select prefabs and/or materials in the Project window\n" +
                "3. Click 'Refresh Selection' to load selected assets\n" +
                "4. Click 'Analyze and Add Variants' to add shader variants to the collection\n\n" +
                "The tool will analyze all materials in selected prefabs and add their shader variants.",
                MessageType.Info);

            EditorGUILayout.EndScrollView();
        }

        private void RefreshSelection()
        {
            var selection = Selection.objects;
            var prefabs = new List<GameObject>();
            var materials = new List<Material>();

            foreach (var obj in selection)
            {
                if (obj is GameObject go)
                {
                    prefabs.Add(go);
                }
                else if (obj is Material mat)
                {
                    materials.Add(mat);
                }
            }

            selectedPrefabs = prefabs.ToArray();
            selectedMaterials = materials.ToArray();
        }

        private void AnalyzeAndAddVariants()
        {
            if (targetCollection == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a ShaderVariantCollection first.", "OK");
                return;
            }

            var allMaterials = new HashSet<Material>();

            // Collect materials from selected materials
            foreach (var mat in selectedMaterials)
            {
                if (mat != null)
                {
                    allMaterials.Add(mat);
                }
            }

            // Collect materials from selected prefabs
            foreach (var prefab in selectedPrefabs)
            {
                if (prefab == null) continue;

                var renderers = includeChildMaterials
                    ? prefab.GetComponentsInChildren<Renderer>(true)
                    : prefab.GetComponents<Renderer>();

                foreach (var renderer in renderers)
                {
                    foreach (var mat in renderer.sharedMaterials)
                    {
                        if (mat != null)
                        {
                            allMaterials.Add(mat);
                        }
                    }
                }
            }

            if (allMaterials.Count == 0)
            {
                EditorUtility.DisplayDialog("No Materials", "No materials found in selected assets.", "OK");
                return;
            }

            int addedVariants = 0;
            var processedShaders = new HashSet<Shader>();

            foreach (var material in allMaterials)
            {
                if (material.shader == null) continue;

                var shader = material.shader;
                if (processedShaders.Contains(shader) && !analyzeAllKeywords)
                {
                    continue;
                }

                processedShaders.Add(shader);

                // Get enabled keywords for this material
                var keywords = material.shaderKeywords;

                // Try to add the variant with Normal pass type as a sensible default
                // Users can enable "Analyze All Keywords" to try additional pass types
                var passType = PassType.Normal;

                // Try to add the variant
                var variant = new ShaderVariantCollection.ShaderVariant(shader, passType, keywords);
                
                try
                {
                    if (!targetCollection.Contains(variant))
                    {
                        targetCollection.Add(variant);
                        addedVariants++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not add variant for shader {shader.name} with PassType.Normal: {e.Message}");
                }

                // If analyzing all keywords, try common pass types
                if (analyzeAllKeywords)
                {
                    foreach (var pass in CommonPassTypes)
                    {
                        try
                        {
                            var passVariant = new ShaderVariantCollection.ShaderVariant(shader, pass, keywords);
                            if (!targetCollection.Contains(passVariant))
                            {
                                targetCollection.Add(passVariant);
                                addedVariants++;
                            }
                        }
                        catch (System.ArgumentException)
                        {
                            // Expected: Some pass types may not be valid for this shader
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"Could not add variant for shader {shader.name} with {pass}: {e.Message}");
                        }
                    }
                }
            }

            EditorUtility.SetDirty(targetCollection);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Variants Added",
                $"Added {addedVariants} shader variants from {allMaterials.Count} materials.\n" +
                $"Total variants in collection: {targetCollection.variantCount}",
                "OK");

            Debug.Log($"[ShaderVariantTool] Added {addedVariants} variants. " +
                     $"Collection now contains {targetCollection.variantCount} total variants.");
        }

        private void OnSelectionChange()
        {
            Repaint();
        }
    }
}
#endif
