using UnityEditor;

namespace FrameSynthesis.WebGLBuildAndDeploy.Editor
{
    [FilePath("WebGLBuildAnsDeployPreferences.asset", FilePathAttribute.Location.PreferencesFolder)]
    public class Preferences : ScriptableSingleton<Preferences>
    {
        public string awsCliPath = "/Library/Frameworks/Python.framework/Versions/3.12/bin";

        public void Modify()
        {
            Save(true);
        }
    }
}
