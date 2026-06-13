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
        private const string NoteSpriteResourcesPath = "Assets/MusicGame/Resources/Images/Notes";

        [MenuItem("MusicGame/Generate Basic Scene Info")]
        public static void Generate()
        {
            EnsureFolder("Assets/MusicGame", "Scenes");
            EnsureFolder("Assets/MusicGame", "Prefabs");
            EnsureFolder("Assets/MusicGame", "Generated");
            EnsureFolder("Assets/MusicGame", "Resources");
            EnsureFolder("Assets/MusicGame/Resources", "Songs");
            EnsureFolder("Assets/MusicGame/Resources", "Charts");
            EnsureFolder("Assets/MusicGame/Resources", "Images");
            EnsureFolder("Assets/MusicGame/Resources/Images", "Notes");
            EnsureFolder(GeneratedPath, "Materials");
            PrepareNoteSprites();

            Material cyan = CreateMaterial("BCI_Cyan", new Color(0.1f, 0.95f, 0.9f, 1f));
            Material white = CreateMaterial("BCI_White", Color.white);
            Material red = CreateMaterial("BCI_Red", new Color(1f, 0.18f, 0.18f, 1f));
            Material blue = CreateMaterial("BCI_Blue", new Color(0.2f, 0.45f, 1f, 1f));
            Material plane = CreateMaterial("JudgePlane_Transparent", new Color(0.1f, 0.9f, 1f, 0.08f), true);

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

            SpriteRenderer headRenderer = CreateSpriteVisual("HoldHeadClick", root.transform, "white_click", new Vector3(0.85f, 0.85f, 1f));
            SpriteRenderer tailRenderer = CreateSpriteVisual("HoldTailSlide", root.transform, "white_slide", new Vector3(0.78f, 0.78f, 1f));
            Transform head = headRenderer.transform;
            Transform tail = tailRenderer.transform;

            LineRenderer line = root.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.positionCount = 2;
            line.startWidth = 0.12f;
            line.endWidth = 0.12f;
            line.useWorldSpace = true;

            SetField(note, "spriteRenderer", headRenderer);
            SetField(note, "visualTransform", head);
            SetField(note, "tailSpriteRenderer", tailRenderer);
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

            SpriteRenderer arrowRenderer = CreateSpriteVisual("SlideArrow", root.transform, "miku_slide", Vector3.one);
            GameObject arrow = arrowRenderer.gameObject;

            SetField(note, "spriteRenderer", arrowRenderer);
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
            managers.AddComponent<AudioManager>();
            managers.AddComponent<UIManager>();

            GameObject canvas = CreateCanvas("MainMenuCanvas");
            Image background = CreatePanel("StartBackground", canvas.transform, Color.white, Vector2.zero, new Vector2(1600, 900)).GetComponent<Image>();
            background.sprite = LoadCoverSprite("Assets/Images/start.png");
            background.preserveAspect = false;
            background.raycastTarget = false;
            background.transform.SetAsFirstSibling();

            Button start = CreateButton("StartButton", canvas.transform, "\u5f00\u59cb", new Vector2(-400, -20), new Vector2(300, 76));
            Button settings = CreateButton("SettingsButton", canvas.transform, "\u8bbe\u7f6e", new Vector2(-400, -100), new Vector2(300, 76));
            Button about = CreateButton("AboutButton", canvas.transform, "\u5173\u4e8e", new Vector2(-400, -180), new Vector2(300, 76));
            Button quit = CreateButton("QuitButton", canvas.transform, "\u9000\u51fa", new Vector2(-400, -260), new Vector2(300, 76));
            StyleMenuTextButton(start);
            StyleMenuTextButton(settings);
            StyleMenuTextButton(about);
            StyleMenuTextButton(quit);

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
            ConfigureCamera(Color.black, false);
            GameObject canvas = CreateCanvas("SongSelectCanvas");
            canvas.AddComponent<SciFiCurveBackground>();

            CreatePanel("TopBand", canvas.transform, new Color(0.04f, 0.055f, 0.075f, 1f), new Vector2(0, 394), new Vector2(1600, 112));
            Text selectTitle = CreateText("SongSelectTitle", canvas.transform, "\u9009\u62e9\u97f3\u4e50", 42, new Vector2(-360, 394), new Vector2(420, 70));
            selectTitle.alignment = TextAnchor.MiddleLeft;
            Button back = CreateButton("BackButton", canvas.transform, "<  Back", new Vector2(-690, 394), new Vector2(135, 52));
            Button easy = CreateButton("EasyButton", canvas.transform, "EASY", new Vector2(120, -226), new Vector2(148, 62), new Color(0.18f, 0.72f, 0.36f, 1f));
            Button normal = CreateButton("NormalButton", canvas.transform, "NORMAL", new Vector2(286, -226), new Vector2(148, 62), new Color(0.12f, 0.39f, 0.94f, 1f));
            Button hard = CreateButton("HardButton", canvas.transform, "HARD", new Vector2(452, -226), new Vector2(148, 62), new Color(0.84f, 0.14f, 0.22f, 1f));
            Button confirm = CreateButton("ConfirmButton", canvas.transform, "\u786e\u5b9a", new Vector2(286, -302), new Vector2(232, 62), new Color32(0x39, 0xC5, 0xBB, 0xFF));

            Text listHeader = CreateText("SongListHeader", canvas.transform, "TRACK LIST", 30, new Vector2(-420, 276), new Vector2(400, 44));
            StyleGlowText(listHeader, new Color(0.05f, 0.95f, 1f, 0.85f));
            GameObject list = CreatePanel("SongList", canvas.transform, new Color(0.06f, 0.075f, 0.095f, 0.95f), new Vector2(-420, -18), new Vector2(590, 536));
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

            GameObject coverFrame = CreatePanel("CoverFrame", canvas.transform, new Color(0.22f, 0.86f, 0.9f, 1f), new Vector2(286, 103), new Vector2(350, 350));
            Image cover = CreatePanel("Cover", coverFrame.transform, Color.white, Vector2.zero, new Vector2(340, 340)).GetComponent<Image>();
            cover.preserveAspect = true;
            Text title = CreateText("TitleText", canvas.transform, "Song Title", 34, new Vector2(286, -103), new Vector2(530, 55));
            Text artist = CreateText("ArtistText", canvas.transform, "Artist", 20, new Vector2(286, -145), new Vector2(530, 36));
            artist.color = new Color(0.72f, 0.78f, 0.86f);

            SongSelectController controller = new GameObject("SongSelectController").AddComponent<SongSelectController>();
            SetField(controller, "songListContent", content.transform);
            SetField(controller, "songItemPrefab", CreateSongItemPrefab());
            SetField(controller, "availableSongs", LoadGeneratedSongs());
            SetField(controller, "backButton", back);
            SetField(controller, "coverFrame", coverFrame.GetComponent<Image>());
            SetField(controller, "coverImage", cover);
            SetField(controller, "titleText", title);
            SetField(controller, "artistText", artist);
            SetField(controller, "easyButton", easy);
            SetField(controller, "normalButton", normal);
            SetField(controller, "hardButton", hard);
            SetField(controller, "confirmButton", confirm);

            CreateEventSystem();
            Save(scene, "SongSelect");
        }

        private static void BuildGameplay(HoldNote holdPrefab, FlickNote flickPrefab, Material planeMaterial)
        {
            Scene scene = NewScene("Gameplay");
            ConfigureCamera(Color.black, true);

            GameObject judgePlane = new GameObject("JudgePlane_Z0");
            judgePlane.transform.position = Vector3.zero;
            judgePlane.AddComponent<JudgePlane>();
            judgePlane.AddComponent<SpaceGuide>();

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
            Text score = CreateText("ScoreText", canvas.transform, "SCORE  0", 42, Vector2.zero, new Vector2(500, 76));
            score.rectTransform.anchorMin = Vector2.one;
            score.rectTransform.anchorMax = Vector2.one;
            score.rectTransform.pivot = Vector2.one;
            score.rectTransform.anchoredPosition = new Vector2(-28f, -18f);
            score.fontStyle = FontStyle.Bold;
            score.alignment = TextAnchor.MiddleRight;
            Text combo = CreateText("ComboText", canvas.transform, "COMBO  0", 56, new Vector2(0, 390), new Vector2(460, 94));
            combo.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            combo.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            combo.rectTransform.pivot = new Vector2(0.5f, 1f);
            combo.rectTransform.anchoredPosition = new Vector2(0f, -20f);
            combo.fontStyle = FontStyle.Bold;
            combo.color = new Color(0.24f, 0.94f, 1f, 1f);
            Text accuracy = CreateText("AccuracyText", canvas.transform, "ACC  100%", 42, Vector2.zero, new Vector2(500, 76));
            accuracy.rectTransform.anchorMin = Vector2.one;
            accuracy.rectTransform.anchorMax = Vector2.one;
            accuracy.rectTransform.pivot = Vector2.one;
            accuracy.rectTransform.anchoredPosition = new Vector2(-28f, -94f);
            accuracy.fontStyle = FontStyle.Bold;
            accuracy.alignment = TextAnchor.MiddleRight;
            Button pause = CreateButton("PauseButton", canvas.transform, "II", Vector2.zero, new Vector2(120, 94));
            pause.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 1f);
            pause.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 1f);
            pause.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);
            pause.GetComponent<RectTransform>().anchoredPosition = new Vector2(28f, -18f);
            Text pauseLabel = pause.GetComponentInChildren<Text>();
            pauseLabel.fontSize = 56;
            pauseLabel.fontStyle = FontStyle.Bold;
            GameObject pausePanel = CreatePanel("PauseMenuPanel", canvas.transform, new Color(0f, 0f, 0f, 0.82f), Vector2.zero, new Vector2(900, 600));
            Button resume = CreateButton("ResumeButton", pausePanel.transform, "Resume", new Vector2(0, 80), new Vector2(300, 72));
            Button restart = CreateButton("RestartButton", pausePanel.transform, "Restart", Vector2.zero, new Vector2(300, 72));
            Button quit = CreateButton("QuitButton", pausePanel.transform, "Quit", new Vector2(0, -80), new Vector2(300, 72));
            foreach (Button button in new[] { resume, restart, quit })
            {
                Text label = button.GetComponentInChildren<Text>();
                label.fontSize = 40;
                label.fontStyle = FontStyle.Bold;
            }
            pausePanel.SetActive(false);

            GameplayController controller = new GameObject("GameplayController").AddComponent<GameplayController>();
            SetField(controller, "scoreText", score);
            SetField(controller, "comboText", combo);
            SetField(controller, "accuracyText", accuracy);
            SetField(controller, "pauseButton", pause);
            SetField(controller, "pauseMenuPanel", pausePanel);
            SetField(controller, "resumeButton", resume);
            SetField(controller, "restartButton", restart);
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
            SettingsController controller = new GameObject("SettingsController").AddComponent<SettingsController>();
            CreateEventSystem();
            Save(scene, "Settings");
        }

        private static void BuildAbout()
        {
            Scene scene = NewScene("About");
            ConfigureCamera(Color.black, false);
            GameObject canvas = CreateCanvas("AboutCanvas");
            Text about = CreateText("AboutText", canvas.transform, "", 22, new Vector2(0, 40), new Vector2(680, 280));
            AboutController controller = new GameObject("AboutController").AddComponent<AboutController>();
            SetField(controller, "aboutText", about);
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
            image.color = new Color(0.1f, 0.13f, 0.17f, 1f);
            Button button = item.AddComponent<Button>();
            ConfigureButtonColors(button, image.color);
            item.AddComponent<SongItemHoverEffect>();
            Text label = CreateText("Label", item.transform, "Song", 24, Vector2.zero, rect.sizeDelta);
            label.alignment = TextAnchor.MiddleLeft;
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(24, 0);
            labelRect.offsetMax = new Vector2(-24, 0);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(item, $"{PrefabPath}/SongItem_Basic.prefab");
            Object.DestroyImmediate(item);
            return prefab;
        }

        private static void PrepareNoteSprites()
        {
            string[] colors = { "miku", "white", "red", "blue" };
            string[] shapes = { "round", "click", "slide" };
            foreach (string color in colors)
            {
                foreach (string shape in shapes)
                {
                    string fileName = $"{color}_{shape}.svg";
                    string source = $"Assets/Images/Notes/{fileName}";
                    string destination = $"{NoteSpriteResourcesPath}/{fileName}";
                    if (AssetDatabase.LoadAssetAtPath<Object>(destination) == null)
                    {
                        AssetDatabase.CopyAsset(source, destination);
                    }
                    SetSvgAsSprite(destination);
                }
            }
        }

        private static void SetSvgAsSprite(string path)
        {
            AssetImporter importer = AssetImporter.GetAtPath(path);
            if (importer == null) return;

            SerializedObject serializedImporter = new SerializedObject(importer);
            SerializedProperty svgType = serializedImporter.FindProperty("m_SvgType");
            if (svgType != null && svgType.enumValueIndex != 1)
            {
                svgType.enumValueIndex = 1;
                serializedImporter.ApplyModifiedPropertiesWithoutUndo();
                importer.SaveAndReimport();
            }
        }

        private static void GenerateDefaultSongsAndCharts()
        {
            CreateSong("2077", "2077", "City of Night", "曲师：xxx 谱师：xyxuanying", "2077", "song2", "City of Night");
            CreateSong("Jumping", "jumping", "Cute Jump", "曲师：xxx 谱师：xyxuanying", "Jumping", "song3", "Cute Jump");
            CreateSong("Kite", "kite", "风筝", "曲师：xxx 谱师：xyxuanying", "Kite", "song6", "F");
            CreateSong("song1", "song1", "A Forever Friend", "曲师：未知 谱师：xyxuanying", "2077", "song1", "A Forever Friend");
            CreateSong("song4", "song4", "Lost in the Phantom Night", "曲师：未知 谱师：xyxuanying", "2077", "song4", "Lost in the Phantom Night(1)");
            CreateSong("song5", "song5", "song5", "曲师：未知 谱师：xyxuanying", "2077", "song5", "");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateSong(string assetName, string songId, string title, string artist, string coverName, string cueSheetName, string cueName)
        {
            string assetPath = $"Assets/MusicGame/Resources/Songs/{assetName}.asset";
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
            song.cueSheetName = cueSheetName;
            song.cueName = cueName;
            song.coverImage = LoadCoverSprite($"Assets/Images/Covers/{coverName}.png")
                ?? LoadCoverSprite("Assets/Images/Covers/2077.png");
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
                    hasTailFlick = false,
                    duration = flick ? 0f : Mathf.Lerp(0.8f, 1.8f, (i % 3) / 2f),
                    threshold = 45 + (i % 4) * 10,
                    flickDirection = (FlickDirection)(i % 4),
                    approachTime = 2f,
                    useCustomEndPoint = !flick,
                    endX = Mathf.Sin((i + 1) * 0.7f) * 2.8f,
                    endY = Mathf.Cos((i + 1) * 0.55f) * 1.8f,
                    endZ = 10f,
                    attentionPoints = flick
                        ? new List<NotePathPoint>()
                        : new List<NotePathPoint>
                        {
                            new NotePathPoint
                            {
                                timeOffset = Mathf.Lerp(0.25f, 0.65f, (i % 3) / 2f),
                                x = Mathf.Lerp(Mathf.Sin(i * 0.7f) * 2.8f, Mathf.Sin((i + 1) * 0.7f) * 2.8f, 0.5f),
                                y = Mathf.Lerp(Mathf.Cos(i * 0.55f) * 1.8f, Mathf.Cos((i + 1) * 0.55f) * 1.8f, 0.5f),
                                z = 10f
                            }
                        }
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
                AssetDatabase.LoadAssetAtPath<SongData>("Assets/MusicGame/Resources/Songs/Kite.asset"),
                AssetDatabase.LoadAssetAtPath<SongData>("Assets/MusicGame/Resources/Songs/song1.asset"),
                AssetDatabase.LoadAssetAtPath<SongData>("Assets/MusicGame/Resources/Songs/song4.asset"),
                AssetDatabase.LoadAssetAtPath<SongData>("Assets/MusicGame/Resources/Songs/song5.asset")
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

        private static SpriteRenderer CreateSpriteVisual(string name, Transform parent, string spriteName, Vector3 scale)
        {
            GameObject visual = new GameObject(name);
            visual.transform.SetParent(parent, false);
            visual.transform.localScale = scale;
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{NoteSpriteResourcesPath}/{spriteName}.svg");
            renderer.color = Color.white;
            return renderer;
        }

        private static Material CreateMaterial(string name, Color color, bool transparent = false)
        {
            string path = $"{GeneratedPath}/Materials/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = transparent
                    ? Shader.Find("Universal Render Pipeline/Unlit")
                    : Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
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
            label.font = UIThemeFont.Font;
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
            return CreateButton(name, parent, label, pos, dimensions, new Color(0.12f, 0.16f, 0.21f, 1f));
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 pos, Vector2 dimensions, Color normalColor)
        {
            GameObject obj = CreatePanel(name, parent, normalColor, pos, dimensions);
            Button button = obj.AddComponent<Button>();
            ConfigureButtonColors(button, normalColor);
            CreateText("Text", obj.transform, label, 18, Vector2.zero, dimensions);
            return button;
        }

        private static void ConfigureButtonColors(Button button, Color normalColor)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.25f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(normalColor.r, normalColor.g, normalColor.b, 0.35f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        private static void StyleGlowText(Text text, Color glowColor)
        {
            text.color = new Color(0.28f, 0.93f, 1f, 1f);
            Outline outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = glowColor;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private static void StyleMenuTextButton(Button button)
        {
            Image background = button.GetComponent<Image>();
            if (background != null)
                background.color = Color.clear;
            button.transition = Selectable.Transition.None;

            Text label = button.GetComponentInChildren<Text>();
            if (label == null) return;
            label.fontSize = 40;
            label.fontStyle = FontStyle.Bold;
            Outline outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.05f, 0.78f, 1f, 0.68f);
            outline.effectDistance = new Vector2(2f, -2f);
            SongItemHoverEffect hoverEffect = button.gameObject.AddComponent<SongItemHoverEffect>();
            hoverEffect.SetLabel(label);
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
