using Scorewriter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScorewriterEditor
{
    public static class ScorewriterSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Scorewriter.unity";

        [MenuItem("Tools/Scorewriter/Rebuild Scorewriter Scene")]
        public static void RebuildScorewriterScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.062f, 0.075f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.tag = "MainCamera";

            GameObject appObject = new GameObject("Scorewriter App");
            appObject.AddComponent<ScorewriterCriAudioPlayer>();
            appObject.AddComponent<ScorewriterApp>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.Refresh();
            Selection.activeGameObject = appObject;
            Debug.Log($"[ScorewriterSceneBuilder] Rebuilt {ScenePath}");
        }
    }
}
