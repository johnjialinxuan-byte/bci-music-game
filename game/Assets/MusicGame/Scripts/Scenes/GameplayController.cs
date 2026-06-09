using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using MusicGame.Core;
using MusicGame.Audio;
using MusicGame.Gameplay;
using MusicGame.Managers;
using MusicGame.UI;

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
        private int displayedCombo;
        private float comboPulseTimer;
        private const float ComboPulseDuration = 0.2f;
        private const float ComboPulseScale = 0.28f;

        private void Start()
        {
            SetupScene();
            InitializeGameplay();
        }

        private void SetupScene()
        {
            ConfigureHudDisplay();
            ConfigureGameplayBackground();


            if (pauseButton != null)
                pauseButton.onClick.AddListener(OnPause);
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResume);
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitToMenu);
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestart);

            ConfigurePauseMenuPanel();


            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);

            // [TEMP] Auto-create IPC + BCIInputProvider + BCIDebugOverlay via weak references.
            // Removing any of these scripts later will not break compilation.
            System.Type ipcType = System.Type.GetType("IPC, Assembly-CSharp");
            if (ipcType != null)
            {
                Object ipcObj = FindAnyObjectByType(ipcType);
                if (ipcObj == null)
                {
                    GameObject ipcGo = new GameObject("IPC (TEMP)");
                    ipcGo.AddComponent(ipcType);
                    Debug.Log("[GameplayController] Auto-created IPC GameObject.");
                }
            }

            System.Type bciInputType = System.Type.GetType("MusicGame.Input.BCIInputProvider, Assembly-CSharp");
            if (bciInputType != null && FindAnyObjectByType(bciInputType) == null)
            {
                GameObject bciGo = new GameObject("BCIInputProvider (TEMP)");
                Object comp = bciGo.AddComponent(bciInputType);

                // Try to wire up IPC reference via reflection
                System.Type ipcType2 = System.Type.GetType("IPC, Assembly-CSharp");
                if (ipcType2 != null)
                {
                    Object ipcInstance = FindAnyObjectByType(ipcType2);
                    if (ipcInstance != null)
                    {
                        var field = bciInputType.GetField("ipc", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (field != null) field.SetValue(comp, ipcInstance);
                    }
                }
                Debug.Log("[GameplayController] Auto-created BCIInputProvider GameObject.");
            }

            System.Type bciType = System.Type.GetType("MusicGame.Input.BCIDebugOverlay, Assembly-CSharp");
            if (bciType != null && FindAnyObjectByType(bciType) == null)
            {
                GameObject debugGo = new GameObject("BCIDebugOverlay (TEMP)");
                debugGo.AddComponent(bciType);
            }
        }

        private void ConfigureHudDisplay()
        {
            ConfigureCounterText(scoreText, new Vector2(-28f, -18f), TextAnchor.MiddleRight);
            ConfigureCounterText(accuracyText, new Vector2(-28f, -94f), TextAnchor.MiddleRight);
            ConfigurePauseMenuButton(resumeButton);
            ConfigurePauseMenuButton(restartButton);
            ConfigurePauseMenuButton(quitButton);

            if (comboText != null)
            {
                RectTransform rect = comboText.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -20f);
                rect.sizeDelta = new Vector2(460f, 94f);
                rect.localScale = Vector3.one;

                comboText.text = "COMBO  0";
                comboText.fontSize = 56;
                comboText.fontStyle = FontStyle.Bold;
                comboText.alignment = TextAnchor.MiddleCenter;
                comboText.color = new Color(0.24f, 0.94f, 1f, 1f);

                Outline outline = comboText.GetComponent<Outline>();
                if (outline != null)
                    Destroy(outline);
            }

            if (pauseButton != null)
            {
                RectTransform rect = pauseButton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(28f, -18f);
                rect.sizeDelta = new Vector2(120f, 94f);

                Text pauseLabel = pauseButton.GetComponentInChildren<Text>(true);
                if (pauseLabel != null)
                {
                    pauseLabel.fontSize = 56;
                    pauseLabel.fontStyle = FontStyle.Bold;
                }
            }
        }

        private static void ConfigureCounterText(Text text, Vector2 position, TextAnchor alignment)
        {
            if (text == null) return;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(500f, 76f);
            text.fontSize = 42;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = Color.white;
        }

private static void ConfigurePauseMenuButton(Button button)
        {
            if (button == null) return;

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320f, 74f);

            Image image = button.GetComponent<Image>();
            if (image == null)
                image = button.gameObject.AddComponent<Image>();
            image.sprite = PillButtonStyle.GetSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.02f, 0.08f, 0.12f, 0.86f);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.rectTransform.sizeDelta = rect.sizeDelta;
                label.fontSize = 34;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
            }

            SongItemHoverEffect hover = button.GetComponent<SongItemHoverEffect>();
            if (hover == null)
                hover = button.gameObject.AddComponent<SongItemHoverEffect>();
            hover.SetLabel(label);
            hover.SetBackgroundGraphic(image);
            hover.SetHoverColor(new Color(0.18f, 0.95f, 1f, 1f));
            hover.SetHoverScale(1.06f);

            if (button.GetComponent<ButtonSFX>() == null)
                button.gameObject.AddComponent<ButtonSFX>();
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
            

            int attentionThreshold = GameplaySettings.GetAttentionThreshold(GameStateManager.Instance.SelectedDifficulty);
            foreach (NoteData note in currentChart.notes)
            {
                if (note.noteType == NoteType.Hold)
                    note.threshold = attentionThreshold;
            }
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

            int totalJudgments = currentChart.notes.Count;
            foreach (NoteData note in currentChart.notes)
            {
                if (note.HasTailFlick)
                    totalJudgments++;
            }
            ScoreManager.Instance.Initialize(totalJudgments);
            NoteManager.Instance.LoadChart(currentChart);

            StartCoroutine(GameStartCountdownCoroutine());
        }

        private void Update()
        {
            if (!isPlaying || isPaused) return;
            UpdateUI();
            AnimateCombo();
            CheckGameEnd();
        }

        private void UpdateUI()
        {
            if (scoreText != null)
                scoreText.text = $"SCORE  {ScoreManager.Instance.Score}";
            if (comboText != null)
            {
                int currentCombo = ScoreManager.Instance.Combo;
                comboText.text = $"COMBO  {currentCombo}";
                if (currentCombo > displayedCombo)
                    comboPulseTimer = ComboPulseDuration;
                displayedCombo = currentCombo;
            }
            if (accuracyText != null)
                accuracyText.text = $"ACC  {ScoreManager.Instance.Accuracy:F1}%";
        }

        private void AnimateCombo()
        {
            if (comboText == null) return;

            float scale = 1f;
            if (comboPulseTimer > 0f)
            {
                comboPulseTimer -= Time.unscaledDeltaTime;
                float progress = 1f - Mathf.Clamp01(comboPulseTimer / ComboPulseDuration);
                scale += Mathf.Sin(progress * Mathf.PI) * ComboPulseScale;
            }

            comboText.rectTransform.localScale = Vector3.one * scale;
        }

        private void CheckGameEnd()
        {
            bool audioStillPlaying = AudioManager.Instance.IsPlaying();
            if (audioStillPlaying && !AudioManager.Instance.IsCurrentCueIndefinite) return;
            if (NoteManager.Instance.HasUnspawnedNotes || NoteManager.Instance.HasActiveNotes) return;
            if (audioStillPlaying && !AudioManager.Instance.HasReachedCurrentCueSinglePlaybackEnd) return;

            if (audioStillPlaying)
                AudioManager.Instance.StopSong();

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
                StartCoroutine(ShowPauseMenuCoroutine());
        }

        private void OnResume()
        {
            if (!isPaused || isCountingDown) return;
            StartCoroutine(CountdownCoroutine());
        }

        private IEnumerator GameStartCountdownCoroutine()
        {
            isCountingDown = true;
            for (int i = 3; i > 0; i--)
            {
                countdownDisplay = i.ToString();
                yield return new WaitForSecondsRealtime(1f);
            }
            countdownDisplay = "";
            isCountingDown = false;

            AudioManager.Instance.PlaySong(currentSong);
            if (!AudioManager.Instance.IsPlaying())
            {
                Debug.LogError($"[GameplayController] CRIWARE playback failed for song '{currentSong.title}'.");
                yield break;
            }

            NoteManager.Instance.StartSpawning();
            isPlaying = true;
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
            GameStateManager.Instance.ChangeScene(GameScene.SongSelect);
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
    

private void ConfigureGameplayBackground()
        {
            Canvas gameplayCanvas = GetComponentInParent<Canvas>();
            if (gameplayCanvas == null)
                gameplayCanvas = FindAnyObjectByType<Canvas>();
            if (gameplayCanvas == null) return;

            SciFiCurveBackground background = gameplayCanvas.GetComponent<SciFiCurveBackground>();
            if (background == null)
                background = gameplayCanvas.gameObject.AddComponent<SciFiCurveBackground>();

            background.ConfigurePerspectiveFlow(
                12,
                36,
                1700f,
                900f,
                58f,
                1.8f,
                0.68f,
                new Color(0.05f, 0.95f, 1f, 0.095f),
                new Color(0.55f, 0.25f, 1f, 0.075f));
        }


private void ConfigurePauseMenuPanel()
        {
            if (pauseMenuPanel == null) return;

            RectTransform rect = pauseMenuPanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(620f, 430f);
            }

            Image panelImage = pauseMenuPanel.GetComponent<Image>();
            if (panelImage == null)
                panelImage = pauseMenuPanel.AddComponent<Image>();
            panelImage.sprite = PillButtonStyle.GetSprite();
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.02f, 0.08f, 0.12f, 0.86f);
            panelImage.raycastTarget = true;

            CanvasGroup group = pauseMenuPanel.GetComponent<CanvasGroup>();
            if (group == null)
                group = pauseMenuPanel.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;

            PositionPauseMenuButton(resumeButton, 92f);
            PositionPauseMenuButton(restartButton, 0f);
            PositionPauseMenuButton(quitButton, -92f);
            SetButtonLabel(resumeButton, "继续");
            SetButtonLabel(restartButton, "重新开始");
            SetButtonLabel(quitButton, "返回选歌");

            Text[] labels = pauseMenuPanel.GetComponentsInChildren<Text>(true);
            foreach (Text label in labels)
            {
                label.color = Color.white;
                label.fontStyle = FontStyle.Bold;
            }
        }

        private IEnumerator ShowPauseMenuCoroutine()
        {
            if (pauseMenuPanel == null) yield break;

            pauseMenuPanel.SetActive(true);
            CanvasGroup group = pauseMenuPanel.GetComponent<CanvasGroup>();
            RectTransform rect = pauseMenuPanel.GetComponent<RectTransform>();
            if (group == null || rect == null) yield break;

            group.alpha = 0f;
            rect.localScale = Vector3.one * 0.94f;
            float elapsed = 0f;
            const float duration = 0.18f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                group.alpha = t;
                rect.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, t);
                yield return null;
            }

            group.alpha = 1f;
            rect.localScale = Vector3.one;
        }


private static void PositionPauseMenuButton(Button button, float y)
        {
            if (button == null) return;

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(320f, 74f);
        }

private static void SetButtonLabel(Button button, string text)
        {
            if (button == null) return;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label == null) return;

            label.text = text;
            label.color = Color.white;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
        }

}
}
