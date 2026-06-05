using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Scorewriter.Editor
{
    public sealed class ScorewriterCriStreamingAssetsBuilder : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        [MenuItem("Scorewriter/同步 CRI 音频到 StreamingAssets")]
        public static void SyncCriAudioFiles()
        {
            string sourceRoot = Path.Combine(Application.dataPath, "CRI/Public");
            string targetRoot = Path.Combine(Application.streamingAssetsPath, "CRI/Public");

            if (!Directory.Exists(sourceRoot))
            {
                Debug.LogWarning($"[Scorewriter] CRI source folder not found: {sourceRoot}");
                return;
            }

            CopyDirectory(sourceRoot, targetRoot);
            AssetDatabase.Refresh();
            Debug.Log($"[Scorewriter] Synced CRI audio files to StreamingAssets: {targetRoot}");
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            SyncCriAudioFiles();
        }

        private static void CopyDirectory(string sourceRoot, string targetRoot)
        {
            Directory.CreateDirectory(targetRoot);

            foreach (string sourceFile in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                if (sourceFile.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                string relativePath = sourceFile.Substring(sourceRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string targetFile = Path.Combine(targetRoot, relativePath);
                string targetDirectory = Path.GetDirectoryName(targetFile);
                if (!string.IsNullOrEmpty(targetDirectory))
                    Directory.CreateDirectory(targetDirectory);

                File.Copy(sourceFile, targetFile, true);
            }
        }
    }
}
