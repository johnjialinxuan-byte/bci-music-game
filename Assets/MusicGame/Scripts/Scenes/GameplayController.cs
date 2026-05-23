using UnityEngine;
using UnityEngine.UI;
using MusicGame.Core;
using MusicGame.Audio;
using MusicGame.Gameplay;
using MusicGame.Managers;

namespace MusicGame.Scenes
{
    public class GameplayController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text comboText;
        [SerializeField] private Text accuracyText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitButton;

        private SongData currentSong;
        private ChartData currentChart;
        private bool isPlaying;
        private bool isPaused;

        private void Start()
        {
            SetupScene();
            InitializeGameplay();
        }

        private void SetupScene()
        {
            if (pauseButton != null)
                pauseButton.onClick.AddListener(OnPause);
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResume);
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitToMenu);

            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
        }

private void InitializeGameplay()
        {
            if (GameStateManager.Instance == null)
            {
                Debug.LogError("[GameplayController] GameStateManager not found. Start from MainMenu.");
                return;
            }

            currentSong = GameStateManager.Instance.SelectedSong;
            if (currentSong == null)
            {
                Debug.LogError("[GameplayController] No song selected. Returning to SongSelect.");
                GameStateManager.Instance.ChangeScene(GameScene.SongSelect);
                return;
            }

            string chartPath = currentSong.GetChartPath(GameStateManager.Instance.SelectedDifficulty);
            currentChart = ChartManager.Instance.LoadChart(chartPath);
            if (currentChart == null)
            {
                Debug.LogError("[GameplayController] No chart found!");
                GameStateManager.Instance.ChangeScene(GameScene.SongSelect);
                return;
            }

            ScoreManager.Instance.Initialize(currentChart.notes.Count);
            NoteManager.Instance.LoadChart(currentChart);

            AudioManager.Instance.PlaySong(currentSong.cueSheetName, currentSong.cueName);
            NoteManager.Instance.StartSpawning();
            isPlaying = true;
        }

        private void Update()
        {
            if (!isPlaying || isPaused) return;
            UpdateUI();
            CheckGameEnd();
        }

        private void UpdateUI()
        {
            if (scoreText != null)
                scoreText.text = $"Score: {ScoreManager.Instance.Score}";
            if (comboText != null)
                comboText.text = $"Combo: {ScoreManager.Instance.Combo}";
            if (accuracyText != null)
                accuracyText.text = $"Acc: {ScoreManager.Instance.Accuracy:F1}%";
        }

        private void CheckGameEnd()
        {
            if (AudioManager.Instance.IsPlaying()) return;
            if (NoteManager.Instance.HasActiveNotes) return;

            Invoke(nameof(ShowResult), 1f);
            isPlaying = false;
        }

        private void ShowResult()
        {
            GameStateManager.Instance.ChangeScene(GameScene.Result);
        }

        private void OnPause()
        {
            if (!isPlaying) return;
            isPaused = true;
            AudioManager.Instance.Pause();
            NoteManager.Instance.StopSpawning();
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(true);
        }

        private void OnResume()
        {
            if (!isPaused) return;
            isPaused = false;
            AudioManager.Instance.Resume();
            NoteManager.Instance.StartSpawning();
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
        }

        private void OnQuitToMenu()
        {
            AudioManager.Instance.StopSong();
            GameStateManager.Instance.ChangeScene(GameScene.MainMenu);
        }
    }
}
