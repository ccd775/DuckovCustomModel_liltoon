# lilToon Shader Guide

English | [中文](GUIDE_LILTOON.md)

## Table of Contents

- [Why lilToon Requires Special Handling](#why-liltoon-requires-special-handling)
- [lilToon's Automatic Optimization Mechanism](#liltoons-automatic-optimization-mechanism)
- [Correct lilToon Bundling Workflow](#correct-liltoon-bundling-workflow)
- [Material Property Settings](#material-property-settings)
- [Frequently Asked Questions](#frequently-asked-questions)

## Why lilToon Requires Special Handling

lilToon is a powerful Unity Shader that automatically performs the following optimizations during build time to optimize performance and reduce build size:

1. **Automatic Shader Stripping**: Automatically removes unused shader variants based on the actual material configurations used in the scene
2. **Build-time Optimization**: During AssetBundle building, lilToon analyzes material properties and generates optimized shader code

**This means**: If you package the lilToon Shader separately into a Shader Bundle while packaging materials that use it into another model Bundle, lilToon's optimization mechanism will not work properly, potentially causing:

- Lost shader variants, resulting in incorrect material display (pink/magenta color)
- Missing necessary shader features, causing rendering anomalies
- Runtime loading failures or crashes

## lilToon's Automatic Optimization Mechanism

### How It Works

When you build AssetBundles in the Unity Editor, lilToon will:

1. **Scan all materials in the Bundle**: Check each material using the lilToon Shader
2. **Analyze material properties**: Determine which features each material has enabled (e.g., Outline, Emission, MatCap, etc.)
3. **Generate optimized variants**: Only include actually used shader variants, removing unused code paths
4. **Package into the same Bundle**: The optimized shader is packaged together with the materials

### Why Separate Packaging Doesn't Work

If you package shaders and materials separately:

```
❌ Wrong approach:
├── shaders.bundle          # Contains lilToon Shader
└── character.bundle        # Contains materials using lilToon
```

The problem is:

- When building `shaders.bundle`, lilToon doesn't know which variants are needed by materials in `character.bundle`
- When building `character.bundle`, it cannot access the optimized shader variants
- Result: Either shader variants don't match, or features are missing

## Correct lilToon Bundling Workflow

### Step 1: Install lilToon

1. Obtain lilToon from the [official lilToon repository](https://github.com/lilxyzw/lilToon) or BOOTH
2. Import the lilToon package into your Unity project
3. Confirm that lilToon Shader is available under the `Assets/lilToon` directory

### Step 2: Create and Configure Materials

1. Create materials and select a lilToon Shader (e.g., `lilToon`, `lilToonOutline`, etc.)
2. Configure material properties (colors, textures, effects, etc.)
3. **Important**: In the material's `Advanced Settings`:
   - Find the `Remove Unused Properties` option
   - Select the appropriate setting based on your needs (see "Material Property Settings" below)

### Step 3: Set Up Model Prefab

1. Apply the configured materials to your model
2. Ensure all Renderer components in the Prefab correctly reference the materials
3. Verify that materials display correctly in the editor

### Step 4: Configure AssetBundle Packaging

**Key point: Do NOT set a separate AssetBundle Name for the lilToon Shader**

1. Select your model Prefab
2. At the bottom of the Inspector panel, set the AssetBundle Name (e.g., `charactermodel`)
3. **Do NOT** set an AssetBundle Name for the lilToon Shader files
4. **Do NOT** set a separate AssetBundle Name for materials (materials should follow the Prefab)

Unity will automatically package all dependencies referenced by the Prefab (including materials and Shaders) into the same Bundle.

### Step 5: Build AssetBundle

Use the standard Unity AssetBundle build process:

```csharp
BuildPipeline.BuildAssetBundles(
    outputPath,
    BuildAssetBundleOptions.None,
    BuildTarget.StandaloneWindows64
);
```

lilToon will automatically optimize shader variants during the build process.

### Step 6: Configure bundleinfo.json

In your model package configuration file, **leave `ShaderBundlePath` empty or omit it entirely**:

```json
{
  "BundleName": "My Character Model",
  "BundlePath": "charactermodel.bundle",
  "Models": [
    {
      "ModelID": "my_liltoon_character",
      "Name": "lilToon Character",
      "Author": "Your Name",
      "Version": "1.0.0",
      "PrefabPath": "Assets/Characters/MyCharacter.prefab",
      "Target": ["Character"]
    }
  ]
}
```

**Note**:
- **Do NOT** set `ShaderBundlePath`
- **Do NOT** set `ShaderVariantPath`
- The Shader is already included in the Bundle specified by `BundlePath`

### Step 7: Test Your Model Package

1. Copy the model package folder to `ModConfigs/DuckovCustomModel/Models/`
2. Start the game
3. Select your model in the model selection interface
4. Verify that materials display correctly without pink textures

## Material Property Settings

### Remove Unused Properties Option

In the `Advanced Settings` section of lilToon materials, there's a `Remove Unused Properties` option that affects AssetBundle size and compatibility:

#### Option Descriptions

- **Don't Remove**
  - Keeps all material properties, even if unused
  - Pros: Maximum compatibility, can modify any property at runtime
  - Cons: Larger AssetBundle file size
  - **Recommended for**: Cases where materials need to be modified dynamically in-game

- **Remove Unused Properties**
  - Only keeps properties related to currently enabled features
  - Pros: Significantly reduces AssetBundle size
  - Cons: Cannot enable removed features at runtime
  - **Recommended for**: Static models that don't require runtime modifications

- **Remove All**
  - Most aggressive optimization, removes all removable properties
  - Pros: Smallest file size
  - Cons: May cause compatibility issues
  - **Use with caution**

#### Recommended Settings

For most models:

1. If your model materials are fixed and don't need in-game modifications:
   - Choose **Remove Unused Properties**
   - This provides a good balance of file size and compatibility

2. If you need to dynamically toggle material effects in-game (e.g., emission, color changes):
   - Choose **Don't Remove**
   - Trade some file size for flexibility

3. Always check before building:
   - Preview materials in the editor to ensure effects are correct
   - Test that all required visual effects are enabled
   - Confirm you won't accidentally remove needed features

### Shader Variant Related Settings

lilToon provides options to control shader variants:

- **Shader Setting** → **Optimization**
  - Manually remove unneeded rendering modes (e.g., Cutout, Transparent)
  - Reduces variant count, optimizing performance and file size

- Disable unused features before building:
  - If you don't need Outline, turn off Outline settings
  - If you don't need MatCap, turn off MatCap settings
  - This reduces the number of built variants

## Frequently Asked Questions

### Q: Can I share lilToon Shader across multiple models?

A: Not recommended. Each model package should contain its own copy of the lilToon Shader. While this increases overall file size, it ensures:
- Each model's Shader is optimized for its materials
- Different model packages are independent and don't affect each other
- Avoid version conflicts (different models may use different versions of lilToon)

### Q: My materials are showing as pink, what should I do?

A: Pink materials usually indicate missing or failed-to-load Shaders. Check:

1. **Confirm Shader is included in the Bundle**:
   - Rebuild the AssetBundle
   - Ensure no separate AssetBundle Name is set for the Shader

2. **Check bundleinfo.json**:
   - Confirm `ShaderBundlePath` is empty or not set
   - Verify `BundlePath` points to the correct Bundle file

3. **Verify material references**:
   - Check Prefab material references in Unity Editor
   - Confirm material Shader is set to lilToon

4. **Check lilToon version**:
   - Ensure the lilToon version is compatible with the game
   - Recommend using stable versions rather than beta

### Q: Does using lilToon affect game performance?

A: When used correctly, the impact is minimal:

- lilToon's automatic optimization removes unused features, generating efficient shader code
- Compared to the full shader, optimized variants have lower performance overhead
- Be careful not to enable too many advanced effects (e.g., multi-layer MatCap, complex Outline, etc.)

Performance optimization tips:
- Only enable needed features
- Use appropriate texture resolutions
- Avoid overusing transparent materials
- Test performance on low-end devices

### Q: Can I use lilToon's preset materials?

A: Yes, but note:

1. If using lilToon's provided preset materials:
   - Create a copy of the material instead of using the original preset directly
   - Make modifications and optimizations on the copy
   - Ensure the copy will be included in your AssetBundle

2. Verify dependencies:
   - Check that all textures used by the preset material are in your project
   - Confirm these textures will be packaged correctly

### Q: Should I use ShaderVariantCollection?

A: For lilToon, **usually not needed**:

- lilToon's automatic optimization already handles variant management
- Manually creating ShaderVariantCollection may conflict with lilToon's optimization
- Unless you fully understand lilToon's internal mechanisms, it's not recommended

If you need to use other standard Shaders (non-lilToon) and want to optimize loading performance, you can consider using ShaderVariantCollection. See the Shader Bundle Support section in the main README.

### Q: What should I be aware of when updating lilToon versions?

A: When updating lilToon:

1. **Rebuild all AssetBundles using lilToon**:
   - New versions may have different variants or optimization strategies
   - Old Bundles may be incompatible with new versions

2. **Test all models**:
   - Verify materials display correctly
   - Check that effects (Outline, Emission, etc.) work properly
   - Confirm no significant performance degradation

3. **Keep backups**:
   - Backup old version Bundles before updating
   - Can quickly rollback if issues occur

4. **Read the changelog**:
   - Understand changes in the new version
   - Note any breaking changes or new best practices

### Q: Can I mix lilToon with other Shaders?

A: Yes, mixing different Shaders in the same model is supported:

1. **Different materials using different Shaders**:
   - For example, character body uses lilToon, weapon uses Standard Shader
   - Unity will automatically include all needed Shaders

2. **Considerations**:
   - Each Shader type may have different packaging strategies
   - lilToon should be packaged with materials
   - Standard Shaders can be packaged separately or together

3. **Best practices**:
   - Keep it simple, avoid too many shader types
   - Unified Shaders are easier to manage and optimize
   - Consider using a consistent visual style

## Summary

Key points when using lilToon Shader:

✅ **Should do**:
- Package lilToon Shader together with materials and Prefabs that use it in the same AssetBundle
- Leave `ShaderBundlePath` empty in `bundleinfo.json`
- Set `Remove Unused Properties` appropriately based on needs
- Only enable needed lilToon features to optimize performance
- Thoroughly test material effects in the editor

❌ **Should NOT do**:
- Don't package lilToon Shader separately into a Shader Bundle
- Don't set a separate AssetBundle Name for lilToon Shader files
- Don't configure `ShaderBundlePath` to point to lilToon in `bundleinfo.json`
- Don't expect to modify removed material properties at runtime

Follow these guidelines, and your lilToon models will load and render correctly while maintaining good performance and file size!

## Related Resources

- [lilToon Official Documentation](https://lilxyzw.github.io/lilToon/#/)
- [lilToon GitHub Repository](https://github.com/lilxyzw/lilToon)
- [Main README](README_EN.md) - DuckovCustomModel Usage Guide
- [Shader Bundle Support](README_EN.md#shader-bundle-support-advanced-feature) - For other custom Shaders
