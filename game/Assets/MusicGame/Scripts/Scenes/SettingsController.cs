using System;
using System.Collections;

using UnityEngine;
using UnityEngine.UI;
using MusicGame.Core;
using MusicGame.UI;

namespace MusicGame.Scenes
{
    public class SettingsController : MonoBehaviour
    {
        private const float Step = 5f;
        private const float HeaderCenterY = -56f;
        private const float HeaderBackX = 96f;
        private const float HeaderTitleX = 360f;

        [SerializeField] private Sprite backArrowSprite;

        private Canvas canvas;
        private bool initialized;


private void Start()
        {
            InitializeSettings();
        }

private void InitializeSettings()
        {
            if (initialized) return;

            GameplaySettings.InitializeAttentionDefaults();
            if (!EnsureCanvas()) return;

            initialized = true;
            EnsureSongSelectBackground();
            SetupTopBar();
            ConfigureTitle();
            SetupBackButton();
            BuildTuningPanel();
            ConfigureParallax();
            UIThemeFont.ApplyAll(canvas.transform);
        }


private void ConfigureTitle()
        {
            Text title = GameObject.Find("Title")?.GetComponent<Text>();
            if (title == null) return;

            title.enabled = true;
            title.text = "\u8bbe\u7f6e";
            title.fontSize = 42;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleLeft;
            title.color = Color.white;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.verticalOverflow = VerticalWrapMode.Overflow;
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(0f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            title.rectTransform.sizeDelta = new Vector2(420f, 70f);
            title.rectTransform.anchoredPosition = SafeAreaUtility.TopLeft(new Vector2(HeaderTitleX, HeaderCenterY), 0f, 0f);

            Outline outline = title.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = false;

            Transform art = title.transform.parent != null ? title.transform.parent.Find("TitleArt_" + title.name) : null;
            if (art != null)
                Destroy(art.gameObject);
        }

        private void EnsureSongSelectBackground()
        {
            if (canvas.GetComponent<SciFiCurveBackground>() == null)
                canvas.gameObject.AddComponent<SciFiCurveBackground>();
        }

private void BuildTuningPanel()
        {
            if (!EnsureCanvas()) return;

            Transform existing = canvas.transform.Find("TuningPanel");
            if (existing != null)
                Destroy(existing.gameObject);

            GameObject panel = new GameObject("TuningPanel", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            panel.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchoredPosition = new Vector2(0f, -34f);
            panelRect.sizeDelta = new Vector2(950f, 620f);

            Image panelImage = panel.GetComponent<Image>();
            panelImage.sprite = PillButtonStyle.GetSprite();
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.02f, 0.08f, 0.12f, 0.44f);
            panelImage.raycastTarget = true;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(panel.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(24f, 24f);
            viewportRect.offsetMax = new Vector2(-48f, -24f);

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 1f);
            contentRect.anchorMax = new Vector2(0.5f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(890f, 1220f);

            ScrollRect scrollRect = panel.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.16f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.12f;
            scrollRect.scrollSensitivity = 42f;
            scrollRect.verticalScrollbar = CreateSettingsScrollbar(panel.transform);
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            contentRect.anchoredPosition = Vector2.zero;
            StartCoroutine(ResetScrollPositionNextFrame(scrollRect));

            CreateSectionFrame(content.transform, new Vector2(0f, -119f), new Vector2(860f, 214f));
            CreateHeader(content.transform, "\u8c31\u9762\u8bbe\u7f6e", -44f);
            CreateStepperRow(content.transform, "\u8c31\u9762\u5ef6\u8fdf", GameplaySettings.ChartDelayMs, -400, 400, " ms", value => GameplaySettings.ChartDelayMs = value, -98f, 1);
            CreateFloatStepperRow(content.transform, "\u8c31\u9762\u6d41\u901f", GameplaySettings.ChartSpeed, 1f, 5f, 0.1f, string.Empty, value => GameplaySettings.ChartSpeed = value, -164f);

            CreateSectionFrame(content.transform, new Vector2(0f, -398f), new Vector2(860f, 280f));
            CreateHeader(content.transform, "\u6ce8\u610f\u529b\u9608\u503c", -290f);
            CreateStepperRow(content.transform, "EASY", GameplaySettings.EasyAttention, 0, 100, string.Empty, value => GameplaySettings.EasyAttention = value, -344f);
            CreateStepperRow(content.transform, "NORMAL", GameplaySettings.NormalAttention, 0, 100, string.Empty, value => GameplaySettings.NormalAttention = value, -410f);
            CreateStepperRow(content.transform, "HARD", GameplaySettings.HardAttention, 0, 100, string.Empty, value => GameplaySettings.HardAttention = value, -476f);

            CreateSectionFrame(content.transform, new Vector2(0f, -677f), new Vector2(860f, 214f));
            CreateHeader(content.transform, "Flick \u5224\u5b9a\u8303\u56f4", -602f);
            CreateStepperRow(content.transform, "PERFECT", GameplaySettings.FlickPerfectMs, 40, 120, " ms", value => GameplaySettings.FlickPerfectMs = value, -656f);
            CreateStepperRow(content.transform, "GREAT", GameplaySettings.FlickGreatMs, 120, 500, " ms", value => GameplaySettings.FlickGreatMs = value, -722f);

            CreateCommunicationSection(content.transform, -923f);
            CreateTutorialRestartButton(content.transform, -1114f);
        }

private static void CreateHeader(Transform parent, string value, float y)
        {
            Text text = CreateText(parent, value, 21, TextAnchor.MiddleLeft);
            RectTransform rect = text.rectTransform;
            SetTopAnchor(rect);
            rect.anchoredPosition = new Vector2(-205f, y);
            rect.sizeDelta = new Vector2(360f, 34f);
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
        }

private void CreateStepperRow(Transform parent, string label, int initial, int min, int max, string suffix, Action<int> onChanged, float y, int step = (int)Step)
        {
            GameObject row = new GameObject(label + "Row", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(parent, false);

            RectTransform rowRect = row.GetComponent<RectTransform>();
            SetTopAnchor(rowRect);
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
            slider.minValue = min / (float)step;
            slider.maxValue = max / (float)step;
            slider.wholeNumbers = true;
            slider.SetValueWithoutNotify(initial / (float)step);

            Action update = () =>
            {
                int snapped = Mathf.RoundToInt(slider.value * step);
                valueText.text = snapped + suffix;
                onChanged(snapped);
            };

            slider.onValueChanged.AddListener(_ => update());
            minus.onClick.AddListener(() => slider.value -= 1f);
            plus.onClick.AddListener(() => slider.value += 1f);
            update();
        }

private void CreateFloatStepperRow(Transform parent, string label, float initial, float min, float max, float step, string suffix, Action<float> onChanged, float y)
        {
            GameObject row = new GameObject(label + "Row", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(parent, false);

            RectTransform rowRect = row.GetComponent<RectTransform>();
            SetTopAnchor(rowRect);
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
            slider.minValue = min / step;
            slider.maxValue = max / step;
            slider.wholeNumbers = true;
            slider.SetValueWithoutNotify(initial / step);

            Action update = () =>
            {
                float snapped = Mathf.Round(slider.value) * step;
                snapped = Mathf.Clamp(snapped, min, max);
                valueText.text = snapped.ToString("0.0") + suffix;
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
            button.transition = Selectable.Transition.None;

            Text text = PillButtonStyle.CreateLabel(obj.transform, label, 26);
            text.color = Color.white;
            SongItemHoverEffect hover = obj.AddComponent<SongItemHoverEffect>();
            hover.SetLabel(text);
            hover.SetHoverScale(1.06f);
            hover.SetColorChangeEnabled(false);

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
            handle.rectTransform.sizeDelta = new Vector2(12f, 10f);

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
            text.font = UIThemeFont.Font;
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

        private static void SetTopAnchor(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
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
            if (existing != null)
                Destroy(existing);

            GameObject backObj = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            backObj.transform.SetParent(canvas.transform, false);
            RectTransform backRect = backObj.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0f, 1f);
            backRect.anchorMax = new Vector2(0f, 1f);
            backRect.pivot = new Vector2(0.5f, 0.5f);
            SetRect(backRect, SafeAreaUtility.TopLeft(new Vector2(HeaderBackX, HeaderCenterY), 0f, 0f), new Vector2(150f, 72f));

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
            SetTopAnchor(rect);
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
            if (oldTopBar != null)
                Destroy(oldTopBar.gameObject);

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

        private void ConfigureParallax()
        {
            if (!EnsureCanvas()) return;

            SongSelectParallax parallax = canvas.GetComponent<SongSelectParallax>();
            if (parallax == null)
                parallax = canvas.gameObject.AddComponent<SongSelectParallax>();

            parallax.ClearTargets();
            RegisterParallaxTarget(parallax, canvas.transform.Find("SciFiCurveBackground"), new Vector2(-18f, -10f));
            RegisterParallaxTarget(parallax, canvas.transform.Find("TuningPanel"), new Vector2(7f, 4f));
            parallax.ResetBaseTransforms();
        }

        private static void RegisterParallaxTarget(SongSelectParallax parallax, Transform target, Vector2 motion)
        {
            if (parallax == null || target == null) return;

            RectTransform rect = target as RectTransform;
            if (rect == null)
                rect = target.GetComponent<RectTransform>();

            parallax.RegisterTarget(rect, motion);
        }
    

private static InputField CreateIpInput(Transform parent, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject("IpInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            obj.transform.SetParent(parent, false);
            SetRect(obj.GetComponent<RectTransform>(), position, size);

            Image image = obj.GetComponent<Image>();
            image.sprite = PillButtonStyle.GetSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.86f, 0.90f, 0.92f, 0.20f);

            Text text = CreateText(obj.transform, string.Empty, 20, TextAnchor.MiddleLeft);
            SetStretch(text.rectTransform, new Vector2(16f, 4f), new Vector2(-16f, -4f));
            text.color = Color.white;

            Text placeholder = CreateText(obj.transform, CommunicationSettings.DeviceIpv4Default, 20, TextAnchor.MiddleLeft);
            SetStretch(placeholder.rectTransform, new Vector2(16f, 4f), new Vector2(-16f, -4f));
            placeholder.color = new Color(1f, 1f, 1f, 0.42f);

            InputField input = obj.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 32;
            input.contentType = InputField.ContentType.Standard;
            input.targetGraphic = image;
            return input;
        }


private static void ApplyModeButtonState(Button button, bool selected)
        {
            if (button == null) return;
            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = selected ? new Color(0.08f, 0.42f, 0.52f, 0.92f) : new Color(0.02f, 0.08f, 0.12f, 0.78f);

            SetChildTextColor(button.transform, Color.white);
        }


private static void SetChildTextColor(Transform root, Color color)
        {
            Text[] labels = root.GetComponentsInChildren<Text>(true);
            foreach (Text label in labels)
                label.color = color;
        }


private static Button CreateModeButton(Transform parent, string label, Vector2 position)
        {
            GameObject obj = new GameObject(label + "ModeButton", typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            SetTopAnchor(rect);
            SetRect(rect, position, new Vector2(180f, 52f));

            Button button = obj.GetComponent<Button>();
            Image image = obj.GetComponent<Image>();
            image.sprite = PillButtonStyle.GetSprite();
            image.type = Image.Type.Sliced;
            button.targetGraphic = image;

            Text text = PillButtonStyle.CreateLabel(obj.transform, label, 20);
            text.color = Color.white;
            SongItemHoverEffect hover = obj.AddComponent<SongItemHoverEffect>();
            hover.SetLabel(text);
            hover.SetHoverScale(1.04f);
            hover.SetColorChangeEnabled(false);
            SetChildTextColor(obj.transform, Color.white);
            obj.AddComponent<ButtonSFX>();
            return button;
        }


private void CreateCommunicationSection(Transform parent, float centerY)
        {
            CreateSectionFrame(parent, new Vector2(0f, centerY), new Vector2(860f, 214f));
            CreateHeader(parent, "\u901a\u4fe1\u8bbe\u7f6e", centerY + 75f);

            GameObject modeRow = new GameObject("CommunicationModeRow", typeof(RectTransform), typeof(Image));
            modeRow.transform.SetParent(parent, false);
            RectTransform modeRowRect = modeRow.GetComponent<RectTransform>();
            SetTopAnchor(modeRowRect);
            SetRect(modeRowRect, new Vector2(0f, centerY + 21f), new Vector2(820f, 64f));
            Image modeRowImage = modeRow.GetComponent<Image>();
            modeRowImage.sprite = PillButtonStyle.GetSprite();
            modeRowImage.type = Image.Type.Sliced;
            modeRowImage.color = new Color(0.02f, 0.08f, 0.12f, 0.78f);

            Text modeLabel = CreateText(parent, "MODE", 20, TextAnchor.MiddleLeft);
            SetTopAnchor(modeLabel.rectTransform);
            SetRect(modeLabel.rectTransform, new Vector2(-320f, centerY + 21f), new Vector2(132f, 52f));

            Button localButton = CreateModeButton(parent, "\u672c\u5730", new Vector2(-72f, centerY + 21f));
            Button remoteButton = CreateModeButton(parent, "\u8de8\u8bbe\u5907", new Vector2(156f, centerY + 21f));

            GameObject ipRow = new GameObject("IpRow", typeof(RectTransform), typeof(Image));
            ipRow.transform.SetParent(parent, false);
            RectTransform ipRowRect = ipRow.GetComponent<RectTransform>();
            SetTopAnchor(ipRowRect);
            SetRect(ipRowRect, new Vector2(0f, centerY - 45f), new Vector2(820f, 64f));
            Image rowImage = ipRow.GetComponent<Image>();
            rowImage.sprite = PillButtonStyle.GetSprite();
            rowImage.type = Image.Type.Sliced;
            rowImage.color = new Color(0.02f, 0.08f, 0.12f, 0.78f);

            Text ipLabel = CreateText(ipRow.transform, "IP", 20, TextAnchor.MiddleLeft);
            SetRect(ipLabel.rectTransform, new Vector2(-320f, 0f), new Vector2(120f, 58f));

            InputField ipInput = CreateIpInput(ipRow.transform, new Vector2(92f, 0f), new Vector2(520f, 46f));
            ipInput.text = CommunicationSettings.RemoteIp;
            ipInput.onEndEdit.AddListener(value => CommunicationSettings.RemoteIp = value);

            Action refresh = () =>
            {
                bool remote = CommunicationSettings.Mode == CommunicationMode.Remote;
                ipRow.SetActive(remote);
                ApplyModeButtonState(localButton, !remote);
                ApplyModeButtonState(remoteButton, remote);
                SetChildTextColor(localButton.transform, Color.white);
                SetChildTextColor(remoteButton.transform, Color.white);
            };

            localButton.onClick.AddListener(() =>
            {
                CommunicationSettings.Mode = CommunicationMode.Local;
                refresh();
            });
            remoteButton.onClick.AddListener(() =>
            {
                CommunicationSettings.Mode = CommunicationMode.Remote;
                string currentText = ipInput.text != null ? ipInput.text.Trim() : string.Empty;
                if (string.IsNullOrWhiteSpace(currentText) || currentText == CommunicationSettings.DefaultRemoteIp)
                {
                    currentText = CommunicationSettings.DeviceIpv4Default;
                    ipInput.SetTextWithoutNotify(currentText);
                }
                CommunicationSettings.RemoteIp = currentText;
                refresh();
            });
            refresh();
        }


private bool EnsureCanvas()
        {
            if (canvas != null) return true;

            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();
            return canvas != null;
        }


private void OnEnable()
        {
            InitializeSettings();
        }


private static Scrollbar CreateSettingsScrollbar(Transform parent)
        {
            GameObject scrollbarObject = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarObject.transform.SetParent(parent, false);
            RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-20f, 36f);
            scrollbarRect.offsetMax = new Vector2(-10f, -36f);

            Image background = scrollbarObject.GetComponent<Image>();
            background.sprite = PillButtonStyle.GetSprite();
            background.type = Image.Type.Sliced;
            background.color = new Color(0.82f, 0.90f, 0.94f, 0.14f);

            GameObject slidingArea = new GameObject("SlidingArea", typeof(RectTransform));
            slidingArea.transform.SetParent(scrollbarObject.transform, false);
            RectTransform slidingRect = slidingArea.GetComponent<RectTransform>();
            slidingRect.anchorMin = Vector2.zero;
            slidingRect.anchorMax = Vector2.one;
            slidingRect.offsetMin = Vector2.zero;
            slidingRect.offsetMax = Vector2.zero;

            GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObject.transform.SetParent(slidingArea.transform, false);
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(10f, 90f);

            Image handle = handleObject.GetComponent<Image>();
            handle.sprite = PillButtonStyle.GetSprite();
            handle.type = Image.Type.Sliced;
            handle.color = new Color(0.28f, 0.93f, 1f, 0.72f);

            Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handle;
            scrollbar.handleRect = handleRect;
            return scrollbar;
        }


private IEnumerator ResetScrollPositionNextFrame(ScrollRect scrollRect)
        {
            yield return new WaitForEndOfFrame();
            if (scrollRect == null || scrollRect.content == null || scrollRect.viewport == null) yield break;

            Canvas.ForceUpdateCanvases();
            scrollRect.StopMovement();
            scrollRect.velocity = Vector2.zero;
            scrollRect.verticalNormalizedPosition = 1f;
            scrollRect.content.anchoredPosition = new Vector2(scrollRect.content.anchoredPosition.x, 0f);
            if (scrollRect.verticalScrollbar != null)
                scrollRect.verticalScrollbar.SetValueWithoutNotify(1f);
        }


private void CreateTutorialRestartButton(Transform parent, float centerY)
        {
            CreateSectionFrame(parent, new Vector2(0f, centerY), new Vector2(860f, 104f));

            GameObject obj = new GameObject("TutorialRestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            SetTopAnchor(rect);
            SetRect(rect, new Vector2(0f, centerY), new Vector2(820f, 64f));

            Image image = obj.GetComponent<Image>();
            image.sprite = PillButtonStyle.GetSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.02f, 0.08f, 0.12f, 0.82f);

            Button button = obj.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(OnTutorialRestartClicked);

            Text label = PillButtonStyle.CreateLabel(obj.transform, "\u8fdb\u5165\u65b0\u624b\u6559\u7a0b", 24);
            label.color = Color.white;
            SetChildTextColor(obj.transform, Color.white);

            SongItemHoverEffect hover = obj.AddComponent<SongItemHoverEffect>();
            hover.SetLabel(label);
            hover.SetHoverScale(1.035f);
            obj.AddComponent<ButtonSFX>();
        }

        private void OnTutorialRestartClicked()
        {
            PlayerPrefs.SetInt("TutorialCompleted", 0);
            PlayerPrefs.Save();

            SongData[] songs = Resources.LoadAll<SongData>("Songs");
            Array.Sort(songs, (a, b) => string.CompareOrdinal(a != null ? a.songId : string.Empty, b != null ? b.songId : string.Empty));
            SongData tutorialSong = songs.Length > 0 ? songs[0] : null;
            if (tutorialSong == null)
            {
                Debug.LogError("Cannot start tutorial: no song data found in Resources/Songs.");
                return;
            }

            GameStateManager.Instance?.BeginTutorial(tutorialSong);
        }
}
}
