using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

/// <summary>
/// Copies steam_appid.txt from the project root next to the built game,
/// so a build started outside Steam still knows its App ID.
/// </summary>
public static class SteamAppIdPostprocess
{
    const string FileName = "steam_appid.txt";

    [PostProcessBuild]
    static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.StandaloneWindows && target != BuildTarget.StandaloneWindows64 &&
            target != BuildTarget.StandaloneLinux64 && target != BuildTarget.StandaloneOSX)
            return;

        var source = Path.Combine(Directory.GetCurrentDirectory(), FileName);
        if (!File.Exists(source))
        {
            Debug.LogWarning($"[Steam] {FileName} not found in the project root, skipping copy.");
            return;
        }

        // pathToBuiltProject is the .exe / binary / .app; the file goes in the folder next to it.
        var buildDir = Path.GetDirectoryName(pathToBuiltProject);
        File.Copy(source, Path.Combine(buildDir, FileName), true);
    }
}
