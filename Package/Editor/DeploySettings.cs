﻿using UnityEngine;

namespace FrameSynthesis.WebGLBuildAndDeploy.Editor
{
    [CreateAssetMenu(menuName = "WebGL Build Tools/Deploy Settings", fileName = "WebGLDeploySettings")]
    public class DeploySettings : ScriptableObject
    {
        public string Region;
        public string S3URI;
        public bool AddTimestamp;
    }
}
