using UnityEngine;
using UnityEngine.UI;
using MusicGame.Core;
using MusicGame.UI;

namespace MusicGame.Scenes
{
    public class ResultController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text resultTitleText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text maxComboText;
        [SerializeField] private Text perfectText;
        [SerializeField] private Text goodText;
        [SerializeField] private Text missText;
        [SerializeField] private Text accuracyText;
        [SerializeField] private Text rankText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button backButton;
        private Image coverImage;


        [Header("Rank Colors")]
        [SerializeField] private Color sRankColor = Color.yellow;
        [SerializeField] private Color aRankColor = Color.green;
        [SerializeField] private Color bRankColor = Color.cyan;
        [SerializeField] private Color cRankColor = Color.white;
        [SerializeField] private Color dRankColor = Color.gray;

        private void Start()
        {
            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetry);
            if (backButton != null)
                backButton.onClick.AddListener(OnBackToMenu);

            ConfigureResultVisuals();
            StartCoroutine(ShowResultPanelCoroutine());


            DisplayResult();
        }

private void DisplayResult()
        {
            if (coverImage == null)
            {
                GameObject coverObject = GameObject.Find("CoverImage") ?? GameObject.Find("CoverPerformanceArea");
                if (coverObject != null)
                    coverImage = coverObject.GetComponent<Image>();
            }

            SongData song = GameStateManager.Instance != null ? GameStateManager.Instance.SelectedSong : null;
            if (coverImage != null)
            {
                StyleCoverBackdrop(coverImage);

                Transform panel = coverImage.transform.parent != null ? coverImage.transform.parent.Find("ResultPanelStyle") : null;
                if (panel != null)
                    panel.SetSiblingIndex(Mathf.Min(1, panel.parent.childCount - 1));
                if (coverImage.transform.parent != null)
                    coverImage.transform.SetSiblingIndex(Mathf.Min(2, coverImage.transform.parent.childCount - 1));
            }

            if (ScoreManager.Instance == null)
            {
                Debug.LogError("[ResultController] ScoreManager not found!");
                return;
            }

            if (resultTitleText != null)
                resultTitleText.text = song != null ? song.title : "Result";

            if (scoreText != null)
                scoreText.text = $"Score: {ScoreManager.Instance.Score}";
            if (maxComboText != null)
                maxComboText.text = $"Max Combo: {ScoreManager.Instance.MaxCombo}";
            if (perfectText != null)
                perfectText.text = $"Perfect: {ScoreManager.Instance.PerfectCount}";
            if (goodText != null)
                goodText.text = $"Good: {ScoreManager.Instance.GoodCount}";
            if (missText != null)
                missText.text = $"Miss: {ScoreManager.Instance.MissCount}";
            if (accuracyText != null)
                accuracyText.text = $"Accuracy: {ScoreManager.Instance.Accuracy:F1}%";

            char rank = CalculateRank();
            if (rankText != null)
            {
                rankText.text = $"Rank: {rank}";
                rankText.color = GetRankColor(rank);
            }
        }

        private char CalculateRank()
        {
            float acc = ScoreManager.Instance.Accuracy;
            if (acc >= 95f) return 'S';
            if (acc >= 90f) return 'A';
            if (acc >= 80f) return 'B';
            if (acc >= 70f) return 'C';
            return 'D';
        }

        private Color GetRankColor(char rank)
        {
            return rank switch
            {
                'S' => sRankColor,
                'A' => aRankColor,
                'B' => bRankColor,
                'C' => cRankColor,
                _ => dRankColor
            };
        }

        private void OnRetry()
        {
            GameStateManager.Instance.ChangeScene(GameScene.Gameplay);
        }

private void OnBackToMenu()
        {
            GameStateManager.Instance.ChangeScene(GameScene.SongSelect);
        }
    

private void ConfigureResultVisuals()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            if (canvas.GetComponent<SciFiCurveBackground>() == null)
            {
                SciFiCurveBackground background = canvas.gameObject.AddComponent<SciFiCurveBackground>();
                background.Configure(5, 28, 1700f, 900f, 76f, 3f, 0.45f, new Color(0.05f, 0.95f, 1f, 0.28f), new Color(0.55f, 0.25f, 1f, 0.18f));
            }

            RectTransform panel = EnsureResultPanel(canvas.transform);
            StyleResultText(resultTitleText, new Vector2(0f, 238f), new Vector2(720f, 70f), 42, TextAnchor.MiddleCenter, Color.white);
            StyleResultText(rankText, new Vector2(0f, 142f), new Vector2(520f, 104f), 72, TextAnchor.MiddleCenter, new Color(0.24f, 0.94f, 1f, 1f));
            StyleResultText(scoreText, new Vector2(0f, 56f), new Vector2(640f, 56f), 34, TextAnchor.MiddleCenter, Color.white);
            StyleResultText(maxComboText, new Vector2(0f, 0f), new Vector2(640f, 52f), 30, TextAnchor.MiddleCenter, Color.white);
            StyleResultText(perfectText, new Vector2(-220f, -82f), new Vector2(220f, 52f), 28, TextAnchor.MiddleCenter, Color.white);
            StyleResultText(goodText, new Vector2(0f, -82f), new Vector2(220f, 52f), 28, TextAnchor.MiddleCenter, Color.white);
            StyleResultText(missText, new Vector2(220f, -82f), new Vector2(220f, 52f), 28, TextAnchor.MiddleCenter, Color.white);
            StyleResultText(accuracyText, new Vector2(0f, -146f), new Vector2(640f, 52f), 30, TextAnchor.MiddleCenter, Color.white);
            StyleResultButton(retryButton, new Vector2(-170f, -246f), "重试");
            StyleResultButton(backButton, new Vector2(170f, -246f), "选歌");

            if (panel != null)
                panel.SetSiblingIndex(Mathf.Min(1, panel.parent.childCount - 1));
            UIThemeFont.ApplyAll(canvas.transform);
        }

private RectTransform EnsureResultPanel(Transform parent)
        {
            Transform existing = parent.Find("ResultPanelStyle");
            GameObject panelObject = existing != null ? existing.gameObject : new GameObject("ResultPanelStyle", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            panelObject.transform.SetParent(parent, false);

            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1180f, 820f);

            Image image = panelObject.GetComponent<Image>();
            image.sprite = PillButtonStyle.GetSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.02f, 0.08f, 0.12f, 0.82f);
            image.raycastTarget = false;

            CanvasGroup group = panelObject.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            return rect;
        }

        private static void StyleResultText(Text text, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment, Color color)
        {
            if (text == null) return;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = color;
            UIThemeFont.Apply(text);
        }

private static void StyleResultButton(Button button, Vector2 position, string labelText)
        {
            if (button == null) return;

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300f, 74f);

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
                label.text = labelText;
                label.rectTransform.sizeDelta = rect.sizeDelta;
                label.fontSize = 32;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                UIThemeFont.Apply(label);
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

        private System.Collections.IEnumerator ShowResultPanelCoroutine()
        {
            yield return null;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) yield break;
            Transform panel = canvas.transform.Find("ResultPanelStyle");
            if (panel == null) yield break;

            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            RectTransform rect = panel.GetComponent<RectTransform>();
            if (group == null || rect == null) yield break;

            group.alpha = 0f;
            rect.localScale = Vector3.one * 0.94f;
            float elapsed = 0f;
            const float duration = 0.22f;
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


private static void StyleCoverBackdrop(Image image)
        {
            if (image == null) return;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1180f, 820f);
            image.sprite = PillButtonStyle.GetSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.02f, 0.08f, 0.12f, 0.86f);
            image.preserveAspect = false;
            image.raycastTarget = false;
            if (image.transform.parent != null)
                image.transform.SetSiblingIndex(Mathf.Min(2, image.transform.parent.childCount - 1));
        }
}
}
