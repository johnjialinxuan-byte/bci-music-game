using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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
        [SerializeField] private Button restartButton;

        private SongData currentSong;
        private ChartData currentChart;
        private bool isPlaying;
        private bool isPaused;
        private bool isCountingDown;
        private string countdownDisplay = "";

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
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestart);

            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);

            // [TEMP] Auto-create IPC + BCIInputProvider + BCIDebugOverlay via weak references.
            // Removing any of these scripts later will not break compilation.
            System.Type ipcType = System.Type.GetType("IPC, Assembly-CSharp");
            if (ipcType != null)
            {
                Object ipcObj = FindObjectOfType(ipcType);
                if (ipcObj == null)
                {
                    GameObject ipcGo = new GameObject("IPC (TEMP)");
                    ipcGo.AddComponent(ipcType);
                    Debug.Log("[GameplayController] Auto-created IPC GameObject.");
                }
            }

            System.Type bciInputType = System.Type.GetType("MusicGame.Input.BCIInputProvider, Assembly-CSharp");
            if (bciInputType != null && FindObjectOfType(bciInputType) == null)
            {
                GameObject bciGo = new GameObject("BCIInputProvider (TEMP)");
                Object comp = bciGo.AddComponent(bciInputType);

                // Try to wire up IPC reference via reflection
                System.Type ipcType2 = System.Type.GetType("IPC, Assembly-CSharp");
                if (ipcType2 != null)
                {
                    Object ipcInstance = FindObjectOfType(ipcType2);
                    if (ipcInstance != null)
                    {
                        var field = bciInputType.GetField("ipc", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (field != null) field.SetValue(comp, ipcInstance);
                    }
                }
                Debug.Log("[GameplayController] Auto-created BCIInputProvider GameObject.");
            }

            System.Type bciType = System.Type.GetType("MusicGame.Input.BCIDebugOverlay, Assembly-CSharp");
            if (bciType != null && FindObjectOfType(bciType) == null)
            {
                GameObject debugGo = new GameObject("BCIDebugOverlay (TEMP)");
                debugGo.AddComponent(bciType);
            }
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
                Debug.LogError("[GameplayController] No valid JSON chart found.");
                GameStateManager.Instance.ChangeScene(GameScene.SongSelect);
                return;
            }

            if (AudioManager.Instance == null)
            {
                Debug.LogError("[GameplayController] AudioManager not found. Start from MainMenu.");
                return;
            }

            AudioManager.Instance.PlaySong(currentSong);
            if (!AudioManager.Instance.IsPlaying())
            {
                Debug.LogError($"[GameplayController] CRIWARE playback failed for song '{currentSong.title}'.");
                return;
            }

            ScoreManager.Instance.Initialize(currentChart.notes.Count);
            NoteManager.Instance.LoadChart(currentChart);
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
            if (!isPlaying || isCountingDown) return;
            isPaused = true;
            AudioManager.Instance.Pause();
            NoteManager.Instance.StopSpawning();
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(true);
        }

        private void OnResume()
        {
            if (!isPaused || isCountingDown) return;
            StartCoroutine(CountdownCoroutine());
        }

        private IEnumerator CountdownCoroutine()
        {
            isCountingDown = true;
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);

            for (int i = 3; i > 0; i--)
            {
                countdownDisplay = i.ToString();
                yield return new WaitForSecondsRealtime(1f);
            }
            countdownDisplay = "";
            isCountingDown = false;

            isPaused = false;
            AudioManager.Instance.Resume();
            NoteManager.Instance.StartSpawning();
        }

        private void OnRestart()
        {
            AudioManager.Instance.StopSong();
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        private void OnQuitToMenu()
        {
            AudioManager.Instance.StopSong();
            GameStateManager.Instance.ChangeScene(GameScene.MainMenu);
        }

        private void OnGUI()
        {
            if (!isCountingDown || string.IsNullOrEmpty(countdownDisplay)) return;

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 120,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), countdownDisplay, style);
        }
    }
}
