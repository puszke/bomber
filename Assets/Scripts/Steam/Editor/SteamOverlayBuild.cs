using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

/// <summary>
/// The Steam overlay can't draw inside the Unity Editor, and on macOS it only appears when Steam
/// injects gameoverlayrenderer.dylib at launch. This menu builds the game and starts it the same way Steam does.
/// </summary>
public static class SteamOverlayBuild
{
    const string BuildPath = "Builds/macOS/bomber.app";

    static string OverlayDylib => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
        "Library/Application Support/Steam/Steam.AppBundle/Steam/Contents/MacOS/gameoverlayrenderer.dylib");

    [MenuItem("Steam/Build and Run with Overlay (macOS)")]
    static void BuildAndRun()
    {
        if (!File.Exists(OverlayDylib))
        {
            EditorUtility.DisplayDialog("Steam", $"Steam overlay library not found:\n{OverlayDylib}\n\nIs Steam installed?", "OK");
            return;
        }

        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = BuildPath,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.Development,
        };
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[Steam] Build failed: {report.summary.result}");
            return;
        }

        Run();
    }

    [MenuItem("Steam/Run Last Build with Overlay (macOS)")]
    static void Run()
    {
        var macOsDir = Path.Combine(BuildPath, "Contents/MacOS");
        var exe = Directory.Exists(macOsDir) ? Directory.GetFiles(macOsDir).FirstOrDefault() : null;
        if (exe == null)
        {
            Debug.LogError($"[Steam] No build at {BuildPath}. Use Steam > Build and Run with Overlay first.");
            return;
        }

        var appId = File.Exists("steam_appid.txt") ? File.ReadAllText("steam_appid.txt").Trim() : "480";
        var psi = new ProcessStartInfo(Path.GetFullPath(exe))
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetFullPath(Path.GetDirectoryName(BuildPath)),
        };
        psi.EnvironmentVariables["DYLD_INSERT_LIBRARIES"] = OverlayDylib;
        psi.EnvironmentVariables["SteamAppId"] = appId;
        psi.EnvironmentVariables["SteamGameId"] = appId;
        Process.Start(psi);
        Debug.Log($"[Steam] Started {exe} with the Steam overlay (App ID {appId}). Press Shift+Tab in the game.");
    }
}
