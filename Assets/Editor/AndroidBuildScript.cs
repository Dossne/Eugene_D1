using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Hakaton.Lemmings.Editor
{
    public static class AndroidBuildScript
    {
        public static void BuildApk()
        {
            const string outputPath = "Builds/Android/Eugene_D1.apk";

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? "Builds");

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                throw new System.Exception("Failed to switch active build target to Android.");
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new System.Exception($"Android build failed: {report.summary.result}");
            }
        }
    }
}

