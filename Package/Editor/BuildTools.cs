using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FrameSynthesis.WebGLBuildTools.Editor
{
    public class BuildTools
    {
        const string BuildPath = "dist/WebGL";

        [MenuItem("WebGL/Build (Development) &b", priority = 1)]
        public static void BuildDevelopment()
        {
            Build(BuildPath, WebGLCompressionFormat.Disabled, BuildOptions.Development);
        }

        // Requires Browsersync installed (https://browsersync.io/)
        [MenuItem("WebGL/Start Local HTTPS Server", priority = 2)]
        public static void StartLocalTestServer()
        {
            Process.Start(new ProcessStartInfo
            {
                WorkingDirectory = BuildPath,
                FileName = "browser-sync",
                Arguments = "start --server --watch --https --port=1000"
            });
        }

        [MenuItem("WebGL/Open Build Folder", priority = 3)]
        public static void OpenBuildFolder()
        {
            Process.Start(Path.Combine(Application.dataPath, "..", BuildPath));
        }

        [MenuItem("WebGL/Build and Deploy to Amazon S3 (Gzip)", priority = 101)]
        public static void BuildAndDeployWithGzipCompressed()
        {
            
            Build(BuildPath, WebGLCompressionFormat.Gzip, BuildOptions.None);
            DeployToS3();
        }

        [MenuItem("WebGL/Build and Deploy to Amazon S3 (Brotli)", priority = 102)]
        public static void BuildAndDeployWithBrotliCompressed()
        {
            Build(BuildPath, WebGLCompressionFormat.Brotli, BuildOptions.None);
            DeployToS3();
        }

        static string[] ScenePaths => EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path).ToArray();

        static void Build(string executablePath, WebGLCompressionFormat compressionFormat, BuildOptions buildOptions)
        {
            var prevCompressionFormat = PlayerSettings.WebGL.compressionFormat;
            var prevDecompressionFallback = PlayerSettings.WebGL.decompressionFallback;

            var extension = compressionFormat switch
            {
                WebGLCompressionFormat.Disabled => "",
                WebGLCompressionFormat.Gzip => ".gz",
                WebGLCompressionFormat.Brotli => ".br",
                _ => throw new ArgumentOutOfRangeException()
            };

            PlayerSettings.WebGL.compressionFormat = compressionFormat;
            PlayerSettings.WebGL.decompressionFallback = false;

            BuildPipeline.BuildPlayer(ScenePaths, executablePath, BuildTarget.WebGL, buildOptions);

            PlayerSettings.WebGL.compressionFormat = prevCompressionFormat;
            PlayerSettings.WebGL.decompressionFallback = prevDecompressionFallback;

            var wasmFile = new FileInfo(Path.Combine(executablePath, "Build", $"WebGL.wasm{extension}"));
            var wasmSize = wasmFile.Length;

            var dataFile = new FileInfo(Path.Combine(executablePath, "Build", $"WebGL.data{extension}"));
            var dataSize = dataFile.Length;

            Debug.Log($"WASM size: {wasmSize / 1024} KB");
            Debug.Log($"Data size: {dataSize / 1024} KB");
            
            Unity.BuildReportInspector.BuildReportInspector.OpenLastBuild();
        }
        
        [MenuItem("WebGL/Deploy to Amazon S3", priority = 103)]
        public static void DeployToS3()
        {
            DeploySettings deploySettings;
            
            try
            {
                var guid = AssetDatabase.FindAssets("t:" + typeof(DeploySettings).FullName).Single();
                deploySettings = AssetDatabase.LoadAssetAtPath<DeploySettings>(AssetDatabase.GUIDToAssetPath(guid));
            }
            catch (InvalidOperationException)
            {
                Debug.LogError("DeploySettings object is required to deploy.");
                return;
            }

            if (!deploySettings.S3URI.StartsWith("s3://"))
            {
                Debug.LogError("S3URI should starts with S3://.");
                return;
            }

            var s3Uri = deploySettings.S3URI;
            if (deploySettings.AddTimestamp)
            {
                s3Uri += "/" + DateTime.Now.ToString("yyyyMMddHHmmss");
            }

            var pyPath = Path.GetFullPath("Packages/com.framesynthesis.webgl-buildtools/pyscripts/deploy_to_amazon_s3.py");
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"{pyPath} ./{BuildPath} {s3Uri}"
            });
            process?.WaitForExit();

            Process.Start(s3Uri.Replace("s3://", "https://"));
        }
    }
}
