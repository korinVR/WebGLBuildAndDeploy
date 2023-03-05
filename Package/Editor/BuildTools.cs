﻿using System;
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

        [MenuItem("WebGL/Build and Deploy (Gzip)", priority = 101)]
        public static void BuildAndDeployWithGzipCompressed()
        {
            var path = Path.Combine(BasePath, "Gzip", PackageName);
            Build(path, WebGLCompressionFormat.Gzip, BuildOptions.None);
            DeployToS3(path);
        }

        [MenuItem("WebGL/Build and Deploy (Brotli)", priority = 102)]
        public static void BuildAndDeployWithBrotliCompressed()
        {
            var path = Path.Combine(BasePath, "Brotli", PackageName);
            Build(path, WebGLCompressionFormat.Brotli, BuildOptions.None);
            DeployToS3(path);
        }

        [MenuItem("WebGL/Build and Deploy and Open (Gzip)", priority = 201)]
        public static void BuildAndDeployAndOpenWithGzipCompressed()
        {
            var path = Path.Combine(BasePath, "Gzip", PackageName);
            Build(path, WebGLCompressionFormat.Gzip, BuildOptions.None);
            var url = DeployToS3(path);
            Process.Start(url);
        }

        [MenuItem("WebGL/Build and Deploy and Open (Brotli)", priority = 202)]
        public static void BuildAndDeployAndOpenWithBrotliCompressed()
        {
            var path = Path.Combine(BasePath, "Brotli", PackageName);
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
        
        public static string DeployToS3(string buildPath)
        {
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

            var pyPath = Path.GetFullPath("Packages/com.framesynthesis.webgl-build-and-deploy/pyscripts/deploy_to_amazon_s3.py");
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"{pyPath} {profile} {region} {buildPath} {s3Uri}"
            });
            process?.WaitForExit();

            // Output URL text for GitHub Actions
            File.WriteAllText(Path.Combine(BasePath, "URL.txt"), url);

            return url;
        }
    }
}
