using UnityEngine;

namespace FrameSynthesis.WebGLBuildAndDeploy.Editor
{
    [CreateAssetMenu(menuName = "WebGL Build and Deploy/Deploy Settings", fileName = "WebGLDeploySettings")]
    public class DeploySettings : ScriptableObject
    {
        public string Region;
        public string S3URI;
        public bool AddTimestamp;
    }
}
