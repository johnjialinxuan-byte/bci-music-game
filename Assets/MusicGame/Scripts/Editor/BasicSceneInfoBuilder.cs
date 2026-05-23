#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using MusicGame.Audio;
using MusicGame.Core;
using MusicGame.Gameplay;
using MusicGame.Input;
using MusicGame.Managers;
using MusicGame.Notes;
using MusicGame.Scenes;
using MusicGame.UI;

namespace MusicGame.Editor
{
    public static class BasicSceneInfoBuilder
    {
        private const string ScenePath = "Assets/MusicGame/Scenes";
        private const string PrefabPath = "Assets/MusicGame/Prefabs";
        private const string GeneratedPath = "Assets/MusicGame/Generated";

        [MenuItem("MusicGame/Generate Basic Scene Info")]
        public static void Generate()
        {
            EnsureFolder("Assets/MusicGame", "Scenes");
            EnsureFolder("Assets/MusicGame", "Prefabs");
            EnsureFolder("Assets/MusicGame", "Generated");
            EnsureFolder("Assets/MusicGame", "Resources");
            EnsureFolder("Assets/MusicGame/Resources", "Songs");
            EnsureFolder("Assets/MusicGame/Resources", "Charts");
            EnsureFolder(GeneratedPath, "Materials");

            Material cyan = CreateMaterial("BCI_Cyan", new Color(0.1f, 0.95f, 0.9f, 1f));
            Material white = CreateMaterial("BCI_White", Color.white);
            Material red = CreateMaterial("BCI_Red", new Color(1f, 0.18f, 0.18f, 1f));
            Material blue = CreateMaterial("BCI_Blue", new Color(0.2f, 0.45f, 1f, 1f));
            Material plane = CreateMaterial("JudgePlane_Transparent", new Color(0.1f, 0.9f, 1f, 0.22f), true);

            HoldNote holdPrefab = CreateHoldNotePrefab(cyan);
            FlickNote flickPrefab = CreateFlickNotePrefab(cyan);
            GenerateDefaultSongsAndCharts();

            BuildMainMenu();
            BuildSongSelect();
            BuildSettings();
            BuildAbout();
            BuildGameplay(holdPrefab, flickPrefab, plane);
            BuildResult();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenePath}/MainMenu.unity", true),
                new EditorBuildSettingsScene($"{ScenePath}/SongSelect.unity", true),
                new EditorBuildSettingsScene($"{ScenePath}/Settings.unity", true),
                new EditorBuildSettingsScene($"{ScenePath}/About.unity", true),
                new EditorBuildSettingsScene($"{ScenePath}/Gameplay.unity", true),
                new EditorBuildSettingsScene($"{ScenePath}/Result.unity", true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BasicSceneInfoBuilder] Basic CRIWARE-driven music game scenes generated.");
        }

        private static HoldNote CreateHoldNotePrefab(Material material)
        {
            GameObject root = new GameObject("HoldNote_Basic");
            HoldNote note = root.AddComponent<HoldNote>();

            Transform head = CreateCube("HoldHead", root.transform, material, new Vector3(0.7f, 0.18f, 0.08f)).transform;
            Transform tail = CreateCube("HoldTail", root.transform, material, new Vector3(0.45f, 0.15f, 0.08f)).transform;

            LineRenderer line = root.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.positionCount = 2;
            line.startWidth = 0.12f;
            line.endWidth = 0.12f;
            line.useWorldSpace = true;

            SetField(note, "visualTransform", head);
            SetField(note, "tailTransform", tail);
            SetField(note, "connectionLine", line);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabPath}/HoldNote_Basic.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<HoldNote>();
        }

        private static FlickNote CreateFlickNotePrefab(Material material)
        {
            GameObject root = new GameObject("FlickNote_Basic");
            FlickNote note = root.AddComponent<FlickNote>();

            GameObject arrow = new GameObject("ArrowTriangle");
            arrow.transform.SetParent(root.transform, false);
            arrow.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            MeshFilter filter = arrow.AddComponent<MeshFilter>();
            filter.sharedMesh = CreateTriangleMesh();
            MeshRenderer renderer = arrow.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            SetField(note, "visualTransform", arrow.transform);
            SetField(note, "arrowTransform", arrow.transform);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabPath}/FlickNote_Basic.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<FlickNote>();
        }

        private static void BuildMainMenu()
        {
            Scene scene = NewScene("MainMenu");
            ConfigureCamera(Color.black, false);

            GameObject managers = new GameObject("PersistentManagers");
            managers.AddComponent<GameStateManager>();
            managers.AddComponent<ScoreManager>();
            managers.AddComponent<CriAudioManager>();
            managers.AddComponent<AudioManager>();
            managers.AddComponent<UIManager>();

            GameObject canvas = CreateCanvas("MainMenuCanvas");
            CreateText("Title", canvas.transform, "BCI MUSIC GAME", 46, new Vector2(0, 120), new Vector2(720, 80));
            Button start = CreateButton("StartButton", canvas.transform, "Start", new Vector2(0, 35));
            Button settings = CreateButton("SettingsButton", canvas.transform, "Settings", new Vector2(0, -35));
            Button about = CreateButton("AboutButton", canvas.transform, "About", new Vector2(0, -105));
            Button quit = CreateButton("QuitButton", canvas.transform, "Quit", new Vector2(0, -175));

            MainMenuController controller = new GameObject("MainMenuController").AddComponent<MainMenuController>();
            SetField(controller, "startButton", start);
            SetField(controller, "settingsButton", settings);
            SetField(controller, "aboutButton", about);
            SetField(controller, "quitButton", quit);
            CreateEventSystem();
            Save(scene, "MainMenu");
        }

        private static void BuildSongSelect()
        {
            Scene scene = NewScene("SongSelect");
            ConfigureCamera(new Color(0.02f, 0.02f, 0.025f), false);
            GameObject canvas = CreateCanvas("SongSelectCanvas");

            CreateText("SongSelectTitle", canvas.transform, "Select Song", 42, new Vector2(0, 250), new Vector2(720, 70));
            Button back = CreateButton("BackButton", canvas.transform, "Back", new Vector2(-640, 310), new Vector2(150, 56));
            Button preview = CreateButton("PreviewButton", canvas.transform, "Preview", new Vector2(430, -170), new Vector2(190, 58));
            Button easy = CreateButton("EasyButton", canvas.transform, "Easy", new Vector2(250, -305), new Vector2(150, 58));
            Button normal = CreateButton("NormalButton", canvas.transform, "Normal", new Vector2(430, -305), new Vector2(150, 58));
            Button hard = CreateButton("HardButton", canvas.transform, "Hard", new Vector2(610, -305), new Vector2(150, 58));

            GameObject list = CreatePanel("SongList", canvas.transform, new Color(1f, 1f, 1f, 0.08f), new Vector2(-360, -10), new Vector2(560, 480));
            GameObject content = new GameObject("Content");
            content.transform.SetParent(list.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = new Vector2(0, -18);
            contentRect.sizeDelta = new Vector2(-36, 430);
            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 14;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Image cover = CreatePanel("Cover", canvas.transform, new Color(1f, 1f, 1f, 0.18f), new Vector2(430, 85), new Vector2(320, 320)).GetComponent<Image>();
            Text title = CreateText("TitleText", canvas.transform, "Song Title", 32, new Vector2(430, -100), new Vector2(430, 54));
            Text artist = CreateText("ArtistText", canvas.transform, "Artist", 22, new Vector2(430, -145), new Vector2(430, 36));

            SongSelectController controller = new GameObject("SongSelectController").AddComponent<SongSelectController>();
            SetField(controller, "songListContent", content.transform);
            SetField(controller, "songItemPrefab", CreateSongItemPrefab());
            SetField(controller, "availableSongs", LoadGeneratedSongs());
            SetField(controller, "backButton", back);
            SetField(controller, "playPreviewButton", preview);
            SetField(controller, "coverImage", cover);
            SetField(controller, "titleText", title);
            SetField(controller, "artistText", artist);
            SetField(controller, "easyButton", easy);
            SetField(controller, "normalButton", normal);
            SetField(controller, "hardButton", hard);

            CreateEventSystem();
            Save(scene, "SongSelect");
        }

        private static void BuildGameplay(HoldNote holdPrefab, FlickNote flickPrefab, Material planeMaterial)
        {
            Scene scene = NewScene("Gameplay");
            ConfigureCamera(Color.black, true);

            GameObject judgePlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            judgePlane.name = "JudgePlane_Z0";
            judgePlane.transform.position = Vector3.zero;
            judgePlane.transform.localScale = new Vector3(8f, 5f, 1f);
            judgePlane.GetComponent<MeshRenderer>().sharedMaterial = planeMaterial;
            judgePlane.AddComponent<JudgePlane>();

            GameObject chartManager = new GameObject("ChartManager");
            chartManager.AddComponent<ChartManager>();

            GameObject noteManager = new GameObject("NoteManager");
            NoteSpawner spawner = noteManager.AddComponent<NoteSpawner>();
            NoteManager noteMgr = noteManager.AddComponent<NoteManager>();
            SetField(spawner, "holdNotePrefab", holdPrefab);
            SetField(spawner, "flickNotePrefab", flickPrefab);
            SetField(noteMgr, "noteSpawner", spawner);

            GameObject inputManager = new GameObject("InputManager");
            inputManager.AddComponent<InputManager>();
            inputManager.AddComponent<DemoHoldProvider>();
            inputManager.AddComponent<DemoHeadMotionProvider>();
            inputManager.AddComponent<DemoInputProviderBinder>();

            new GameObject("JudgeManager").AddComponent<JudgeManager>();

            GameObject canvas = CreateCanvas("GameplayCanvas");
            Text score = CreateText("ScoreText", canvas.transform, "Score: 0", 22, new Vector2(-350, 230), new Vector2(240, 36));
            Text combo = CreateText("ComboText", canvas.transform, "Combo: 0", 22, new Vector2(-350, 190), new Vector2(240, 36));
            Text accuracy = CreateText("AccuracyText", canvas.transform, "Acc: 100%", 22, new Vector2(-350, 150), new Vector2(240, 36));
            Button pause = CreateButton("PauseButton", canvas.transform, "II", new Vector2(380, 230), new Vector2(60, 44));
            GameObject pausePanel = CreatePanel("PauseMenuPanel", canvas.transform, new Color(0f, 0f, 0f, 0.82f), Vector2.zero, new Vector2(900, 600));
            Button resume = CreateButton("ResumeButton", pausePanel.transform, "Resume", new Vector2(0, 35), new Vector2(180, 48));
            Button quit = CreateButton("QuitButton", pausePanel.transform, "Quit", new Vector2(0, -35), new Vector2(180, 48));
            pausePanel.SetActive(false);

            GameplayController controller = new GameObject("GameplayController").AddComponent<GameplayController>();
            SetField(controller, "scoreText", score);
            SetField(controller, "comboText", combo);
            SetField(controller, "accuracyText", accuracy);
            SetField(controller, "pauseButton", pause);
            SetField(controller, "pauseMenuPanel", pausePanel);
            SetField(controller, "resumeButton", resume);
            SetField(controller, "quitButton", quit);

            CreateEventSystem();
            Save(scene, "Gameplay");
        }

        private static void BuildSettings()
        {
            Scene scene = NewScene("Settings");
            ConfigureCamera(Color.black, false);
            GameObject canvas = CreateCanvas("SettingsCanvas");
            CreateText("Title", canvas.transform, "Settings", 38, new Vector2(0, 170), new Vector2(400, 60));
            Button back = CreateButton("BackButton", canvas.transform, "Back", new Vector2(0, -200));
            SettingsController controller = new GameObject("SettingsController").AddComponent<SettingsController>();
            SetField(controller, "backButton", back);
            CreateEventSystem();
            Save(scene, "Settings");
        }

        private static void BuildAbout()
        {
            Scene scene = NewScene("About");
            ConfigureCamera(Color.black, false);
            GameObject canvas = CreateCanvas("AboutCanvas");
            Text about = CreateText("AboutText", canvas.transform, "", 22, new Vector2(0, 40), new Vector2(680, 280));
            Button back = CreateButton("BackButton", canvas.transform, "Back", new Vector2(0, -210));
            AboutController controller = new GameObject("AboutController").AddComponent<AboutController>();
            SetField(controller, "aboutText", about);
            SetField(controller, "backButton", back);
            CreateEventSystem();
            Save(scene, "About");
        }

        private static void BuildResult()
        {
            Scene scene = NewScene("Result");
            ConfigureCamera(Color.black, false);
            GameObject canvas = CreateCanvas("ResultCanvas");
            Image cover = CreatePanel("CoverPerformanceArea", canvas.transform, new Color(1f, 1f, 1f, 0.18f), Vector2.zero, new Vector2(520, 520)).GetComponent<Image>();
            Text title = CreateText("ResultTitle", canvas.transform, "Result", 34, new Vector2(0, 220), new Vector2(600, 52));
            Text score = CreateText("ScoreText", canvas.transform, "Score: 0", 24, new Vector2(0, 110), new Vector2(420, 36));
            Text maxCombo = CreateText("MaxComboText", canvas.transform, "Max Combo: 0", 22, new Vector2(0, 70), new Vector2(420, 34));
            Text perfect = CreateText("PerfectText", canvas.transform, "Perfect: 0", 20, new Vector2(-130, 20), new Vector2(220, 32));
            Text good = CreateText("GoodText", canvas.transform, "Good: 0", 20, new Vector2(0, 20), new Vector2(220, 32));
            Text miss = CreateText("MissText", canvas.transform, "Miss: 0", 20, new Vector2(130, 20), new Vector2(220, 32));
            Text acc = CreateText("AccuracyText", canvas.transform, "Accuracy: 100%", 22, new Vector2(0, -35), new Vector2(420, 34));
            Text rank = CreateText("RankText", canvas.transform, "Rank: S", 44, new Vector2(0, -105), new Vector2(420, 70));
            Button retry = CreateButton("RetryButton", canvas.transform, "Retry", new Vector2(-90, -220), new Vector2(150, 48));
            Button back = CreateButton("BackButton", canvas.transform, "Song Select", new Vector2(90, -220), new Vector2(150, 48));

            ResultController controller = new GameObject("ResultController").AddComponent<ResultController>();
            SetField(controller, "resultTitleText", title);
            SetField(controller, "scoreText", score);
            SetField(controller, "maxComboText", maxCombo);
            SetField(controller, "perfectText", perfect);
            SetField(controller, "goodText", good);
            SetField(controller, "missText", miss);
            SetField(controller, "accuracyText", acc);
            SetField(controller, "rankText", rank);
            SetField(controller, "retryButton", retry);
            SetField(controller, "backButton", back);
            _ = cover;

            CreateEventSystem();
            Save(scene, "Result");
        }

        private static GameObject CreateSongItemPrefab()
        {
            GameObject item = new GameObject("SongItem_Basic");
            RectTransform rect = item.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 72);
            Image image = item.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.12f);
            item.AddComponent<Button>();
            Text label = CreateText("Label", item.transform, "Song", 24, Vector2.zero, rect.sizeDelta);
            label.alignment = TextAnchor.MiddleLeft;
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.offsetMin = new Vector2(24, 0);
            labelRect.offsetMax = new Vector2(-24, 0);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(item, $"{PrefabPath}/SongItem_Basic.prefab");
            Object.DestroyImmediate(item);
            return prefab;
        }

        private static void GenerateDefaultSongsAndCharts()
        {
            CreateSong("2077", "2077", "BCI Demo", "2077");
            CreateSong("jumping", "Jumping", "BCI Demo", "Jumping");
            CreateSong("kite", "Kite", "BCI Demo", "Kite");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateSong(string songId, string title, string artist, string coverName)
        {
            string assetPath = $"Assets/MusicGame/Resources/Songs/{title}.asset";
            SongData song = AssetDatabase.LoadAssetAtPath<SongData>(assetPath);
            if (song == null)
            {
                song = ScriptableObject.CreateInstance<SongData>();
                AssetDatabase.CreateAsset(song, assetPath);
            }

            song.songId = songId;
            song.title = title;
            song.artist = artist;
            song.bpm = 120f;
            song.previewStartTime = 8f;
            song.cueSheetName = "CueSheet_0";
            song.cueName = "cue_0000";
            song.coverImage = LoadCoverSprite($"Assets/Images/Covers/{coverName}.png");
            song.easyChartPath = $"Charts/{songId}_easy";
            song.normalChartPath = $"Charts/{songId}_normal";
            song.hardChartPath = $"Charts/{songId}_hard";

            EditorUtility.SetDirty(song);
            WriteChart(song.easyChartPath, Difficulty.Easy, 3, 8);
            WriteChart(song.normalChartPath, Difficulty.Normal, 5, 14);
            WriteChart(song.hardChartPath, Difficulty.Hard, 8, 22);
        }

        private static void WriteChart(string resourcePath, Difficulty difficulty, int level, int noteCount)
        {
            ChartData chart = ScriptableObject.CreateInstance<ChartData>();
            chart.difficulty = difficulty;
            chart.level = level;
            chart.notes = new List<NoteData>();

            float startTime = 1.5f;
            float spacing = difficulty == Difficulty.Hard ? 0.75f : difficulty == Difficulty.Normal ? 0.95f : 1.2f;
            for (int i = 0; i < noteCount; i++)
            {
                bool flick = i % 4 == 1;
                chart.notes.Add(new NoteData
                {
                    time = startTime + i * spacing,
                    x = Mathf.Sin(i * 0.7f) * 2.8f,
                    y = Mathf.Cos(i * 0.55f) * 1.8f,
                    z = 10f,
                    noteType = flick ? NoteType.Flick : NoteType.Hold,
                    duration = flick ? 0f : Mathf.Lerp(0.8f, 1.8f, (i % 3) / 2f),
                    threshold = 45 + (i % 4) * 10,
                    flickDirection = (FlickDirection)(i % 4),
                    approachTime = 2f
                });
            }

            string filePath = Path.Combine(Application.dataPath, "MusicGame", "Resources", resourcePath + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, JsonUtility.ToJson(chart, true));
        }

        private static List<SongData> LoadGeneratedSongs()
        {
            return new List<SongData>
            {
                AssetDatabase.LoadAssetAtPath<SongData>("Assets/MusicGame/Resources/Songs/2077.asset"),
                AssetDatabase.LoadAssetAtPath<SongData>("Assets/MusicGame/Resources/Songs/Jumping.asset"),
                AssetDatabase.LoadAssetAtPath<SongData>("Assets/MusicGame/Resources/Songs/Kite.asset")
            };
        }

        private static Sprite LoadCoverSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject CreateCube(string name, Transform parent, Material material, Vector3 scale)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localScale = scale;
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            return cube;
        }

        private static Mesh CreateTriangleMesh()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(0.55f, 0f, 0f),
                new Vector3(-0.35f, 0.42f, 0f),
                new Vector3(-0.35f, -0.42f, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Material CreateMaterial(string name, Color color, bool transparent = false)
        {
            string path = $"{GeneratedPath}/Materials/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.renderQueue = 3000;
            }
            return material;
        }

        private static Scene NewScene(string name)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = name;
            return scene;
        }

        private static void ConfigureCamera(Color background, bool perspective)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.orthographic = !perspective;
            cam.fieldOfView = 55f;
            cam.transform.position = new Vector3(0f, 0f, -8f);
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 80f;
        }

        private static GameObject CreateCanvas(string name)
        {
            GameObject canvasObj = new GameObject(name);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
            return canvasObj;
        }

        private static Text CreateText(string name, Transform parent, string text, int size, Vector2 pos, Vector2 dimensions)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = dimensions;
            Text label = obj.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            return label;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 pos)
        {
            return CreateButton(name, parent, label, pos, new Vector2(180, 52));
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 pos, Vector2 dimensions)
        {
            GameObject obj = CreatePanel(name, parent, new Color(1f, 1f, 1f, 0.14f), pos, dimensions);
            Button button = obj.AddComponent<Button>();
            CreateText("Text", obj.transform, label, 18, Vector2.zero, dimensions);
            return button;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color, Vector2 pos, Vector2 dimensions)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = dimensions;
            Image image = obj.AddComponent<Image>();
            image.color = color;
            return obj;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static void Save(Scene scene, string name)
        {
            EditorSceneManager.SaveScene(scene, $"{ScenePath}/{name}.unity");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            field ??= target.GetType().BaseType?.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            field?.SetValue(target, value);
        }
    }
}
#endif
