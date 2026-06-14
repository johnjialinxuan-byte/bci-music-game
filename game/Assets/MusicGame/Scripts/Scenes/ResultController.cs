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
        private RectTransform resultPanel;
        private RectTransform attentionPanel;
        private Text attentionAverageText;
        private Text attentionPeakText;



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
            SongData song = GameStateManager.Instance != null ? GameStateManager.Instance.SelectedSong : null;
            ScoreManager scoreManager = ScoreManager.Instance;
            UpdateAttentionPanel();

            if (resultTitleText != null)
                resultTitleText.text = song != null ? song.title : "Result";

            if (scoreText != null)
                scoreText.text = $"Score: {(scoreManager != null ? scoreManager.Score : 0)}";
            if (maxComboText != null)
                maxComboText.text = $"Max Combo: {(scoreManager != null ? scoreManager.MaxCombo : 0)}";
            if (perfectText != null)
                perfectText.text = $"Perfect: {(scoreManager != null ? scoreManager.PerfectCount : 0)}";
            if (goodText != null)
                goodText.text = $"Good: {(scoreManager != null ? scoreManager.GoodCount : 0)}";
            if (missText != null)
                missText.text = $"Miss: {(scoreManager != null ? scoreManager.MissCount : 0)}";
            if (accuracyText != null)
                accuracyText.text = $"Accuracy: {(scoreManager != null ? scoreManager.Accuracy : 0f):F1}%";

            char rank = scoreManager != null ? CalculateRank() : 'D';
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
            RemoveLegacyCenterBackdrop(canvas.transform);


            resultPanel = EnsureResultPanel(canvas.transform);
            attentionPanel = EnsureAttentionPanel(canvas.transform);

            EnsureContentBackdrop(resultPanel, "ResultContentBackdrop", new Vector2(0f, 58f), new Vector2(590f, 560f));
            EnsureContentBackdrop(attentionPanel, "AttentionContentBackdrop", new Vector2(0f, 58f), new Vector2(590f, 560f));

            ParentToPanel(resultTitleText, resultPanel);
            ParentToPanel(rankText, resultPanel);
            ParentToPanel(scoreText, resultPanel);
            ParentToPanel(maxComboText, resultPanel);
            ParentToPanel(perfectText, resultPanel);
            ParentToPanel(goodText, resultPanel);
            ParentToPanel(missText, resultPanel);
            ParentToPanel(accuracyText, resultPanel);
            ParentToPanel(retryButton, resultPanel);
            ParentToPanel(backButton, resultPanel);

            StyleResultText(resultTitleText, new Vector2(0f, 258f), new Vector2(540f, 62f), 36, TextAnchor.MiddleCenter, Color.white);
            StyleResultText(rankText, new Vector2(0f, 164f), new Vector2(430f, 90f), 60, TextAnchor.MiddleCenter, new Color(0.24f, 0.94f, 1f, 1f));
            StyleResultText(scoreText, new Vector2(0f, 82f), new Vector2(520f, 48f), 28, TextAnchor.MiddleCenter, Color.white);
            StyleResultText(maxComboText, new Vector2(0f, 28f), new Vector2(520f, 46f), 26, TextAnchor.MiddleCenter, Color.white);
            StyleResultText(perfectText, new Vector2(-170f, -58f), new Vector2(170f, 44f), 23, TextAnchor.MiddleCenter, Color.white);
            StyleResultText(goodText, new Vector2(0f, -58f), new Vector2(170f, 44f), 23, TextAnchor.MiddleCenter, Color.white);
            StyleResultText(missText, new Vector2(170f, -58f), new Vector2(170f, 44f), 23, TextAnchor.MiddleCenter, Color.white);
            StyleResultText(accuracyText, new Vector2(0f, -130f), new Vector2(520f, 48f), 27, TextAnchor.MiddleCenter, Color.white);
            StyleResultButton(retryButton, new Vector2(-132f, -318f), "重试");
            StyleResultButton(backButton, new Vector2(132f, -318f), "选歌");

            resultPanel.SetSiblingIndex(Mathf.Min(1, resultPanel.parent.childCount - 1));
            attentionPanel.SetSiblingIndex(Mathf.Min(2, attentionPanel.parent.childCount - 1));
            ConfigureResultParallax(canvas);
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
            rect.anchoredPosition = new Vector2(-430f, 0f);
            rect.sizeDelta = new Vector2(650f, 760f);

            Image image = panelObject.GetComponent<Image>();
            image.sprite = PillButtonStyle.GetSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.02f, 0.08f, 0.12f, 0.82f);
            image.raycastTarget = false;

            CanvasGroup group = panelObject.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            return rect;
        }

private RectTransform EnsureAttentionPanel(Transform parent)
        {
            Transform existing = parent.Find("AttentionPanelStyle");
            GameObject panelObject = existing != null ? existing.gameObject : new GameObject("AttentionPanelStyle", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            panelObject.transform.SetParent(parent, false);

            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(430f, 0f);
            rect.sizeDelta = new Vector2(650f, 760f);

            Image image = panelObject.GetComponent<Image>();
            image.sprite = PillButtonStyle.GetSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.02f, 0.08f, 0.12f, 0.82f);
            image.raycastTarget = false;

            CanvasGroup group = panelObject.GetComponent<CanvasGroup>();
            group.alpha = 1f;

            Text title = EnsurePanelText(panelObject.transform, "AttentionTitle", "注意力曲线", new Vector2(0f, 292f), new Vector2(560f, 52f), 32, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;

            RectTransform chartRect = EnsureChart(panelObject.transform);
            chartRect.anchoredPosition = new Vector2(34f, 72f);
            chartRect.sizeDelta = new Vector2(468f, 330f);
            EnsureYAxisLabels(panelObject.transform, chartRect);

            attentionAverageText = EnsurePanelText(panelObject.transform, "AttentionAverage", string.Empty, new Vector2(-142f, -196f), new Vector2(260f, 46f), 25, TextAnchor.MiddleCenter);
            attentionPeakText = EnsurePanelText(panelObject.transform, "AttentionPeak", string.Empty, new Vector2(142f, -196f), new Vector2(260f, 46f), 25, TextAnchor.MiddleCenter);
            return rect;
        }

private RectTransform EnsureChart(Transform parent)
        {
            Transform existing = parent.Find("AttentionCurve");
            GameObject chartObject = existing != null ? existing.gameObject : new GameObject("AttentionCurve", typeof(RectTransform), typeof(AttentionCurveGraphic));
            chartObject.transform.SetParent(parent, false);

            RectTransform rect = chartObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            AttentionCurveGraphic graphic = chartObject.GetComponent<AttentionCurveGraphic>();
            graphic.color = Color.white;
            graphic.raycastTarget = false;
            graphic.SetAllDirty();
            return rect;
        }

        private Text EnsurePanelText(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            Transform existing = parent.Find(name);
            GameObject textObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.text = value;
            StyleResultText(text, position, size, fontSize, alignment, Color.white);
            return text;
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
            rect.sizeDelta = new Vector2(230f, 66f);

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
                label.fontSize = 30;
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


private void UpdateAttentionPanel()
        {
            if (attentionAverageText != null)
                attentionAverageText.text = $"平均值: {AttentionRunRecorder.Average:F1}";
            if (attentionPeakText != null)
                attentionPeakText.text = $"最高值: {AttentionRunRecorder.Peak}";

            AttentionCurveGraphic chart = attentionPanel != null ? attentionPanel.GetComponentInChildren<AttentionCurveGraphic>(true) : null;
            if (chart != null)
                chart.SetVerticesDirty();
        }

        private static void ParentToPanel(Text text, RectTransform panel)
        {
            if (text == null || panel == null) return;
            text.transform.SetParent(panel, false);
        }

        private static void ParentToPanel(Button button, RectTransform panel)
        {
            if (button == null || panel == null) return;
            button.transform.SetParent(panel, false);
        }

        private void ConfigureResultParallax(Canvas canvas)
        {
            if (canvas == null) return;

            SongSelectParallax parallax = canvas.GetComponent<SongSelectParallax>();
            if (parallax == null)
                parallax = canvas.gameObject.AddComponent<SongSelectParallax>();

            parallax.ClearTargets();
            parallax.RegisterTarget(resultPanel, new Vector2(7f, 4f));
            parallax.RegisterTarget(attentionPanel, new Vector2(10f, 6f));
            parallax.ResetBaseTransforms();
        }


private static RectTransform EnsureContentBackdrop(RectTransform parent, string name, Vector2 position, Vector2 size)
        {
            Transform existing = parent.Find(name);
            GameObject objectRoot = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
            objectRoot.transform.SetParent(parent, false);

            RectTransform rect = objectRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = objectRoot.GetComponent<Image>();
            image.sprite = PillButtonStyle.GetSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0f, 0f, 0f, 0.42f);
            image.raycastTarget = false;
            objectRoot.transform.SetAsFirstSibling();
            return rect;
        }

        private void EnsureYAxisLabels(Transform parent, RectTransform chartRect)
        {
            if (parent == null || chartRect == null) return;

            float yMin = chartRect.anchoredPosition.y - chartRect.sizeDelta.y * 0.5f;
            float yMax = chartRect.anchoredPosition.y + chartRect.sizeDelta.y * 0.5f;
            float labelX = chartRect.anchoredPosition.x - chartRect.sizeDelta.x * 0.5f - 34f;
            for (int i = 0; i <= 5; i++)
            {
                int value = i * 20;
                float y = Mathf.Lerp(yMin, yMax, i / 5f);
                Text label = EnsurePanelText(parent, $"AttentionYAxis{value}", value.ToString(), new Vector2(labelX, y), new Vector2(58f, 28f), 18, TextAnchor.MiddleRight);
                label.color = new Color(1f, 1f, 1f, 0.78f);
                label.fontStyle = FontStyle.Bold;
            }
        }


private static void RemoveLegacyCenterBackdrop(Transform parent)
        {
            if (parent == null) return;

            Transform legacy = parent.Find("CoverPerformanceArea");
            if (legacy == null) return;

            if (Application.isPlaying)
                Destroy(legacy.gameObject);
            else
                DestroyImmediate(legacy.gameObject);
        }
}
}
