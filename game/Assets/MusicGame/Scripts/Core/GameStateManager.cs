using UnityEngine;
using UnityEngine.SceneManagement;

namespace MusicGame.Core
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public GameScene CurrentScene { get; private set; } = GameScene.MainMenu;
        public SongData SelectedSong { get; set; }
        public Difficulty SelectedDifficulty { get; set; } = Difficulty.Normal;
        public bool IsTutorialMode { get; private set; }


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ApplyAndroidDisplayMode();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                ApplyAndroidDisplayMode();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyAndroidDisplayMode();
        }

        private void Start()
        {
            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                ChangeScene(GameScene.MainMenu);
            }
        }

        public void ChangeScene(GameScene scene)
        {
            CurrentScene = scene;
            switch (scene)
            {
                case GameScene.MainMenu:
                    LoadScene("MainMenu");
                    break;
                case GameScene.SongSelect:
                    LoadScene("SongSelect");
                    break;
                case GameScene.Settings:
                    LoadScene("Settings");
                    break;
                case GameScene.About:
                    LoadScene("About");
                    break;
                case GameScene.Gameplay:
                    LoadScene("Gameplay");
                    break;
                case GameScene.Result:
                    LoadScene("Result");
                    break;
            }
        }

        private void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        private static void ApplyAndroidDisplayMode()
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    using AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow");
                    using AndroidJavaObject decorView = window.Call<AndroidJavaObject>("getDecorView");
                    const int layoutStable = 0x00000100;
                    const int layoutHideNavigation = 0x00000200;
                    const int layoutFullscreen = 0x00000400;
                    const int hideNavigation = 0x00000002;
                    const int fullscreen = 0x00000004;
                    const int immersiveSticky = 0x00001000;
                    decorView.Call("setSystemUiVisibility",
                        layoutStable | layoutHideNavigation | layoutFullscreen | hideNavigation | fullscreen | immersiveSticky);
                    window.Call("setStatusBarColor", 0);
                    window.Call("setNavigationBarColor", 0);

                    using AndroidJavaObject attributes = window.Call<AndroidJavaObject>("getAttributes");
                    attributes.Set("layoutInDisplayCutoutMode", 1);
                    window.Call("setAttributes", attributes);
                }));
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"[GameStateManager] Android immersive mode failed: {exception.Message}");
            }
#endif
        }

public void SetSelectedSong(SongData song)
        {
            IsTutorialMode = false;
            SelectedSong = song;
        }

public void SetSelectedDifficulty(Difficulty difficulty)
        {
            IsTutorialMode = false;
            SelectedDifficulty = difficulty;
        }

public void BeginTutorial(SongData tutorialSong)
        {
            SelectedSong = tutorialSong;
            SelectedDifficulty = Difficulty.Easy;
            IsTutorialMode = true;
            ChangeScene(GameScene.Gameplay);
        }

        public void CompleteTutorial()
        {
            if (!IsTutorialMode) return;
            IsTutorialMode = false;
            PlayerPrefs.SetInt("TutorialCompleted", 1);
            PlayerPrefs.Save();
        }

    }
}
