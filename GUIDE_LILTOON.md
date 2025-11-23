# lilToon Shader 使用指南

[English](GUIDE_LILTOON_EN.md) | 中文

## 目录

- [为什么 lilToon 需要特殊处理](#为什么-liltoon-需要特殊处理)
- [lilToon 的自动优化机制](#liltoon-的自动优化机制)
- [正确的 lilToon 打包流程](#正确的-liltoon-打包流程)
- [材质属性设置注意事项](#材质属性设置注意事项)
- [常见问题解答](#常见问题解答)

## 为什么 lilToon 需要特殊处理

lilToon 是一个功能强大的 Unity Shader，为了优化性能和减小构建体积，它在构建时会自动进行以下优化：

1. **自动变体剔除（Auto Shader Stripping）**：根据场景中实际使用的材质配置，自动移除未使用的 Shader 变体
2. **构建时优化（Build-time Optimization）**：在 AssetBundle 构建过程中，lilToon 会分析材质属性并生成最优化的 Shader 代码

**这意味着**：如果将 lilToon Shader 单独打包到一个 Shader Bundle 中，而将使用该 Shader 的材质打包到另一个模型 Bundle 中，lilToon 的优化机制将无法正常工作，可能导致：

- Shader 变体丢失，材质显示错误（粉红色/洋红色）
- 缺少必要的 Shader 功能，导致渲染异常
- 运行时加载失败或崩溃

## lilToon 的自动优化机制

### 工作原理

当你在 Unity 编辑器中构建 AssetBundle 时，lilToon 会：

1. **扫描 Bundle 中的所有材质**：检查每个使用 lilToon Shader 的材质
2. **分析材质属性**：确定每个材质启用了哪些功能（如 Outline、Emission、MatCap 等）
3. **生成优化的变体**：仅包含实际使用的 Shader 变体，移除未使用的代码路径
4. **打包到同一个 Bundle**：优化后的 Shader 与材质一起打包

### 为什么不能分离打包

如果你将 Shader 和材质分开打包：

```
❌ 错误做法：
├── shaders.bundle          # 包含 lilToon Shader
└── character.bundle        # 包含使用 lilToon 的材质
```

问题在于：

- `shaders.bundle` 构建时，lilToon 不知道 `character.bundle` 中的材质需要哪些变体
- `character.bundle` 构建时，无法访问已优化的 Shader 变体
- 结果：要么 Shader 变体不匹配，要么功能缺失

## 正确的 lilToon 打包流程

### 步骤 1：安装 lilToon

1. 从 [lilToon 官方仓库](https://github.com/lilxyzw/lilToon) 或 BOOTH 获取 lilToon
2. 在你的 Unity 项目中导入 lilToon 包
3. 确认 lilToon Shader 在 `Assets/lilToon` 目录下可用

### 步骤 2：创建和配置材质

1. 创建材质并选择 lilToon Shader（例如 `lilToon`、`lilToonOutline` 等）
2. 配置材质属性（颜色、纹理、特效等）
3. **重要**：在材质的 `Advanced Settings` 中：
   - 找到 `Remove Unused Properties` 选项
   - 根据需求选择适当的设置（见下文"材质属性设置注意事项"）

### 步骤 3：设置模型 Prefab

1. 将配置好的材质应用到模型上
2. 确保 Prefab 中的所有 Renderer 组件都正确引用了材质
3. 验证材质在编辑器中显示正常

### 步骤 4：配置 AssetBundle 打包

**关键点：不要为 lilToon Shader 设置独立的 AssetBundle Name**

1. 选择你的模型 Prefab
2. 在 Inspector 面板底部，设置 AssetBundle Name（例如 `charactermodel`）
3. **不要**为 lilToon Shader 文件设置 AssetBundle Name
4. **不要**为材质单独设置 AssetBundle Name（材质应该跟随 Prefab）

Unity 会自动将 Prefab 引用的所有依赖（包括材质和 Shader）打包到同一个 Bundle 中。

### 步骤 5：构建 AssetBundle

使用标准的 Unity AssetBundle 构建流程：

```csharp
BuildPipeline.BuildAssetBundles(
    outputPath,
    BuildAssetBundleOptions.None,
    BuildTarget.StandaloneWindows64
);
```

lilToon 会在构建过程中自动优化 Shader 变体。

### 步骤 6：配置 bundleinfo.json

在你的模型包配置文件中，**将 `ShaderBundlePath` 留空或完全省略**：

```json
{
  "BundleName": "我的角色模型",
  "BundlePath": "charactermodel.bundle",
  "Models": [
    {
      "ModelID": "my_liltoon_character",
      "Name": "lilToon 角色",
      "Author": "你的名字",
      "Version": "1.0.0",
      "PrefabPath": "Assets/Characters/MyCharacter.prefab",
      "Target": ["Character"]
    }
  ]
}
```

**注意**：
- **不要**设置 `ShaderBundlePath`
- **不要**设置 `ShaderVariantPath`
- Shader 已经包含在 `BundlePath` 指定的 Bundle 中

### 步骤 7：测试模型包

1. 将模型包文件夹复制到 `ModConfigs/DuckovCustomModel/Models/`
2. 启动游戏
3. 在模型选择界面中选择你的模型
4. 验证材质显示正常，没有粉红色纹理

## 材质属性设置注意事项

### Remove Unused Properties 选项

在 lilToon 材质的 `Advanced Settings` 部分，有一个 `Remove Unused Properties` 选项，它会影响 AssetBundle 的大小和兼容性：

#### 选项说明

- **Don't Remove**（不移除）
  - 保留所有材质属性，即使没有使用
  - 优点：最大兼容性，可以在运行时修改任何属性
  - 缺点：AssetBundle 文件更大
  - **推荐用于**：需要在游戏中动态修改材质的情况

- **Remove Unused Properties**（移除未使用的属性）
  - 只保留当前启用的功能相关的属性
  - 优点：显著减小 AssetBundle 大小
  - 缺点：无法在运行时启用已移除的功能
  - **推荐用于**：静态模型，不需要运行时修改

- **Remove All**（移除所有可移除的属性）
  - 最激进的优化，移除所有可以移除的属性
  - 优点：最小的文件大小
  - 缺点：可能导致兼容性问题
  - **谨慎使用**

#### 推荐设置

对于大多数模型：

1. 如果你的模型材质是固定的，不需要在游戏中修改：
   - 选择 **Remove Unused Properties**
   - 这样可以获得良好的文件大小和兼容性平衡

2. 如果你需要在游戏中动态切换材质效果（如发光、颜色变化）：
   - 选择 **Don't Remove**
   - 牺牲一些文件大小换取灵活性

3. 构建前务必检查：
   - 在编辑器中预览材质确保效果正确
   - 测试所有需要的视觉效果都已启用
   - 确认不会意外移除需要的功能

### Shader 变体相关设置

lilToon 提供了一些控制 Shader 变体的选项：

- **Shader Setting** → **Optimization**
  - 可以手动移除不需要的渲染模式（如 Cutout、Transparent）
  - 减少变体数量，优化性能和文件大小

- 在构建前禁用未使用的功能：
  - 如果不需要 Outline，关闭 Outline 设置
  - 如果不需要 MatCap，关闭 MatCap 设置
  - 这样可以减少构建的变体数量

## 常见问题解答

### Q: 我可以在多个模型之间共享 lilToon Shader 吗？

A: 不推荐。每个模型包应该包含自己的 lilToon Shader 副本。虽然这会增加一些总体文件大小，但能确保：
- 每个模型的 Shader 都针对其材质优化
- 不同模型包之间相互独立，不会互相影响
- 避免版本冲突（不同模型可能使用不同版本的 lilToon）

### Q: 我的材质显示为粉红色，怎么办？

A: 粉红色材质通常表示 Shader 缺失或加载失败。检查：

1. **确认 Shader 已包含在 Bundle 中**：
   - 重新构建 AssetBundle
   - 确保没有为 Shader 设置独立的 AssetBundle Name

2. **检查 bundleinfo.json**：
   - 确认 `ShaderBundlePath` 为空或未设置
   - 验证 `BundlePath` 指向正确的 Bundle 文件

3. **验证材质引用**：
   - 在 Unity 编辑器中检查 Prefab 的材质引用是否正常
   - 确认材质 Shader 设置为 lilToon

4. **检查 lilToon 版本**：
   - 确保使用的 lilToon 版本与游戏兼容
   - 建议使用稳定版本而非测试版

### Q: 使用 lilToon 会影响游戏性能吗？

A: 正确使用时，影响很小：

- lilToon 的自动优化会移除未使用的功能，生成高效的 Shader 代码
- 相比完整的 Shader，优化后的变体性能开销更小
- 注意不要启用过多高级特效（如多层 MatCap、复杂 Outline 等）

性能优化建议：
- 只启用需要的功能
- 合理使用纹理分辨率
- 避免过度使用透明材质
- 在低端设备上测试性能

### Q: 我可以使用 lilToon 的预设材质吗？

A: 可以，但需要注意：

1. 如果使用 lilToon 提供的预设材质：
   - 创建材质的副本，而不是直接使用原始预设
   - 在副本上进行修改和优化
   - 确保副本会被包含在你的 AssetBundle 中

2. 验证依赖关系：
   - 检查预设材质使用的所有纹理都包含在你的项目中
   - 确认这些纹理会被正确打包

### Q: 我应该使用 ShaderVariantCollection 吗？

A: 对于 lilToon，**通常不需要**：

- lilToon 的自动优化已经处理了变体管理
- 手动创建 ShaderVariantCollection 可能与 lilToon 的优化冲突
- 除非你完全理解 lilToon 的内部机制，否则不建议使用

如果你需要使用其他标准 Shader（非 lilToon）并想优化加载性能，可以考虑使用 ShaderVariantCollection。参见主 README 中的着色器包支持部分。

### Q: 更新 lilToon 版本时需要注意什么？

A: 更新 lilToon 时：

1. **重新构建所有使用 lilToon 的 AssetBundle**：
   - 新版本可能有不同的变体或优化策略
   - 旧 Bundle 可能与新版本不兼容

2. **测试所有模型**：
   - 验证材质显示正常
   - 检查特效（Outline、Emission 等）工作正常
   - 确认性能没有明显下降

3. **保留备份**：
   - 在更新前备份旧版本的 Bundle
   - 如果有问题，可以快速回退

4. **阅读更新日志**：
   - 了解新版本的变化
   - 注意任何破坏性更改或新的最佳实践

### Q: 能否混用 lilToon 和其他 Shader？

A: 可以，在同一个模型中混用不同 Shader 是支持的：

1. **不同材质使用不同 Shader**：
   - 例如角色身体使用 lilToon，武器使用 Standard Shader
   - Unity 会自动包含所有需要的 Shader

2. **注意事项**：
   - 每种 Shader 的打包策略可能不同
   - lilToon 应该与材质一起打包
   - 标准 Shader 可以分离打包或一起打包

3. **最佳实践**：
   - 尽量保持简单，避免过多种类的 Shader
   - 统一的 Shader 更容易管理和优化
   - 考虑使用一致的视觉风格

## 总结

使用 lilToon Shader 时的关键要点：

✅ **应该做的**：
- 将 lilToon Shader 与使用它的材质和 Prefab 打包在同一个 AssetBundle 中
- 在 `bundleinfo.json` 中留空 `ShaderBundlePath`
- 根据需求合理设置 `Remove Unused Properties`
- 只启用需要的 lilToon 功能以优化性能
- 在编辑器中充分测试材质效果

❌ **不应该做的**：
- 不要将 lilToon Shader 单独打包到 Shader Bundle
- 不要为 lilToon Shader 文件设置独立的 AssetBundle Name
- 不要在 `bundleinfo.json` 中配置 `ShaderBundlePath` 指向 lilToon
- 不要期望在运行时修改已移除的材质属性

遵循这些指南，你的 lilToon 模型将能够正确加载和渲染，同时保持良好的性能和文件大小！

## 相关资源

- [lilToon 官方文档](https://lilxyzw.github.io/lilToon/#/)
- [lilToon GitHub 仓库](https://github.com/lilxyzw/lilToon)
- [主 README](README.md) - DuckovCustomModel 使用指南
- [着色器包支持](README.md#着色器包支持高级功能) - 用于其他自定义 Shader
