using UnityEditor;

namespace FrameSynthesis.WebGLBuildAndDeploy.Editor
{
    static class MySettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateMySettingsProvider()
        {
            var provider = new SettingsProvider("Preferences/WebGL Build and Deploy", SettingsScope.User)
            {
                guiHandler = (searchContext) =>
                {
                    var settings = Preferences.instance;
                    settings.awsCliPath = EditorGUILayout.TextField("AWS CLI Path", settings.awsCliPath);
                    settings.Modify();
                },
            };
            return provider;
        }
    }
}
