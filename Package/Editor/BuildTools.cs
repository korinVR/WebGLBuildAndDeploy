using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FrameSynthesis.WebGLBuildAndDeploy.Editor
{
    public class BuildTools
    {
        static string BasePath => Path.Combine("dist", "WebGL");
        static string PackageName => PlayerSettings.applicationIdentifier.Split('.').Last();
        
        [MenuItem("WebGL/Build (Development) &b", priority = 1)]
        public static void BuildDevelopment()
        {
            var path = Path.Combine(BasePath, "Development", PackageName); 
            Build(path, WebGLCompressionFormat.Disabled, BuildOptions.Development);
        }

        // Requires Browsersync installed (https://browsersync.io/)
        [MenuItem("WebGL/Start Local HTTPS Server", priority = 2)]
        public static void StartLocalTestServer()
        {
            Process.Start(new ProcessStartInfo
            {
                WorkingDirectory = Path.Combine(BasePath, "Development", PackageName),
                FileName = "browser-sync",
                Arguments = "start --server --watch --https --port=1000"
            });
        }

        [MenuItem("WebGL/Open Build Folder", priority = 3)]
        public static void OpenBuildFolder()
        {
            Process.Start(Path.Combine(Application.dataPath, "..", BasePath));
        }

        [MenuItem("WebGL/Build and Deploy to Amazon S3 (Gzip)", priority = 101)]
        public static void BuildAndDeployWithGzipCompressed()
        {
            var path = Path.Combine(BasePath, "Gzip", PackageName);
            Build(path, WebGLCompressionFormat.Gzip, BuildOptions.None);
            DeployToS3(path);
        }

        [MenuItem("WebGL/Build and Deploy to Amazon S3 (Brotli)", priority = 102)]
        public static void BuildAndDeployWithBrotliCompressed()
        {
            var path = Path.Combine(BasePath, "Brotli", PackageName);
            Build(path, WebGLCompressionFormat.Brotli, BuildOptions.None);
            DeployToS3(path);
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

            var wasmFile = new FileInfo(Path.Combine(executablePath, "Build", $"{PackageName}.wasm{extension}"));
            var wasmSize = wasmFile.Length;

            var dataFile = new FileInfo(Path.Combine(executablePath, "Build", $"{PackageName}.data{extension}"));
            var dataSize = dataFile.Length;

            Debug.Log($"WASM size: {wasmSize / 1024} KB");
            Debug.Log($"Data size: {dataSize / 1024} KB");
            
            Unity.BuildReportInspector.BuildReportInspector.OpenLastBuild();
        }
        
        public static void DeployToS3(string path)
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

            var profile = deploySettings.Profile;
            var region = deploySettings.Region;
            var s3Uri = deploySettings.S3URI;

            if (string.IsNullOrEmpty(profile))
            {
                profile = "default";
            }
            
            if (string.IsNullOrEmpty(region))
            {
                Debug.LogError("DeploySettings: Region should be set.");
                return;
            }
            if (!s3Uri.StartsWith("s3://"))
            {
                Debug.LogError("DeploySettings: S3URI should start with S3://.");
                return;
            }

            if (deploySettings.AddTimestamp)
            {
                s3Uri += "/" + DateTime.Now.ToString("yyyyMMddHHmmss");
            }

            var pyPath = Path.GetFullPath("Packages/com.framesynthesis.webgl-build-and-deploy/pyscripts/deploy_to_amazon_s3.py");
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"{pyPath} {profile} {region} {path} {s3Uri}"
            });
            process?.WaitForExit();

            Process.Start(s3Uri.Replace("s3://", "https://"));
        }
    }
}
