using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Command-line iOS build: exports the Xcode project to Builds/iOS.
/// Unity -batchmode -quit -projectPath . -buildTarget iOS -executeMethod BuildIOS.Build
/// Set the APPLE_TEAM_ID environment variable to sign with your Apple team.
/// </summary>
public static class BuildIOS
{
    [MenuItem("Tools/Build iOS (Xcode project)")]
    public static void Build()
    {
        string team = System.Environment.GetEnvironmentVariable("APPLE_TEAM_ID");
        if (!string.IsNullOrEmpty(team))
        {
            PlayerSettings.iOS.appleDeveloperTeamID = team;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
        }

        var options = new BuildPlayerOptions
        {
            scenes = System.Array.ConvertAll(
                System.Array.FindAll(EditorBuildSettings.scenes, s => s.enabled), s => s.path),
            locationPathName = "Builds/iOS",
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("BuildIOS result: " + report.summary.result);
        if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
