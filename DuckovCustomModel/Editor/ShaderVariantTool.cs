#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace DuckovCustomModel.Editor
{
    /// <summary>
    /// Shader Variant Collection tool for advanced users who need to optimize shader loading.
    /// 
    /// IMPORTANT: This tool is NOT recommended for lilToon shaders!
    /// lilToon has its own automatic optimization mechanism that works best when 
    /// shaders are packaged together with materials in the same AssetBundle.
    /// 
    /// This tool is intended for:
    /// - Standard Unity shaders
    /// - Other custom shaders that don't have build-time optimization
    /// - Advanced users who want to create shader variant collections for separate shader bundles
    /// </summary>
    public class ShaderVariantTool : EditorWindow
    {
        private GameObject targetPrefab;
        private string outputPath = "Assets/ShaderVariants";
        private string collectionName = "ModelShaderVariants";
        private Vector2 scrollPosition;
        private List<Material> foundMaterials = new List<Material>();
        private Dictionary<Shader, List<string>> shaderKeywords = new Dictionary<Shader, List<string>>();

        [MenuItem("DuckovCustomModel/Shader Variant Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<ShaderVariantTool>("Shader Variant Tool");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // Warning message
            EditorGUILayout.HelpBox(
                "⚠️ WARNING: This tool is NOT for lilToon shaders!\n\n" +
                "lilToon shaders must be packaged WITH materials in the same AssetBundle. " +
                "Using this tool for lilToon will cause rendering issues.\n\n" +
                "This tool is for standard Unity shaders or other custom shaders that support separate packaging.",
                MessageType.Warning
            );

            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Shader Variant Collection Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Target prefab selection
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("1. Select Target Prefab", EditorStyles.boldLabel);
            targetPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Model Prefab", 
                targetPrefab, 
                typeof(GameObject), 
                false
            );
            
            if (GUILayout.Button("Scan Materials"))
            {
                ScanMaterials();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Display found materials and shaders
            if (foundMaterials.Count > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("2. Found Materials & Shaders", EditorStyles.boldLabel);
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                
                foreach (var kvp in shaderKeywords)
                {
                    EditorGUILayout.LabelField($"Shader: {kvp.Key.name}", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    
                    foreach (var keyword in kvp.Value)
                    {
                        EditorGUILayout.LabelField($"• {keyword}");
                    }
                    
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space(5);
                }
                
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(10);

                // Output configuration
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("3. Output Configuration", EditorStyles.boldLabel);
                
                outputPath = EditorGUILayout.TextField("Output Folder", outputPath);
                collectionName = EditorGUILayout.TextField("Collection Name", collectionName);
                
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("Create Shader Variant Collection", GUILayout.Height(30)))
                {
                    CreateShaderVariantCollection();
                }
                
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);

            // Instructions
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("How to Use:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "1. Select your model prefab\n" +
                "2. Click 'Scan Materials' to analyze shaders\n" +
                "3. Review the found shaders and keywords\n" +
                "4. Configure output path and name\n" +
                "5. Click 'Create Shader Variant Collection'\n" +
                "6. Include the generated .shadervariants file in your shader bundle",
                EditorStyles.wordWrappedLabel
            );
            EditorGUILayout.EndVertical();
        }

        private void ScanMaterials()
        {
            if (targetPrefab == null)
            {
                EditorUtility.DisplayDialog(
                    "No Prefab Selected",
                    "Please select a model prefab to scan.",
                    "OK"
                );
                return;
            }

            foundMaterials.Clear();
            shaderKeywords.Clear();

            // Get all renderers in the prefab
            var renderers = targetPrefab.GetComponentsInChildren<Renderer>(true);
            
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null) continue;
                    if (foundMaterials.Contains(material)) continue;
                    
                    foundMaterials.Add(material);
                    
                    var shader = material.shader;
                    if (shader == null) continue;

                    // Check if it's lilToon and warn - using StartsWith for more accurate detection
                    if (shader.name.StartsWith("lilToon") || 
                        shader.name.StartsWith("Hidden/lilToon") ||
                        shader.name.Contains("/lilToon"))
                    {
                        Debug.LogWarning(
                            $"⚠️ lilToon shader detected in material '{material.name}' (Shader: {shader.name})!\n" +
                            "lilToon shaders should NOT use separate shader bundles. " +
                            "Please package lilToon with your materials."
                        );
                    }

                    if (!shaderKeywords.ContainsKey(shader))
                    {
                        shaderKeywords[shader] = new List<string>();
                    }

                    // Collect enabled keywords
                    var keywords = material.shaderKeywords;
                    foreach (var keyword in keywords)
                    {
                        if (!shaderKeywords[shader].Contains(keyword))
                        {
                            shaderKeywords[shader].Add(keyword);
                        }
                    }
                }
            }

            if (foundMaterials.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Materials Found",
                    "No materials were found in the selected prefab.",
                    "OK"
                );
            }
            else
            {
                Debug.Log($"Found {foundMaterials.Count} materials using {shaderKeywords.Count} different shaders.");
            }
        }

        private void CreateShaderVariantCollection()
        {
            if (shaderKeywords.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Shaders Found",
                    "Please scan materials first before creating a variant collection.",
                    "OK"
                );
                return;
            }

            // Create output directory if it doesn't exist
            if (!AssetDatabase.IsValidFolder(outputPath))
            {
                var folders = outputPath.Split('/');
                var currentPath = folders[0];
                
                for (int i = 1; i < folders.Length; i++)
                {
                    var newPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
            }

            // Create shader variant collection
            var collection = new ShaderVariantCollection();
            
            // Common pass types to include for comprehensive coverage
            var passTypes = new[]
            {
                UnityEngine.Rendering.PassType.Normal,
                UnityEngine.Rendering.PassType.ForwardBase,
                UnityEngine.Rendering.PassType.ForwardAdd,
                UnityEngine.Rendering.PassType.ShadowCaster,
                UnityEngine.Rendering.PassType.Deferred
            };
            
            foreach (var kvp in shaderKeywords)
            {
                var shader = kvp.Key;
                var keywords = kvp.Value;

                foreach (var passType in passTypes)
                {
                    // Add variant with no keywords (base variant)
                    var baseVariant = new ShaderVariantCollection.ShaderVariant(
                        shader,
                        passType
                    );
                    
                    try
                    {
                        collection.Add(baseVariant);
                    }
                    catch (System.Exception e)
                    {
                        // Silently skip unsupported pass types - not all shaders support all passes
                        if (!e.Message.Contains("does not exist") && !e.Message.Contains("not found"))
                        {
                            Debug.LogWarning($"Could not add base variant for {shader.name} (Pass: {passType}): {e.Message}");
                        }
                    }

                    // Add variants with keywords
                    if (keywords.Count > 0)
                    {
                        var keywordArray = keywords.ToArray();
                        var variant = new ShaderVariantCollection.ShaderVariant(
                            shader,
                            passType,
                            keywordArray
                        );
                        
                        try
                        {
                            collection.Add(variant);
                        }
                        catch (System.Exception e)
                        {
                            // Silently skip unsupported combinations
                            if (!e.Message.Contains("does not exist") && !e.Message.Contains("not found"))
                            {
                                Debug.LogWarning($"Could not add variant for {shader.name} (Pass: {passType}) with keywords [{string.Join(", ", keywords)}]: {e.Message}");
                            }
                        }
                    }
                }
            }

            // Save the collection
            var assetPath = $"{outputPath}/{collectionName}.shadervariants";
            AssetDatabase.CreateAsset(collection, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Select the created asset
            var createdAsset = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(assetPath);
            Selection.activeObject = createdAsset;
            EditorGUIUtility.PingObject(createdAsset);

            EditorUtility.DisplayDialog(
                "Success",
                $"Shader Variant Collection created successfully!\n\n" +
                $"Location: {assetPath}\n" +
                $"Variants: {collection.variantCount}\n\n" +
                "Next steps:\n" +
                "1. Build this collection into your shader bundle\n" +
                "2. Set ShaderVariantPath in bundleinfo.json\n" +
                "3. Set WarmupShaders to true",
                "OK"
            );

            Debug.Log($"✅ Created shader variant collection with {collection.variantCount} variants at {assetPath}");
        }
    }
}
#endif
