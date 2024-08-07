using System;
using System.Diagnostics;

namespace FrameSynthesis.WebGLBuildAndDeploy.Editor
{
    public static class ProcessStarter
    {
        public static void RunBrowsersync(string workingDirectory)
        {
#if UNITY_EDITOR_WIN
            Process.Start(new ProcessStartInfo
            {
                FileName = "browser-sync",
                Arguments = $"start --server --watch --https --port=1000",
                WorkingDirectory = workingDirectory
            });
#endif
#if UNITY_EDITOR_OSX
            var command = $"cd {workingDirectory} && browser-sync start --server --watch --https --port=1000";
            var appleScriptCommand = $"tell application \"Terminal\" to do script \"{command}\"";
            
            Process.Start(new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = $"-e '{appleScriptCommand}'",
                UseShellExecute = true
            });
#endif
        }
        
        public static void CallPython(string pythonScriptPath, string workingDirectory, string arguments)
        {
#if UNITY_EDITOR_WIN
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"{pythonScriptPath} {arguments}",
                WorkingDirectory = workingDirectory
            });
            process?.WaitForExit();
#endif
#if UNITY_EDITOR_OSX
            // Requires AWS CLI path to call from Python on macOS
            var environmentPath = Preferences.instance.awsCliPath + ":" + Environment.GetEnvironmentVariable("PATH");

            UnityEngine.Debug.Log($"{pythonScriptPath} {arguments}");
            
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/python3",
                Arguments = $"{pythonScriptPath} {arguments}",
                UseShellExecute = false,
                WorkingDirectory = workingDirectory,
                EnvironmentVariables = { ["PATH"] = environmentPath }
            };

            var process = new Process();
            process.StartInfo = processStartInfo;
            process.Start();

            process.WaitForExit();
#endif
        }
    }
}
