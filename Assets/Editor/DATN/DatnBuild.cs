using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
///     Build ban Windows 64-bit cho do an (chay qua -executeMethod).
///     Chi can scene Initialization trong Build Settings — cac scene khac
///     nam trong Addressables (da bat "Build Addressables on Player Build").
/// </summary>
namespace DATN
{
    public static class DatnBuild
    {
        private const string OutputDir = "F:/Do_An_KT/Build/ShadowgroveSurvivors";
        private const string ExeName = "ShadowgroveSurvivors.exe";

        public static void BuildWindows64()
        {
            try
            {
                string[] scenes = EditorBuildSettings.scenes
                    .Where(s => s.enabled)
                    .Select(s => s.path)
                    .ToArray();

                Debug.Log($"[Build] Scenes trong Build Settings: {string.Join(", ", scenes)}");

                Directory.CreateDirectory(OutputDir);

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = Path.Combine(OutputDir, ExeName),
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;

                Debug.Log($"[Build] Ket qua: {summary.result} | size: {summary.totalSize / (1024 * 1024)} MB | " +
                          $"errors: {summary.totalErrors} | warnings: {summary.totalWarnings} | " +
                          $"thoi luong: {summary.totalTime.TotalSeconds:F0}s");

                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Build] LOI: {ex}");
                EditorApplication.Exit(1);
            }
        }
    }
}
