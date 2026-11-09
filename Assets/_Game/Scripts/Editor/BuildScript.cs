using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ShiftShift.EditorTools
{
    /// <summary>
    /// Menu + command-line builds. Batch mode example (Windows, adjust the Unity path):
    ///   Unity.exe -batchmode -quit -projectPath . -executeMethod ShiftShift.EditorTools.BuildScript.BuildWebGL -logFile -
    /// Exits with code 1 on failure in batch mode. Output goes to Builds/ (git-ignored).
    /// </summary>
    public static class BuildScript
    {
        private const string WebGLDir = "Builds/WebGL";
        private const string WindowsExe = "Builds/Windows/ShiftShift.exe";

        [MenuItem("Shift+Shift/Build WebGL")]
        public static void BuildWebGL()
        {
            // itch.io does not send Content-Encoding headers for Unity's .br files,
            // so the decompression fallback is required for Brotli to load there.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            Build(BuildTarget.WebGL, WebGLDir);
        }

        [MenuItem("Shift+Shift/Build Windows")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, WindowsExe);

        private static void Build(BuildTarget target, string location)
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Fail("No enabled scenes in File > Build Profiles / Build Settings.");
                return;
            }

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = location,
                target = target,
                options = BuildOptions.None,
            });

            BuildSummary s = report.summary;
            if (s.result != BuildResult.Succeeded)
            {
                Fail($"{target} build {s.result} with {s.totalErrors} error(s).");
                return;
            }

            string dir = Directory.Exists(location) ? location : Path.GetDirectoryName(location);
            long bytes = Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);
            Debug.Log($"[Build] {target} OK in {s.totalTime:mm\\:ss} -> {location}, {bytes / (1024.0 * 1024.0):F1} MB on disk" +
                      (target == BuildTarget.WebGL ? " (plan budget: under 20 MB)" : ""));
        }

        private static void Fail(string message)
        {
            Debug.LogError("[Build] " + message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
