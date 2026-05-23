#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using MusicGame.Core;
using MusicGame.Audio;
using MusicGame.Gameplay;
using MusicGame.Notes;
using MusicGame.UI;
using MusicGame.Scenes;

namespace MusicGame.Editor
{
    public static class SceneBuilder
    {
        private const string ScenesPath = "Assets/MusicGame/Scenes";

        [MenuItem("MusicGame/Build All Scenes")]
        public static void BuildAllScenes()
        {
            if (!AssetDatabase.IsValidFolder(ScenesPath))
            {
                AssetDatabase.CreateFolder("Assets/MusicGame", "Scenes");
            }

            BuildMainMenuScene();
            BuildSongSelectScene();
            BuildDifficultySelectScene();
            BuildGameplayScene();
            BuildSettingsScene();
            BuildAboutScene();
            BuildResultScene();

            // Add scenes to build settings
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene($"{ScenesPath}/MainMenu.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/SongSelect.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/DifficultySelect.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/Settings.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/About.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/Gameplay.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/Result.unity", true)
            };

            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBuilder] All scenes built successfully!");
        }

        private static void BuildMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "MainMenu";

            // Setup Camera for 2D
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 5;
                cam.transform.position = new Vector3(0, 0, -10);
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            }

            // Create Managers
            GameObject managers = new GameObject("Managers");
            managers.AddComponent<GameStateManager>();
            managers.AddComponent<ScoreManager>();
            managers.AddComponent<AudioManager>();
            managers.AddComponent<UIManager>();

            // Create Canvas
            GameObject canvasObj = CreateCanvas("MainMenuCanvas");
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Background
            GameObject bg = CreatePanel("Background", canvasObj.transform, Color.black);
            bg.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bg.GetComponent<RectTransform>().anchorMax = Vector2.one;
            bg.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            bg.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            // Title
            GameObject title = CreateText("Title", canvasObj.transform, "Music Game", 48, TextAnchor.MiddleCenter);
            title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 100);

            // Start Button
            GameObject startBtn = CreateButton("StartButton", canvasObj.transform, "Start Game", new Vector2(0, 20));

            // Settings Button
            GameObject settingsBtn = CreateButton("SettingsButton", canvasObj.transform, "Settings", new Vector2(0, -60));

            // About Button
            GameObject aboutBtn = CreateButton("AboutButton", canvasObj.transform, "About", new Vector2(0, -140));

            // Quit Button
            GameObject quitBtn = CreateButton("QuitButton", canvasObj.transform, "Quit", new Vector2(0, -220));

            // Add MainMenuController
            GameObject controller = new GameObject("MainMenuController");
            MainMenuController menuCtrl = controller.AddComponent<MainMenuController>();
            menuCtrl.GetType().GetField("startButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(menuCtrl, startBtn.GetComponent<Button>());
            menuCtrl.GetType().GetField("settingsButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(menuCtrl, settingsBtn.GetComponent<Button>());
            menuCtrl.GetType().GetField("aboutButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(menuCtrl, aboutBtn.GetComponent<Button>());
            menuCtrl.GetType().GetField("quitButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(menuCtrl, quitBtn.GetComponent<Button>());

            // Create EventSystem
            CreateEventSystem();

            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/MainMenu.unity");
        }

        private static void BuildSongSelectScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "SongSelect";

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            }

            GameObject canvasObj = CreateCanvas("SongSelectCanvas");

            // Back Button
            GameObject backBtn = CreateButton("BackButton", canvasObj.transform, "Back", new Vector2(-350, 200), new Vector2(120, 50));

            // Song List ScrollView
            GameObject scrollView = CreateScrollView("SongList", canvasObj.transform, new Vector2(0, -20), new Vector2(500, 350));
            Transform content = scrollView.transform.Find("Viewport/Content");

            // Cover Image
            GameObject coverObj = new GameObject("CoverImage");
            coverObj.transform.SetParent(canvasObj.transform);
            Image coverImg = coverObj.AddComponent<Image>();
            coverImg.color = Color.gray;
            RectTransform coverRect = coverObj.GetComponent<RectTransform>();
            coverRect.anchoredPosition = new Vector2(250, 80);
            coverRect.sizeDelta = new Vector2(200, 200);

            // Title & Artist
            GameObject titleTxt = CreateText("TitleText", canvasObj.transform, "Song Title", 28, TextAnchor.MiddleLeft);
            titleTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(250, -60);
            titleTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 40);

            GameObject artistTxt = CreateText("ArtistText", canvasObj.transform, "Artist Name", 20, TextAnchor.MiddleLeft);
            artistTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(250, -100);
            artistTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 30);

            // Preview Button
            GameObject previewBtn = CreateButton("PreviewButton", canvasObj.transform, "Play Preview", new Vector2(250, -160), new Vector2(180, 50));

            // Controller
            GameObject controller = new GameObject("SongSelectController");
            SongSelectController ctrl = controller.AddComponent<SongSelectController>();
            ctrl.GetType().GetField("songListContent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, content);
            ctrl.GetType().GetField("backButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, backBtn.GetComponent<Button>());
            ctrl.GetType().GetField("playPreviewButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, previewBtn.GetComponent<Button>());
            ctrl.GetType().GetField("coverImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, coverImg);
            ctrl.GetType().GetField("titleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, titleTxt.GetComponent<Text>());
            ctrl.GetType().GetField("artistText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, artistTxt.GetComponent<Text>());

            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/SongSelect.unity");
        }

        private static void BuildDifficultySelectScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "DifficultySelect";

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            }

            GameObject canvasObj = CreateCanvas("DifficultySelectCanvas");

            // Title
            GameObject titleTxt = CreateText("SongTitle", canvasObj.transform, "Song Title", 32, TextAnchor.MiddleCenter);
            titleTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 150);
            titleTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 60);

            // Difficulty Buttons
            GameObject easyBtn = CreateButton("EasyButton", canvasObj.transform, "Easy", new Vector2(0, 60), new Vector2(200, 50));
            GameObject normalBtn = CreateButton("NormalButton", canvasObj.transform, "Normal", new Vector2(0, 0), new Vector2(200, 50));
            GameObject hardBtn = CreateButton("HardButton", canvasObj.transform, "Hard", new Vector2(0, -60), new Vector2(200, 50));

            // Info Text
            GameObject infoTxt = CreateText("DifficultyInfo", canvasObj.transform, "Select Difficulty", 20, TextAnchor.MiddleCenter);
            infoTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -180);
            infoTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 40);

            // Back Button
            GameObject backBtn = CreateButton("BackButton", canvasObj.transform, "Back", new Vector2(-350, 200), new Vector2(120, 50));

            // Controller
            GameObject controller = new GameObject("DifficultySelectController");
            DifficultySelectController ctrl = controller.AddComponent<DifficultySelectController>();
            ctrl.GetType().GetField("easyButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, easyBtn.GetComponent<Button>());
            ctrl.GetType().GetField("normalButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, normalBtn.GetComponent<Button>());
            ctrl.GetType().GetField("hardButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, hardBtn.GetComponent<Button>());
            ctrl.GetType().GetField("backButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, backBtn.GetComponent<Button>());
            ctrl.GetType().GetField("songTitleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, titleTxt.GetComponent<Text>());
            ctrl.GetType().GetField("difficultyInfoText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, infoTxt.GetComponent<Text>());

            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/DifficultySelect.unity");
        }

        private static void BuildGameplayScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "Gameplay";

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 5;
                cam.transform.position = new Vector3(0, 0, -10);
                cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 100f;
            }

            // Gameplay Camera
            GameObject gameplayCamObj = new GameObject("GameplayCamera");
            Camera gameplayCam = gameplayCamObj.AddComponent<Camera>();
            gameplayCam.orthographic = true;
            gameplayCam.orthographicSize = 5;
            gameplayCam.transform.position = new Vector3(0, 0, -10);
            gameplayCam.backgroundColor = Color.clear;
            gameplayCam.clearFlags = CameraClearFlags.Depth;
            gameplayCam.depth = 1;

            // Judge Plane
            GameObject judgePlaneObj = new GameObject("JudgePlane");
            judgePlaneObj.transform.position = Vector3.zero;
            JudgePlane judgePlane = judgePlaneObj.AddComponent<JudgePlane>();
            judgePlaneObj.AddComponent<SpaceGuide>();

            // Managers
            GameObject chartMgr = new GameObject("ChartManager");
            chartMgr.AddComponent<MusicGame.Managers.ChartManager>();

            GameObject noteMgr = new GameObject("NoteManager");
            NoteSpawner spawner = noteMgr.AddComponent<NoteSpawner>();
            noteMgr.AddComponent<MusicGame.Managers.NoteManager>();

            GameObject inputMgr = new GameObject("InputManager");
            inputMgr.AddComponent<MusicGame.Managers.InputManager>();
            inputMgr.AddComponent<MusicGame.Input.DemoHoldProvider>();
            inputMgr.AddComponent<MusicGame.Input.DemoHeadMotionProvider>();

            GameObject judgeMgr = new GameObject("JudgeManager");
            judgeMgr.AddComponent<MusicGame.Managers.JudgeManager>();

            // Canvas
            GameObject canvasObj = CreateCanvas("GameplayCanvas");
            canvasObj.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.GetComponent<Canvas>().sortingOrder = 10;

            // Score Text
            GameObject scoreTxt = CreateText("ScoreText", canvasObj.transform, "Score: 0", 24, TextAnchor.MiddleLeft);
            scoreTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(-350, 220);
            scoreTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 40);

            // Combo Text
            GameObject comboTxt = CreateText("ComboText", canvasObj.transform, "Combo: 0", 24, TextAnchor.MiddleLeft);
            comboTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(-350, 180);
            comboTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 40);

            // Accuracy Text
            GameObject accTxt = CreateText("AccuracyText", canvasObj.transform, "Acc: 100%", 24, TextAnchor.MiddleLeft);
            accTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(-350, 140);
            accTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 40);

            // Pause Button
            GameObject pauseBtn = CreateButton("PauseButton", canvasObj.transform, "II", new Vector2(380, 220), new Vector2(60, 50));

            // Pause Menu Panel
            GameObject pausePanel = CreatePanel("PauseMenuPanel", canvasObj.transform, new Color(0, 0, 0, 0.8f));
            pausePanel.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            pausePanel.GetComponent<RectTransform>().anchorMax = Vector2.one;
            pausePanel.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            pausePanel.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            pausePanel.SetActive(false);

            // Resume Button (child of pause panel)
            GameObject resumeBtn = CreateButton("ResumeButton", pausePanel.transform, "Resume", new Vector2(0, 30), new Vector2(200, 50));

            // Quit Button (child of pause panel)
            GameObject quitBtn = CreateButton("QuitButton", pausePanel.transform, "Quit to Menu", new Vector2(0, -40), new Vector2(200, 50));

            // Controller
            GameObject controller = new GameObject("GameplayController");
            GameplayController ctrl = controller.AddComponent<GameplayController>();
            ctrl.GetType().GetField("noteSpawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, spawner);
            ctrl.GetType().GetField("gameplayCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, gameplayCam);
            ctrl.GetType().GetField("judgePlaneTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, judgePlaneObj.transform);
            ctrl.GetType().GetField("scoreText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, scoreTxt.GetComponent<Text>());
            ctrl.GetType().GetField("comboText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, comboTxt.GetComponent<Text>());
            ctrl.GetType().GetField("accuracyText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, accTxt.GetComponent<Text>());
            ctrl.GetType().GetField("pauseButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, pauseBtn.GetComponent<Button>());
            ctrl.GetType().GetField("pauseMenuPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, pausePanel);
            ctrl.GetType().GetField("resumeButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, resumeBtn.GetComponent<Button>());
            ctrl.GetType().GetField("quitButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, quitBtn.GetComponent<Button>());

            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/Gameplay.unity");
        }

        private static void BuildResultScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "Result";

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            }

            GameObject canvasObj = CreateCanvas("ResultCanvas");

            // Title
            GameObject titleTxt = CreateText("ResultTitle", canvasObj.transform, "Result", 36, TextAnchor.MiddleCenter);
            titleTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 200);
            titleTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 60);

            // Stats
            GameObject scoreTxt = CreateText("ScoreText", canvasObj.transform, "Score: 0", 24, TextAnchor.MiddleCenter);
            scoreTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 120);
            scoreTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 40);

            GameObject maxComboTxt = CreateText("MaxComboText", canvasObj.transform, "Max Combo: 0", 22, TextAnchor.MiddleCenter);
            maxComboTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 80);
            maxComboTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 35);

            GameObject perfectTxt = CreateText("PerfectText", canvasObj.transform, "Perfect: 0", 20, TextAnchor.MiddleCenter);
            perfectTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(-100, 30);
            perfectTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 30);

            GameObject goodTxt = CreateText("GoodText", canvasObj.transform, "Good: 0", 20, TextAnchor.MiddleCenter);
            goodTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(-100, -10);
            goodTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 30);

            GameObject missTxt = CreateText("MissText", canvasObj.transform, "Miss: 0", 20, TextAnchor.MiddleCenter);
            missTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(100, -10);
            missTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 30);

            GameObject accTxt = CreateText("AccuracyText", canvasObj.transform, "Accuracy: 100%", 22, TextAnchor.MiddleCenter);
            accTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -60);
            accTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 35);

            // Rank Text
            GameObject rankTxt = CreateText("RankText", canvasObj.transform, "Rank: S", 48, TextAnchor.MiddleCenter);
            rankTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -120);
            rankTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 70);

            // Buttons
            GameObject retryBtn = CreateButton("RetryButton", canvasObj.transform, "Retry", new Vector2(-120, -220), new Vector2(180, 50));
            GameObject backBtn = CreateButton("BackButton", canvasObj.transform, "Back to Menu", new Vector2(120, -220), new Vector2(180, 50));

            // Controller
            GameObject controller = new GameObject("ResultController");
            ResultController ctrl = controller.AddComponent<ResultController>();
            ctrl.GetType().GetField("resultTitleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, titleTxt.GetComponent<Text>());
            ctrl.GetType().GetField("scoreText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, scoreTxt.GetComponent<Text>());
            ctrl.GetType().GetField("maxComboText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, maxComboTxt.GetComponent<Text>());
            ctrl.GetType().GetField("perfectText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, perfectTxt.GetComponent<Text>());
            ctrl.GetType().GetField("goodText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, goodTxt.GetComponent<Text>());
            ctrl.GetType().GetField("missText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, missTxt.GetComponent<Text>());
            ctrl.GetType().GetField("accuracyText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, accTxt.GetComponent<Text>());
            ctrl.GetType().GetField("rankText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, rankTxt.GetComponent<Text>());
            ctrl.GetType().GetField("retryButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, retryBtn.GetComponent<Button>());
            ctrl.GetType().GetField("backButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, backBtn.GetComponent<Button>());

            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/Result.unity");
        }

        private static void BuildSettingsScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "Settings";

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            }

            GameObject canvasObj = CreateCanvas("SettingsCanvas");

            GameObject titleTxt = CreateText("Title", canvasObj.transform, "Settings", 36, TextAnchor.MiddleCenter);
            titleTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 180);
            titleTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 60);

            GameObject masterVolTxt = CreateText("MasterVolLabel", canvasObj.transform, "Master Volume", 20, TextAnchor.MiddleLeft);
            masterVolTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, 100);
            masterVolTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 30);
            GameObject masterVolSlider = CreateSlider("MasterVolSlider", canvasObj.transform, new Vector2(0, 60));

            GameObject musicVolTxt = CreateText("MusicVolLabel", canvasObj.transform, "Music Volume", 20, TextAnchor.MiddleLeft);
            musicVolTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, 20);
            musicVolTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 30);
            GameObject musicVolSlider = CreateSlider("MusicVolSlider", canvasObj.transform, new Vector2(0, -20));

            GameObject sfxVolTxt = CreateText("SFXVolLabel", canvasObj.transform, "SFX Volume", 20, TextAnchor.MiddleLeft);
            sfxVolTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, -60);
            sfxVolTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 30);
            GameObject sfxVolSlider = CreateSlider("SFXVolSlider", canvasObj.transform, new Vector2(0, -100));

            GameObject offsetTxt = CreateText("OffsetLabel", canvasObj.transform, "Input Offset", 20, TextAnchor.MiddleLeft);
            offsetTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, -140);
            offsetTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 30);
            GameObject offsetSlider = CreateSlider("OffsetSlider", canvasObj.transform, new Vector2(0, -180));
            GameObject offsetValueTxt = CreateText("OffsetValue", canvasObj.transform, "0ms", 20, TextAnchor.MiddleLeft);
            offsetValueTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(200, -180);
            offsetValueTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 30);

            GameObject calibrateBtn = CreateButton("CalibrateButton", canvasObj.transform, "Calibrate Input", new Vector2(0, -240), new Vector2(200, 50));
            GameObject backBtn = CreateButton("BackButton", canvasObj.transform, "Back", new Vector2(0, -320), new Vector2(200, 50));

            GameObject controller = new GameObject("SettingsController");
            SettingsController ctrl = controller.AddComponent<SettingsController>();
            ctrl.GetType().GetField("masterVolumeSlider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, masterVolSlider.GetComponent<Slider>());
            ctrl.GetType().GetField("musicVolumeSlider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, musicVolSlider.GetComponent<Slider>());
            ctrl.GetType().GetField("sfxVolumeSlider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, sfxVolSlider.GetComponent<Slider>());
            ctrl.GetType().GetField("inputOffsetSlider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, offsetSlider.GetComponent<Slider>());
            ctrl.GetType().GetField("inputOffsetText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, offsetValueTxt.GetComponent<Text>());
            ctrl.GetType().GetField("calibrateButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, calibrateBtn.GetComponent<Button>());
            ctrl.GetType().GetField("backButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, backBtn.GetComponent<Button>());

            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/Settings.unity");
        }

        private static void BuildAboutScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "About";

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            }

            GameObject canvasObj = CreateCanvas("AboutCanvas");

            GameObject titleTxt = CreateText("Title", canvasObj.transform, "About", 36, TextAnchor.MiddleCenter);
            titleTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 180);
            titleTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 60);

            GameObject aboutTxt = CreateText("AboutText", canvasObj.transform, "About Text", 20, TextAnchor.MiddleCenter);
            aboutTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            aboutTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 300);

            GameObject backBtn = CreateButton("BackButton", canvasObj.transform, "Back", new Vector2(0, -220), new Vector2(200, 50));

            GameObject controller = new GameObject("AboutController");
            AboutController ctrl = controller.AddComponent<AboutController>();
            ctrl.GetType().GetField("aboutText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, aboutTxt.GetComponent<Text>());
            ctrl.GetType().GetField("backButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ctrl, backBtn.GetComponent<Button>());

            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/About.unity");
        }

        // Helpers

        private static GameObject CreateCanvas(string name)
        {
            GameObject canvasObj = new GameObject(name);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
            return canvasObj;
        }

        private static GameObject CreateEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            return eventSystem;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent);
            Image img = panel.AddComponent<Image>();
            img.color = color;
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            return panel;
        }

        private static GameObject CreateText(string name, Transform parent, string text, int fontSize, TextAnchor alignment)
        {
            GameObject txtObj = new GameObject(name);
            txtObj.transform.SetParent(parent);
            Text txt = txtObj.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = Color.white;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform rect = txtObj.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(400, 50);
            rect.localScale = Vector3.one;
            return txtObj;
        }

        private static GameObject CreateButton(string name, Transform parent, string text, Vector2 pos, Vector2 size = default)
        {
            if (size == default) size = new Vector2(200, 60);

            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent);
            Button btn = btnObj.AddComponent<Button>();
            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.5f, 0.9f, 1f);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;

            GameObject txtObj = CreateText(name + "Text", btnObj.transform, text, 24, TextAnchor.MiddleCenter);
            txtObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            txtObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
            txtObj.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            txtObj.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            txtObj.GetComponent<RectTransform>().localScale = Vector3.one;

            return btnObj;
        }

        private static GameObject CreateSlider(string name, Transform parent, Vector2 pos)
        {
            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent);
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.5f;
            RectTransform rect = sliderObj.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(300, 30);
            rect.localScale = Vector3.one;

            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.5f, 0.9f, 1f);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            slider.fillRect = fillRect;
            slider.targetGraphic = fillImg;

            return sliderObj;
        }

        private static GameObject CreateScrollView(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            GameObject scrollObj = new GameObject(name);
            scrollObj.transform.SetParent(parent);
            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            RectTransform scrollRect = scrollObj.GetComponent<RectTransform>();
            scrollRect.anchoredPosition = pos;
            scrollRect.sizeDelta = size;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform);
            RectTransform vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>();
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 300);
            content.AddComponent<VerticalLayoutGroup>();
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRect;
            scroll.content = contentRect;
            scroll.vertical = true;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            return scrollObj;
        }
    }
}
#endif
