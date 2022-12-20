using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Text.RegularExpressions;

/*
  Trivial build script, used to build on the command-line.

  See
  https://docs.unity3d.com/Manual/CommandLineArguments.html
  https://docs.unity3d.com/ScriptReference/BuildPipeline.BuildPlayer.html
*/
public class Build
{
    const string AppName = "neuropace";

    // Cross-platform stuff ------------------------------------------------------------------

    private static void BuildCommon(BuildTarget target, string locationPathName, bool debug)
    {
        // Regular build stuff

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity" };
        buildPlayerOptions.locationPathName = locationPathName;
        buildPlayerOptions.target = target;
        buildPlayerOptions.options = BuildOptions.CompressWithLz4HC;

        if (debug)
        {
            // enable logs
            buildPlayerOptions.options |= BuildOptions.Development;
        }

        //PlayerSettings.SplashScreen.showUnityLogo = false;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        Debug.Log("Build result: " + summary.result.ToString());
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
        }
        else
        {
            throw new System.Exception("Build failed"); // make the cag-build-xxx script fail
        }
    }

    // Windows ------------------------------------------------------------------------

    private static void BuildStandaloneWindows64(bool debug)
    {
        BuildCommon(BuildTarget.StandaloneWindows64, "build/StandaloneWindows64/" + AppName + ".exe", debug);
    }

    [MenuItem("Build/Build Standalone Windows 64 (Debug)")]
    private static void BuildStandaloneWindows64Debug()
    {
        BuildStandaloneWindows64(true);
    }

    [MenuItem("Build/Build Standalone Windows 64 (Release)")]
    private static void BuildStandaloneWindows64Release()
    {
        BuildStandaloneWindows64(false);
    }

    // Linux ---------------------------------------------------------------------------------

    private static void BuildStandaloneLinux64(bool debug)
    {
        BuildCommon(BuildTarget.StandaloneLinux64, "build/StandaloneLinux64/" + AppName, debug);
    }

    [MenuItem("Build/Build Standalone Linux 64 (Debug)")]
    private static void BuildStandaloneLinux64Debug()
    {
        BuildStandaloneLinux64(true);
    }

    [MenuItem("Build/Build Standalone Linux 64 (Release)")]
    private static void BuildStandaloneLinux64Release()
    {
        BuildStandaloneLinux64(false);
    }

    // Web -------------------------------------------------------------------------------------

    private static void BuildWebGL(bool debug)
    {
        BuildCommon(BuildTarget.WebGL, "build/WebGL/" + AppName, debug);
    }

    [MenuItem("Build/Build WebGL (debug)")]
    private static void BuildWebGLDebug()
    {
        BuildWebGL(true);
    }

    [MenuItem("Build/Build WebGL (release)")]
    private static void BuildWebGLRelease()
    {
        BuildWebGL(false);
    }
}
