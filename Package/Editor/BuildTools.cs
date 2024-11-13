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
        static string BasePath => Path.Combine("Builds", "WebGL");
        static string AppName
        {
            get
            {
                var items = PlayerSettings.applicationIdentifier.Split('.');
                var appName = items.Last();
                if (appName == "development")
                {
                    appName = items[^2];
                }
                return appName;
            }
        }
        
        [MenuItem("WebGL/Build (Development) &b", priority = 1)]
        public static void BuildDevelopment()
        {
            var path = Path.Combine(BasePath, "Development", AppName); 
            Build(path, WebGLCompressionFormat.Disabled, BuildOptions.Development);
        }

        // Requires Browsersync installed (https://browsersync.io/)
        [MenuItem("WebGL/Start Local HTTPS Server", priority = 2)]
        public static void StartLocalTestServer()
        {
            var workingDirectory = Path.Combine(Application.dataPath, "..", BasePath, "Development", AppName);
            ProcessStarter.RunBrowsersync(workingDirectory);
        }

        [MenuItem("WebGL/Open Build Folder", priority = 3)]
        public static void OpenBuildFolder()
        {
            Process.Start(Path.Combine(Application.dataPath, "..", BasePath));
        }

        [MenuItem("WebGL/Build (Gzip)", priority = 101)]
        public static void BuildWithGzipCompressed()
        {
            var path = Path.Combine(BasePath, "Gzip", AppName);
            Build(path, WebGLCompressionFormat.Gzip, BuildOptions.None);
        }

        [MenuItem("WebGL/Build (Brotli)", priority = 102)]
        public static void BuildWithBrotliCompressed()
        {
            var path = Path.Combine(BasePath, "Brotli", AppName);
            Build(path, WebGLCompressionFormat.Brotli, BuildOptions.None);
        }

        [MenuItem("WebGL/Build and Deploy (Gzip)", priority = 201)]
        public static void BuildAndDeployWithGzipCompressed()
        {
            var path = Path.Combine(BasePath, "Gzip", AppName);
            Build(path, WebGLCompressionFormat.Gzip, BuildOptions.None);
            DeployToS3(path);
        }

        [MenuItem("WebGL/Build and Deploy (Brotli)", priority = 202)]
        public static void BuildAndDeployWithBrotliCompressed()
        {
            var path = Path.Combine(BasePath, "Brotli", AppName);
            Build(path, WebGLCompressionFormat.Brotli, BuildOptions.None);
            DeployToS3(path);
        }

        [MenuItem("WebGL/Build and Deploy and Open (Gzip)", priority = 301)]
        public static void BuildAndDeployAndOpenWithGzipCompressed()
        {
            var path = Path.Combine(BasePath, "Gzip", AppName);
            Build(path, WebGLCompressionFormat.Gzip, BuildOptions.None);
            var url = DeployToS3(path);
            Process.Start(url);
        }

        [MenuItem("WebGL/Build and Deploy and Open (Brotli)", priority = 302)]
        public static void BuildAndDeployAndOpenWithBrotliCompressed()
        {
            var path = Path.Combine(BasePath, "Brotli", AppName);
            Build(path, WebGLCompressionFormat.Brotli, BuildOptions.None);
            var url = DeployToS3(path);
            Process.Start(url);
        }

        static string[] ScenePaths => EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path).ToArray();

        static void Build(string executablePath, WebGLCompressionFormat compressionFormat, BuildOptions buildOptions)
        {
            var prevCompressionFormat = PlayerSettings.WebGL.compressionFormat;
            var prevDecompressionFallback = PlayerSettings.WebGL.decompressionFallback;
            
#if UNITY_2022_2_OR_NEWER
            // Runtime Speed and Disk Size is not usable at the moment due to the VERY LONG build time.
            SetCodeOptimization(CodeOptimization.BuildTimes);
#endif

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

            var wasmFile = new FileInfo(Path.Combine(executablePath, "Build", $"{AppName}.wasm{extension}"));
            var wasmSize = wasmFile.Length;

            var dataFile = new FileInfo(Path.Combine(executablePath, "Build", $"{AppName}.data{extension}"));
            var dataSize = dataFile.Length;

            var compressionFormatString = compressionFormat switch
            {
                WebGLCompressionFormat.Disabled => "uncompressed",
                WebGLCompressionFormat.Gzip => "Gzip",
                WebGLCompressionFormat.Brotli => "Brotli",
                _ => throw new ArgumentOutOfRangeException()
            };
            
            var buildSizeLog = $"Build Size: wasm {wasmSize / 1024} KB / data {dataSize / 1024} KB ({compressionFormatString})";
            Debug.Log(buildSizeLog);
            
            // Output build size for for GitHub Actions
            File.WriteAllText(Path.Combine(BasePath, "BuildSize.txt"), buildSizeLog);
        }
        
        public static string DeployToS3(string buildPath)
        {
            Debug.Log("Uploading...");
            
            DeploySettings deploySettings;
            
            try
            {
                var guid = AssetDatabase.FindAssets("t:" + typeof(DeploySettings).FullName).Single();
                deploySettings = AssetDatabase.LoadAssetAtPath<DeploySettings>(AssetDatabase.GUIDToAssetPath(guid));
            }
            catch (InvalidOperationException)
            {
                throw new Exception("DeploySettings object is required to deploy.");
            }

            var profile = deploySettings.Profile;
            var region = deploySettings.Region;
            var s3Uri = deploySettings.S3URI;
            var url = deploySettings.URL;

            if (string.IsNullOrEmpty(profile))
            {
                profile = "default";
            }
            
            if (string.IsNullOrEmpty(region))
            {
                throw new Exception("DeploySettings: Region should be set.");
            }
            if (!s3Uri.StartsWith("s3://"))
            {
                throw new Exception("DeploySettings: S3URI should start with S3://.");
            }

            if (deploySettings.AddTimestamp)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                s3Uri += "/" + timestamp;
                url += "/" + timestamp;
            }

            var pythonScriptPath = Path.GetFullPath("Packages/com.framesynthesis.webgl-build-and-deploy/pyscripts/deploy_to_amazon_s3.py");
            var arguments = $"{profile} {region} {buildPath} {s3Uri}";
            var workingDirectory =　Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            ProcessStarter.CallPython(pythonScriptPath, workingDirectory, arguments);

            // Output URL text for GitHub Actions
            File.WriteAllText(Path.Combine(BasePath, "URL.txt"), url);
            
            Debug.Log($"Uploaded to {url}");

            return url;
        }

#if UNITY_2022_2_OR_NEWER
        enum CodeOptimization
        {
            BuildTimes,
            RuntimeSpeed,
            DiskSize,
        }

        static void SetCodeOptimization(CodeOptimization codeOptimization)
        {
            EditorUserBuildSettings.SetPlatformSettings(BuildPipeline.GetBuildTargetName(BuildTarget.WebGL), "CodeOptimization", codeOptimization.ToString());
        }
#endif
    }
}
