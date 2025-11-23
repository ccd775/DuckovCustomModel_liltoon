# Shader Variant Collection Tool

这是一个可选的 Unity Editor 工具脚本，用于帮助 Modder 收集和管理着色器变体。

**重要提示**：此工具**不适用于 lilToon 着色器**。lilToon 有自己的自动优化机制，应该随模型一起打包。此工具仅用于需要独立打包的其他自定义着色器。

## 适用场景

- 使用标准 Unity 着色器或其他不具备自动优化功能的自定义着色器
- 需要将着色器独立打包以供多个模型共享
- 需要手动控制着色器变体的包含和排除

## 不适用场景

- **lilToon 着色器** - 请参阅 README.md 中的 "lilToon Shader 工作流程指南"
- 其他具有自动变体优化功能的着色器系统

## 使用方法

### 步骤 1：创建 Editor 脚本

在您的 Unity 项目中创建一个 `Editor` 文件夹（如果尚未存在），然后在其中创建以下脚本：

**文件路径**：`Assets/Editor/ShaderVariantTool.cs`

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 着色器变体收集工具
/// 用于从场景中的材质收集使用的着色器变体，并创建 ShaderVariantCollection
/// </summary>
public class ShaderVariantTool : EditorWindow
{
    private GameObject targetPrefab;
    private string outputPath = "Assets/ShaderVariants/CollectedVariants.shadervariants";
    private Vector2 scrollPosition;
    private Dictionary<Shader, HashSet<Material>> shaderMaterialMap = new Dictionary<Shader, HashSet<Material>>();
    private bool showDetails = false;

    [MenuItem("Tools/Shader Variant Collection Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<ShaderVariantTool>("Shader Variant Tool");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("着色器变体收集工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "此工具用于从 Prefab 中收集使用的着色器变体。\n" +
            "注意：不适用于 lilToon 等具有自动优化功能的着色器。",
            MessageType.Info);

        EditorGUILayout.Space();

        // 目标 Prefab 选择
        EditorGUILayout.LabelField("1. 选择要分析的 Prefab", EditorStyles.boldLabel);
        targetPrefab = EditorGUILayout.ObjectField("目标 Prefab", targetPrefab, typeof(GameObject), false) as GameObject;

        EditorGUILayout.Space();

        // 分析按钮
        using (new EditorGUI.DisabledScope(targetPrefab == null))
        {
            if (GUILayout.Button("分析 Prefab 中的着色器", GUILayout.Height(30)))
            {
                AnalyzePrefab();
            }
        }

        EditorGUILayout.Space();

        // 显示分析结果
        if (shaderMaterialMap.Count > 0)
        {
            EditorGUILayout.LabelField("2. 分析结果", EditorStyles.boldLabel);
            
            showDetails = EditorGUILayout.Foldout(showDetails, $"发现 {shaderMaterialMap.Count} 个着色器");
            
            if (showDetails)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                
                foreach (var kvp in shaderMaterialMap)
                {
                    if (kvp.Key == null) continue;
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(kvp.Key.name, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"使用此着色器的材质数量: {kvp.Value.Count}");
                    
                    foreach (var mat in kvp.Value)
                    {
                        if (mat != null)
                        {
                            EditorGUILayout.ObjectField(mat, typeof(Material), false);
                        }
                    }
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }
                
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space();

            // 输出路径
            EditorGUILayout.LabelField("3. 创建 ShaderVariantCollection", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            outputPath = EditorGUILayout.TextField("输出路径", outputPath);
            if (GUILayout.Button("浏览...", GUILayout.Width(60)))
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "保存 ShaderVariantCollection",
                    "ShaderVariants",
                    "shadervariants",
                    "选择保存位置");
                if (!string.IsNullOrEmpty(path))
                {
                    outputPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 创建按钮
            if (GUILayout.Button("创建 ShaderVariantCollection", GUILayout.Height(30)))
            {
                CreateShaderVariantCollection();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "创建 ShaderVariantCollection 后，您需要：\n" +
                "1. 手动添加需要的变体（打开 SVC 资源，点击着色器展开变体列表）\n" +
                "2. 将 SVC 打包到着色器 AssetBundle 中\n" +
                "3. 在 bundleinfo.json 中配置 ShaderVariantPath",
                MessageType.Info);
        }

        EditorGUILayout.Space();
    }

    private void AnalyzePrefab()
    {
        shaderMaterialMap.Clear();

        if (targetPrefab == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选择一个 Prefab", "确定");
            return;
        }

        // 获取 Prefab 中所有的 Renderer 组件
        var renderers = targetPrefab.GetComponentsInChildren<Renderer>(true);
        
        if (renderers.Length == 0)
        {
            EditorUtility.DisplayDialog("警告", "在 Prefab 中未找到任何 Renderer 组件", "确定");
            return;
        }

        // 收集所有材质使用的着色器
        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null || material.shader == null)
                    continue;

                if (!shaderMaterialMap.ContainsKey(material.shader))
                {
                    shaderMaterialMap[material.shader] = new HashSet<Material>();
                }

                shaderMaterialMap[material.shader].Add(material);
            }
        }

        Debug.Log($"分析完成：在 {renderers.Length} 个 Renderer 中发现 {shaderMaterialMap.Count} 个不同的着色器");
    }

    private void CreateShaderVariantCollection()
    {
        if (shaderMaterialMap.Count == 0)
        {
            EditorUtility.DisplayDialog("错误", "请先分析 Prefab", "确定");
            return;
        }

        // 确保输出目录存在
        string directory = System.IO.Path.GetDirectoryName(outputPath);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        // 创建 ShaderVariantCollection
        ShaderVariantCollection collection = new ShaderVariantCollection();

        // 为每个着色器添加基础变体
        foreach (var shader in shaderMaterialMap.Keys)
        {
            if (shader == null) continue;

            // 添加基础变体（无关键字）
            var variant = new ShaderVariantCollection.ShaderVariant
            {
                shader = shader,
                passType = UnityEngine.Rendering.PassType.Normal,
                keywords = new string[0]
            };

            try
            {
                if (!collection.Contains(variant))
                {
                    collection.Add(variant);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"无法添加着色器 {shader.name} 的基础变体: {e.Message}");
            }
        }

        // 保存资源
        AssetDatabase.CreateAsset(collection, outputPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 选中创建的资源
        EditorGUIUtility.PingObject(collection);
        Selection.activeObject = collection;

        EditorUtility.DisplayDialog(
            "成功",
            $"已创建 ShaderVariantCollection，包含 {shaderMaterialMap.Count} 个着色器的基础变体。\n\n" +
            "请在 Inspector 中手动添加您需要的变体关键字组合。",
            "确定");

        Debug.Log($"已创建 ShaderVariantCollection: {outputPath}");
    }
}
```

### 步骤 2：使用工具

1. 在 Unity 编辑器中，选择菜单 `Tools > Shader Variant Collection Tool`
2. 将您的模型 Prefab 拖入"目标 Prefab"字段
3. 点击"分析 Prefab 中的着色器"按钮
4. 查看分析结果，确认收集到的着色器
5. 设置输出路径（或使用默认路径）
6. 点击"创建 ShaderVariantCollection"按钮

### 步骤 3：配置变体

工具创建的 ShaderVariantCollection 只包含基础变体。您需要手动添加实际使用的变体：

1. 在 Project 窗口中选择创建的 `.shadervariants` 文件
2. 在 Inspector 中展开每个着色器
3. 点击"Add variant"按钮添加需要的变体
4. 选择正确的 Pass Type 和 Keywords 组合

**提示**：要找到需要的变体组合，您可以：
- 在 Unity 编辑器的 Graphics Settings 中查看项目使用的着色器变体
- 使用 Frame Debugger 查看运行时使用的着色器变体
- 参考材质当前启用的功能和关键字

### 步骤 4：打包着色器 Bundle

创建 Editor 脚本来构建着色器 AssetBundle：

**文件路径**：`Assets/Editor/BuildShaderBundle.cs`

```csharp
using UnityEditor;
using UnityEngine;

public class BuildShaderBundle
{
    [MenuItem("Tools/Build Shader Bundle")]
    public static void BuildBundle()
    {
        string outputPath = "Assets/../Output";
        
        if (!System.IO.Directory.Exists(outputPath))
        {
            System.IO.Directory.CreateDirectory(outputPath);
        }

        // 配置要打包的着色器变体集合
        string shaderVariantPath = "Assets/ShaderVariants/CollectedVariants.shadervariants";
        
        if (!System.IO.File.Exists(shaderVariantPath))
        {
            EditorUtility.DisplayDialog("错误", 
                $"找不到着色器变体文件：{shaderVariantPath}\n请先使用 Shader Variant Collection Tool 创建。", 
                "确定");
            return;
        }

        BuildPipeline.BuildAssetBundles(
            outputPath,
            new AssetBundleBuild[]
            {
                new AssetBundleBuild
                {
                    assetBundleName = "shaders.bundle",
                    assetNames = new[] { shaderVariantPath }
                }
            },
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows64
        );

        Debug.Log($"着色器包构建完成：{outputPath}/shaders.bundle");
        EditorUtility.RevealInFinder(outputPath);
    }
}
```

使用方法：
1. 选择菜单 `Tools > Build Shader Bundle`
2. 等待构建完成
3. 在 `Output` 文件夹中找到 `shaders.bundle` 文件

### 步骤 5：配置 bundleinfo.json

将构建的 `shaders.bundle` 复制到您的模型包文件夹，然后配置 `bundleinfo.json`：

```json
{
  "BundleName": "我的自定义着色器模型",
  "BundlePath": "mymodel.assetbundle",
  "ShaderBundlePath": "shaders.bundle",
  "ShaderVariantPath": "Assets/ShaderVariants/CollectedVariants.shadervariants",
  "WarmupShaders": true,
  "Models": [
    {
      "ModelID": "custom_shader_model",
      "Name": "自定义着色器模型",
      "PrefabPath": "Assets/MyModel.prefab",
      "Target": ["Character"],
      "Features": ["NoAutoShaderReplace"]
    }
  ]
}
```

## 常见问题

### Q: 为什么不能用于 lilToon？

A: lilToon 具有构建时自动优化机制，它会：
- 自动分析材质属性
- 只保留实际使用的变体
- 移除未使用的属性

这些优化需要在构建时与材质一起运行。如果分离打包，优化机制会失效，导致包体积增大、性能下降，甚至可能出现渲染错误。

### Q: 如何知道需要哪些着色器变体？

A: 推荐方法：
1. **使用 Frame Debugger**：在 Play 模式下打开 Window > Analysis > Frame Debugger，查看实际使用的着色器变体
2. **检查材质设置**：查看材质上启用了哪些特性（如 Normal Map、Emission、Transparency 等）
3. **测试不同场景**：在不同光照条件、天气、时间下测试，确保所有需要的变体都包含

### Q: ShaderVariantCollection 太大怎么办？

A: 优化建议：
1. 只包含确实需要的变体，不要包含所有可能的组合
2. 移除不会在游戏中使用的 Pass Type（如 ShadowCaster、Meta 等可能不需要）
3. 考虑简化着色器，减少可选特性
4. 对于共享着色器，创建多个较小的变体集合而不是一个大的

### Q: 如何测试着色器变体是否正确？

A: 测试步骤：
1. 构建 AssetBundle 并放入游戏
2. 加载模型，检查是否有粉红色材质（表示着色器缺失）
3. 使用 Frame Debugger 查看实际使用的着色器变体
4. 在不同场景、光照条件下测试
5. 检查游戏日志是否有着色器编译或加载错误

## 参考资源

- [Unity ShaderVariantCollection 官方文档](https://docs.unity3d.com/ScriptReference/ShaderVariantCollection.html)
- [Unity AssetBundle 最佳实践](https://docs.unity3d.com/Manual/AssetBundles-BestPractices.html)
- [Unity Graphics - Managing Shader Compilation](https://docs.unity3d.com/Manual/shader-compilation-variants.html)

## 限制和注意事项

1. **此工具仅创建基础框架**：您需要手动添加具体的变体关键字组合
2. **不支持自动变体检测**：Unity 没有提供 API 自动检测材质需要的所有变体
3. **需要手动测试验证**：必须在实际游戏环境中测试以确保所有变体都正确包含
4. **平台相关性**：不同平台可能需要不同的变体，示例脚本只构建 Windows 版本

## 总结

对于大多数情况，我们建议：
- **lilToon 着色器**：与模型一起打包，不使用此工具
- **Unity 标准着色器**：通常已包含在游戏中，不需要额外打包
- **其他自定义着色器**：使用此工具收集变体，但需要仔细测试

如有问题或建议，欢迎在项目 Issue 中反馈！
