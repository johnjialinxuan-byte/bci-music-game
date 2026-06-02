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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
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

        public void SetSelectedSong(SongData song)
        {
            SelectedSong = song;
        }

        public void SetSelectedDifficulty(Difficulty difficulty)
        {
            SelectedDifficulty = difficulty;
        }
    }
}
