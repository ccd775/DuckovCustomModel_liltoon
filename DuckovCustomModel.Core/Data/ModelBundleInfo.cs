using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace DuckovCustomModel.Core.Data
{
    public class ModelBundleInfo
    {
        public string BundleName { get; set; } = string.Empty;
        public string BundlePath { get; set; } = string.Empty;

        public ModelInfo[] Models { get; set; } = [];

        /// <summary>
        /// Shader Bundle path (relative to model directory).
        /// 
        /// IMPORTANT: NOT recommended for lilToon shader!
        /// - Leave this empty/null for lilToon models to package the shader WITH the model
        /// - lilToon's build-time optimization requires it to be in the same bundle as the model
        /// - Separating lilToon shader may cause variant loss and rendering issues
        /// 
        /// This is intended for other custom shaders that don't have build-time optimization.
        /// </summary>
        public string? ShaderBundlePath { get; set; }

        /// <summary>
        /// Shader Variant Collection asset path within the Shader Bundle
        /// </summary>
        public string? ShaderVariantPath { get; set; }

        /// <summary>
        /// Whether to warmup shader variants on load
        /// </summary>
        public bool WarmupShaders { get; set; } = true;

        [JsonIgnore] public string DirectoryPath { get; internal set; } = string.Empty;

        public static ModelBundleInfo? LoadFromDirectory(string directoryPath,
            JsonSerializerSettings? jsonSettings = null)
        {
            var infoFilePath = Path.Combine(directoryPath, "bundleinfo.json");
            if (!File.Exists(infoFilePath)) return null;

            try
            {
                var json = File.ReadAllText(infoFilePath);
                var settings = jsonSettings ?? JsonSettings.Default;
                var info = JsonConvert.DeserializeObject<ModelBundleInfo>(json, settings);
                if (info == null) return info;
                info.DirectoryPath = directoryPath;
                info.Models = info.Models.Where(model => model.Validate()).ToArray();
                return info;
            }
            catch (JsonException ex)
            {
                ModLogger.LogError(
                    $"Failed to parse bundleinfo.json in '{directoryPath}': {ex.Message}");
                ModLogger.LogException(ex);
                return null;
            }
            catch (Exception ex)
            {
                ModLogger.LogError(
                    $"Failed to load bundleinfo.json from '{directoryPath}': {ex.Message}");
                ModLogger.LogException(ex);
                return null;
            }
        }

        public ModelBundleInfo CreateFilteredCopy(ModelInfo[] filteredModels)
        {
            var copy = new ModelBundleInfo
            {
                BundleName = BundleName,
                BundlePath = BundlePath,
                ShaderBundlePath = ShaderBundlePath,
                ShaderVariantPath = ShaderVariantPath,
                WarmupShaders = WarmupShaders,
                Models = filteredModels,
                DirectoryPath = DirectoryPath,
            };
            return copy;
        }
    }
}
