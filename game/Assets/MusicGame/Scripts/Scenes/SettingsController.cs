using System;
using UnityEngine;
using UnityEngine.UI;
using MusicGame.Core;
using MusicGame.UI;

namespace MusicGame.Scenes
{
    public class SettingsController : MonoBehaviour
    {
        private const float Step = 5f;
        private Canvas canvas;
        [SerializeField] private Sprite backArrowSprite;


        private void Start()
        {
            
            GameplaySettings.InitializeAttentionDefaults();
canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            EnsureSongSelectBackground();

            SetupTopBar();
            ConfigureTitle();
            SetupBackButton();
            BuildTuningPanel();
        }

private void ConfigureTitle()
        {
            Text title = GameObject.Find("Title")?.GetComponent<Text>();
            if (title == null) return;

            title.text = "设置";
            title.fontSize = 42;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleLeft;
            title.color = Color.white;
            title.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            title.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            title.rectTransform.sizeDelta = new Vector2(460f, 64f);
            title.rectTransform.anchoredPosition = new Vector2(-230f, 394f);

            Outline outline = title.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = false;
        }

private void EnsureSongSelectBackground()
        {
            if (canvas.GetComponent<SciFiCurveBackground>() == null)
                canvas.gameObject.AddComponent<SciFiCurveBackground>();
        }


        private void BuildTuningPanel()
        {
            Transform existing = canvas.transform.Find("TuningPanel");
            if (existing != null) Destroy(existing.gameObject);

            GameObject panel = new GameObject("TuningPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchoredPosition = new Vector2(0f, -20f);
            panelRect.sizeDelta = new Vector2(950f, 620f);
            Image panelImage = panel.GetComponent<Image>();
            panelImage.sprite = PillButtonStyle.GetSprite();
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.02f, 0.08f, 0.12f, 0.44f);

            CreateSectionFrame(panel.transform, new Vector2(0f, 126f), new Vector2(860f, 304f));
            CreateHeader(panel.transform, "注意力阈值", 240f);
            CreateStepperRow(panel.transform, "EASY", GameplaySettings.EasyAttention, 0, 100, string.Empty, value => GameplaySettings.EasyAttention = value, 170f);
            CreateStepperRow(panel.transform, "NORMAL", GameplaySettings.NormalAttention, 0, 100, string.Empty, value => GameplaySettings.NormalAttention = value, 94f);
            CreateStepperRow(panel.transform, "HARD", GameplaySettings.HardAttention, 0, 100, string.Empty, value => GameplaySettings.HardAttention = value, 18f);
            CreateSectionFrame(panel.transform, new Vector2(0f, -151f), new Vector2(860f, 222f));
            CreateHeader(panel.transform, "Flick 判定范围", -70f);
            CreateStepperRow(panel.transform, "PERFECT", GameplaySettings.FlickPerfectMs, 40, 120, " ms", value => GameplaySettings.FlickPerfectMs = value, -132f);
            CreateStepperRow(panel.transform, "GREAT", GameplaySettings.FlickGreatMs, 120, 200, " ms", value => GameplaySettings.FlickGreatMs = value, -208f);
        }

private static void CreateHeader(Transform parent, string value, float y)
        {
            Text text = CreateText(parent, value, 21, TextAnchor.MiddleLeft);
            RectTransform rect = text.rectTransform;
            rect.anchoredPosition = new Vector2(-205f, y);
            rect.sizeDelta = new Vector2(350f, 34f);
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
        }

        private void CreateStepperRow(Transform parent, string label, int initial, int min, int max, string suffix, Action<int> onChanged, float y)
        {
            GameObject row = new GameObject(label + "Row", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(parent, false);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchoredPosition = new Vector2(0f, y);
            rowRect.sizeDelta = new Vector2(820f, 64f);
            Image rowImage = row.GetComponent<Image>();
            rowImage.sprite = PillButtonStyle.GetSprite();
            rowImage.type = Image.Type.Sliced;
            rowImage.color = new Color(0.02f, 0.08f, 0.12f, 0.78f);

            Text name = CreateText(row.transform, label, 20, TextAnchor.MiddleLeft);
            SetRect(name.rectTransform, new Vector2(-320f, 0f), new Vector2(132f, 60f));
            SongItemHoverEffect effect = row.AddComponent<SongItemHoverEffect>();
            effect.SetLabel(name);
            effect.SetHoverScale(1.06f);


            Button minus = CreateSmallButton(row.transform, "-", new Vector2(-178f, 0f));
            Button plus = CreateSmallButton(row.transform, "+", new Vector2(318f, 0f));
            Text valueText = CreateText(row.transform, string.Empty, 19, TextAnchor.MiddleCenter);
            SetRect(valueText.rectTransform, new Vector2(224f, 0f), new Vector2(112f, 60f));
            valueText.color = Color.white;

            Slider slider = CreateSlider(row.transform, new Vector2(30f, 0f), new Vector2(300f, 38f));
            slider.minValue = min / Step;
            slider.maxValue = max / Step;
            slider.wholeNumbers = true;
            slider.SetValueWithoutNotify(initial / Step);

            Action update = () =>
            {
                int snapped = Mathf.RoundToInt(slider.value * Step);
                valueText.text = snapped + suffix;
                onChanged(snapped);
            };
            slider.onValueChanged.AddListener(_ => update());
            minus.onClick.AddListener(() => slider.value -= 1f);
            plus.onClick.AddListener(() => slider.value += 1f);
            update();
        }

        private static Button CreateSmallButton(Transform parent, string label, Vector2 position)
        {
            GameObject obj = new GameObject(label == "+" ? "Plus" : "Minus", typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            SetRect(obj.GetComponent<RectTransform>(), position, new Vector2(50f, 44f));
            Button button = obj.GetComponent<Button>();
            PosterUIStyle.ApplyPosterButton(button, label == "+" ? PosterUIStyle.Blue : PosterUIStyle.Ink, false);
            Text text = PillButtonStyle.CreateLabel(obj.transform, label, 26);
            SongItemHoverEffect hover = obj.AddComponent<SongItemHoverEffect>();
            hover.SetLabel(text);
            hover.SetHoverScale(1.06f);

            obj.AddComponent<ButtonSFX>();
            return button;
        }

        private static Slider CreateSlider(Transform parent, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            obj.transform.SetParent(parent, false);
            SetRect(obj.GetComponent<RectTransform>(), position, size);
            Slider slider = obj.GetComponent<Slider>();

            Image background = CreateSliderImage(obj.transform, "Background", new Color(0.82f, 0.86f, 0.88f, 0.30f));
            SetStretch(background.rectTransform, new Vector2(0f, 14f), new Vector2(0f, -14f));
            Image fill = CreateSliderImage(obj.transform, "Fill", new Color(0.86f, 0.90f, 0.92f, 0.76f));
            SetStretch(fill.rectTransform, new Vector2(0f, 14f), new Vector2(0f, -14f));
            Image handle = CreateSliderImage(obj.transform, "Handle", new Color(0.92f, 0.95f, 0.96f, 1f));
            handle.rectTransform.sizeDelta = new Vector2(14f, 34f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private static Image CreateSliderImage(Transform parent, string name, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            image.color = color;
            image.sprite = PillButtonStyle.GetSprite();
            image.type = Image.Type.Sliced;
            return image;
        }

        private static Text CreateText(Transform parent, string value, int size, TextAnchor alignment)
        {
            GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            Text text = obj.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = alignment;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetStretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = min;
            rect.offsetMax = max;
        }

private void SetupBackButton()
        {
            GameObject existing = GameObject.Find("BackButton");
            if (existing != null) Destroy(existing);

            GameObject backObj = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            backObj.transform.SetParent(canvas.transform, false);
            SetRect(backObj.GetComponent<RectTransform>(), new Vector2(-690f, 394f), new Vector2(135f, 52f));
            Image hitImage = backObj.GetComponent<Image>();
            hitImage.color = Color.clear;

            Button button = backObj.GetComponent<Button>();
            button.targetGraphic = hitImage;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OnBackClicked);
            backObj.AddComponent<ButtonSFX>();

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(backObj.transform, false);
            SetRect(iconObject.GetComponent<RectTransform>(), Vector2.zero, new Vector2(40f, 40f));
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = backArrowSprite;
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        private void OnBackClicked()
        {
            GameStateManager.Instance.ChangeScene(GameScene.MainMenu);
        }
    

private static void CreateSectionFrame(Transform parent, Vector2 position, Vector2 size)
        {
            GameObject frame = new GameObject("SectionFrame", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(parent, false);
            RectTransform rect = frame.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = frame.GetComponent<Image>();
            image.sprite = PillButtonStyle.GetSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.02f, 0.08f, 0.12f, 0.30f);
            image.raycastTarget = false;
        }


private void SetupTopBar()
        {
            Transform oldTopBar = canvas.transform.Find("TopBar");
            if (oldTopBar != null) Destroy(oldTopBar.gameObject);

            Transform existing = canvas.transform.Find("TopBand");
            GameObject topBar = existing != null ? existing.gameObject : new GameObject("TopBand", typeof(RectTransform), typeof(Image));
            topBar.transform.SetParent(canvas.transform, false);
            topBar.transform.SetAsFirstSibling();

            RectTransform rect = topBar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(0f, 338f);
            rect.offsetMax = new Vector2(0f, 450f);

            Image image = topBar.GetComponent<Image>();
            if (image == null)
                image = topBar.AddComponent<Image>();
            image.color = new Color(0.02f, 0.08f, 0.12f, 0.78f);
            image.raycastTarget = false;
        }
}
}

