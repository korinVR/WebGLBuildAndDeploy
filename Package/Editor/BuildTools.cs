using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FrameSynthesis.WebGLToolkit.Editor
{
    public class BuildTools
    {
        const string BuildPath = "dist\\WebGL";

        [MenuItem("WebGL/Build (Development) &b", priority = 1)]
        public static void BuildDevelopment()
        {
            Build(BuildPath, WebGLCompressionFormat.Disabled, BuildOptions.Development);
        }

        [MenuItem("WebGL/Build (Gzip)", priority = 2)]
        public static void BuildGzip()
        {
            Build(BuildPath, WebGLCompressionFormat.Gzip, BuildOptions.None);
        }

        [MenuItem("WebGL/Build (Brotli)", priority = 3)]
        public static void BuildBrotli()
        {
            Build(BuildPath, WebGLCompressionFormat.Brotli, BuildOptions.None);
        }

        [MenuItem("WebGL/Open Build Folder", priority = 4)]
        public static void OpenBuildFolder()
        {
            Process.Start(Path.Combine(Application.dataPath, "..", BuildPath));
        }

        // Requires Browsersync installed (https://browsersync.io/)
        [MenuItem("WebGL/Start Local HTTPS Server", priority = 5)]
        public static void StartLocalTestServer()
        {
            Process.Start(new ProcessStartInfo
            {
                WorkingDirectory = BuildPath,
                FileName = "browser-sync",
                Arguments = "start --server --watch --https --port=1000"
            });
        }

        [MenuItem("WebGL/Deploy to Amazon S3")]
        public static void DeployToS3()
        {
            var pyPath = Path.GetFullPath("Packages/com.framesynthesis.webgl-buildtools/pyscripts/deploy_to_amazon_s3.py");
            
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "python",
                Arguments = pyPath
            });
            process?.WaitForExit();
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
    }
}