using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MusicGame.Core;
using MusicGame.UI;

namespace MusicGame.Scenes
{
    public class SongSelectController : MonoBehaviour
    {
        private const float PanelGap = 24f;
        private const float ButtonGap = 18f;
        private const float SongListX = -400f;
        private const float DetailCenterX = 360f;
        private const float SongItemWidth = 600f;
        private const float SongItemHeight = 72f;
        private const float DifficultyButtonWidth = 150f;
        private const float DifficultyButtonHeight = 60f;
        private const float ConfirmButtonSize = 112f;
        private const float HeaderCenterY = -56f;
        private const float HeaderBackX = 96f;
        private const float HeaderTitleX = 360f;

        [Header("UI References")]
        [SerializeField] private Transform songListContent;
        [SerializeField] private GameObject songItemPrefab;
        [SerializeField] private Button backButton;
        [SerializeField] private Image coverFrame;
        [SerializeField] private Image coverImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text artistText;
        [SerializeField] private Button easyButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;
        [SerializeField] private Button confirmButton;

        [Header("Difficulty Colors")]
        [SerializeField] private Color easyFrameColor = new Color(0.20f, 0.92f, 0.55f, 1f);
        [SerializeField] private Color normalFrameColor = new Color(0.20f, 0.86f, 1f, 1f);
        [SerializeField] private Color hardFrameColor = new Color(1f, 0.24f, 0.40f, 1f);
        [SerializeField] private Color easyButtonColor = new Color(0.18f, 0.72f, 0.36f, 1f);
        [SerializeField] private Color normalButtonColor = new Color(0.12f, 0.39f, 0.94f, 1f);
        [SerializeField] private Color hardButtonColor = new Color(0.84f, 0.14f, 0.22f, 1f);
        [SerializeField] private Color confirmButtonColor = new Color32(0x39, 0xC5, 0xBB, 0xFF);

        [Header("Data")]
        [SerializeField] private List<SongData> availableSongs = new List<SongData>();

        private SongData selectedSong;
        private bool isStartingGameplay;
        private List<GameObject> songItems = new List<GameObject>();
        private readonly Dictionary<SongData, SongItemHoverEffect> songItemEffects = new Dictionary<SongData, SongItemHoverEffect>();
        private Sprite rectButtonSprite;

        private void Start()
        {
            EnsureAudioManager();

            EnsureConfirmButton();
            ConfigureTopBand();
            ConfigureLayout();
            ConfigureDifficultyButtons();

            if (backButton != null)
            {
                ConfigureBackButton(backButton);

                backButton.onClick.AddListener(OnBackClicked);
                if (backButton.GetComponent<ButtonSFX>() == null)
                    backButton.gameObject.AddComponent<ButtonSFX>();
            }
            if (easyButton != null)
            {
                easyButton.onClick.AddListener(() => OnDifficultySelected(Difficulty.Easy));
                if (easyButton.GetComponent<ButtonSFX>() == null)
                    easyButton.gameObject.AddComponent<ButtonSFX>();
            }
            if (normalButton != null)
            {
                normalButton.onClick.AddListener(() => OnDifficultySelected(Difficulty.Normal));
                if (normalButton.GetComponent<ButtonSFX>() == null)
                    normalButton.gameObject.AddComponent<ButtonSFX>();
            }
            if (hardButton != null)
            {
                hardButton.onClick.AddListener(() => OnDifficultySelected(Difficulty.Hard));
                if (hardButton.GetComponent<ButtonSFX>() == null)
                    hardButton.gameObject.AddComponent<ButtonSFX>();
            }
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
                if (confirmButton.GetComponent<ButtonSFX>() == null)
                    confirmButton.gameObject.AddComponent<ButtonSFX>();
            }

            RemoveSongListClipping();
            ConfigureHeaderEffects();
            ConfigureParallax();
            if (GameStateManager.Instance != null)
                UpdateCoverFrameColor(GameStateManager.Instance.SelectedDifficulty);
            LoadSongs();

            // Apply the flat button art + right-edge confirm bar last, so nothing
            // earlier in Start overwrites them.
            StyleButtons();
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
                UIThemeFont.ApplyAll(canvas.transform);
        }

        private void LoadSongs()
        {
            foreach (var item in songItems)
            {
                Destroy(item);
            }
            songItems.Clear();
            songItemEffects.Clear();

            SongData[] songs = Resources.LoadAll<SongData>("Songs");
            foreach (SongData song in songs)
            {
                if (song == null || availableSongs.Exists(existing => existing != null && existing.songId == song.songId))
                    continue;
                availableSongs.Add(song);
            }

            // Fallback: create test song if no songs found
            if (availableSongs.Count == 0)
            {
                CreateTestSong();
            }

            availableSongs.Sort((a, b) => string.Compare(a.songId, b.songId, System.StringComparison.OrdinalIgnoreCase));

            foreach (var song in availableSongs)
            {
                CreateSongItem(song);
            }

            if (availableSongs.Count > 0)
            {
                SelectSong(availableSongs[0]);
            }
        }

        private void CreateTestSong()
        {
            SongData testSong = ScriptableObject.CreateInstance<SongData>();
            testSong.songId = "test_01";
            testSong.title = "Test Song";
            testSong.artist = "Test Artist";
            testSong.bpm = 120f;
            testSong.previewStartTime = 0f;
            testSong.cueSheetName = "";
            testSong.cueName = "";
            testSong.easyChartPath = "MusicGame/Resources/TestChart_Easy";
            testSong.normalChartPath = "MusicGame/Resources/TestChart_Easy";
            testSong.hardChartPath = "MusicGame/Resources/TestChart_Easy";
            
            // Load cover image from Resources
            Sprite cover = Resources.Load<Sprite>("Images/Covers/2077");
            if (cover == null)
            {
                // Try loading as Texture2D and creating sprite
                Texture2D tex = Resources.Load<Texture2D>("Images/Covers/2077");
                if (tex != null)
                {
                    cover = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
            testSong.coverImage = cover;
            
            availableSongs.Add(testSong);
            Debug.Log("[SongSelectController] Created test song.");
        }

        private void CreateSongItem(SongData song)
        {
            if (songItemPrefab == null || songListContent == null) return;

            GameObject item = Instantiate(songItemPrefab, songListContent);
            item.name = $"SongItem_{song.title}";
            Button btn = item.GetComponent<Button>();
            RectTransform itemRect = item.GetComponent<RectTransform>();
            if (itemRect != null)
            {
                itemRect.anchorMin = new Vector2(0f, 1f);
                itemRect.anchorMax = new Vector2(0f, 1f);
                itemRect.pivot = new Vector2(0f, 1f);
                itemRect.sizeDelta = new Vector2(SongItemWidth, SongItemHeight);
            }

            LayoutElement layoutElement = item.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = item.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = SongItemWidth;
            layoutElement.minWidth = SongItemWidth;
            layoutElement.preferredHeight = SongItemHeight;
            layoutElement.minHeight = SongItemHeight;

            Text txt = item.GetComponentInChildren<Text>(true);
            Image itemImage = item.GetComponent<Image>();
            if (itemImage != null)
            {
                itemImage.sprite = GetRectButtonSprite();
                itemImage.type = Image.Type.Sliced;
                itemImage.color = new Color(0.02f, 0.08f, 0.12f, 0.78f);
            }

            
if (txt != null)
            {
                UIThemeFont.Apply(txt);
                txt.text = song.title;
                txt.enabled = true;
                txt.color = Color.white;
                txt.fontSize = 28;
                txt.resizeTextForBestFit = true;
                txt.resizeTextMinSize = 18;
                txt.resizeTextMaxSize = 28;
                txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                txt.verticalOverflow = VerticalWrapMode.Truncate;
                txt.alignment = TextAnchor.MiddleLeft;

                RectTransform textRect = txt.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = new Vector2(34f, 0f);
                    textRect.offsetMax = new Vector2(-34f, 0f);
                }
            }

            if (btn != null)
            {
                SongData capturedSong = song;
                btn.onClick.AddListener(() => OnSongSelected(capturedSong));
            }

            SongItemHoverEffect hoverEffect = item.GetComponent<SongItemHoverEffect>();
            if (hoverEffect == null)
                hoverEffect = item.AddComponent<SongItemHoverEffect>();
            hoverEffect.SetGraphic(txt);
            hoverEffect.SetBackgroundGraphic(itemImage);
            hoverEffect.SetScaleTargets(false, true);
            hoverEffect.SetHoverScale(1.16f);
            hoverEffect.SetHoverColor(new Color(0.28f, 0.93f, 1f, 1f));
            songItemEffects[song] = hoverEffect;

            ButtonSFX sfx = item.GetComponent<ButtonSFX>();
            if (sfx == null)
                item.AddComponent<ButtonSFX>();

            songItems.Add(item);
        }

private void ConfigureHeaderEffects()
        {
            Text[] texts = GetComponentsInChildren<Text>(true);
            foreach (Text text in texts)
            {
                string normalized = text.text != null ? text.text.Replace(" ", string.Empty).Replace("_", string.Empty).ToUpperInvariant() : string.Empty;
                if (text.name == "SongSelectTitle")
                {
                    text.text = "\u9009\u62e9\u97f3\u4e50";
                    RectTransform titleRect = text.rectTransform;
                    titleRect.anchorMin = new Vector2(0f, 1f);
                    titleRect.anchorMax = new Vector2(0f, 1f);
                    titleRect.pivot = new Vector2(0.5f, 0.5f);
                    titleRect.anchoredPosition = SafeAreaUtility.TopLeft(new Vector2(HeaderTitleX, HeaderCenterY), 0f, 0f);
                    titleRect.sizeDelta = new Vector2(420f, 70f);
                    UI.TextArt.ReplaceWithSprite(text, "Images/Titles/title_songselect");
                }
                else if (text.name == "SongListHeader" || normalized == "TRACKLIST" || normalized == "SONGLIST")
                {
                    text.text = string.Empty;
                    text.enabled = false;
                    text.gameObject.SetActive(false);
                }
                else if (text.name == "CollectionLabel")
                {
                    text.text = string.Empty;
                    text.enabled = false;
                }
            }
        }

private static void AddGlow(Text text, int fontSize, Color glowColor)
        {
            text.fontSize = fontSize;
            text.color = new Color(0.28f, 0.93f, 1f, 1f);
            Outline outline = text.GetComponent<Outline>();
            if (outline == null)
                outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = glowColor;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private void OnSongSelected(SongData song)
        {
            SelectSong(song);
        }

private void SelectSong(SongData song)
        {
            if (song == null) return;

            selectedSong = song;
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.SetSelectedSong(song);
            UpdateSelectedSongEffect();

            if (titleText != null) titleText.text = song.title;
            if (artistText != null) artistText.text = song.artist;
            ApplyCoverSprite(song);
            if (GameStateManager.Instance != null)
                UpdateCoverFrameColor(GameStateManager.Instance.SelectedDifficulty);

            EnsureAudioManager();
            Audio.AudioManager.Instance?.PlaySongPreview(song);
        }

        private void UpdateSelectedSongEffect()
        {
            foreach (KeyValuePair<SongData, SongItemHoverEffect> entry in songItemEffects)
            {
                if (entry.Value != null)
                    entry.Value.SetSelected(entry.Key == selectedSong);
            }
        }

        private void OnDifficultySelected(Difficulty difficulty)
        {
            if (isStartingGameplay || GameStateManager.Instance == null) return;

            GameStateManager.Instance.SetSelectedDifficulty(difficulty);
            UpdateCoverFrameColor(difficulty);
        }

private void OnConfirmClicked()
        {
            if (selectedSong == null || isStartingGameplay || GameStateManager.Instance == null) return;

            isStartingGameplay = true;
            Audio.AudioManager.Instance?.StopPreviewImmediate();

            Color wipeColor = new Color(confirmButtonColor.r * 0.5f, confirmButtonColor.g * 0.5f, confirmButtonColor.b * 0.5f, 1f);
            RectTransform sourceRect = confirmButton != null ? confirmButton.GetComponent<RectTransform>() : null;
            UI.ScreenWipe.PlayFrom(GameScene.Gameplay, wipeColor, sourceRect);
        }

        private void EnsureConfirmButton()
        {
            if (confirmButton != null) return;

            Transform parent = hardButton != null ? hardButton.transform.parent : transform.parent;
            if (parent == null) return;

            GameObject buttonObject = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            confirmButton = buttonObject.GetComponent<Button>();
            confirmButton.targetGraphic = buttonObject.GetComponent<Image>();

            GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text label = labelObject.GetComponent<Text>();
            label.text = "\u786e\u5b9a";
            label.font = UIThemeFont.Font;
            label.fontSize = 20;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
        }

private void ConfigureDifficultyButtons()
        {
            float rowY = -274f;
            float centerX = DetailCenterX;
            float step = DifficultyButtonWidth + ButtonGap;

            ConfigurePillButton(easyButton, easyButtonColor, new Vector2(centerX - step + DifficultyButtonWidth * 0.5f, rowY), new Vector2(DifficultyButtonWidth, DifficultyButtonHeight));
            ConfigurePillButton(normalButton, normalButtonColor, new Vector2(centerX + DifficultyButtonWidth * 0.5f, rowY), new Vector2(DifficultyButtonWidth, DifficultyButtonHeight));
            ConfigurePillButton(hardButton, hardButtonColor, new Vector2(centerX + step + DifficultyButtonWidth * 0.5f, rowY), new Vector2(DifficultyButtonWidth, DifficultyButtonHeight));
            ConfigurePillButton(confirmButton, confirmButtonColor, new Vector2(770f, rowY), new Vector2(ConfirmButtonSize, ConfirmButtonSize));

            LockButtonBackgroundAlpha(easyButton, 0.68f);
            LockButtonBackgroundAlpha(normalButton, 0.68f);
            LockButtonBackgroundAlpha(hardButton, 0.68f);
            EnsureButtonWashOverlay(easyButton, new Vector2(9f, 9f), 0.50f);
            EnsureButtonWashOverlay(normalButton, new Vector2(9f, 9f), 0.50f);
            EnsureButtonWashOverlay(hardButton, new Vector2(9f, 9f), 0.50f);

        }

private static void LockButtonBackgroundAlpha(Button button, float alpha)
        {
            if (button == null) return;

            Image image = button.GetComponent<Image>();
            if (image == null) return;

            Color color = image.color;
            color.a = alpha;
            image.color = color;

            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = color;
            colors.pressedColor = color;
            colors.selectedColor = color;
            colors.disabledColor = new Color(color.r, color.g, color.b, alpha * 0.45f);
            button.colors = colors;
            button.transition = Selectable.Transition.None;
        }

        private void ConfigurePillButton(Button button, Color color, Vector2 rightAnchoredPosition, Vector2 size)
        {
            ConfigureRectButton(button, color, true);

            RectTransform rect = button != null ? button.GetComponent<RectTransform>() : null;
            if (rect != null)
            {
                rect.pivot = new Vector2(1f, 0.5f);
                rect.anchoredPosition = rightAnchoredPosition;
                rect.sizeDelta = size;
            }

            SongItemHoverEffect hoverEffect = button != null ? button.GetComponent<SongItemHoverEffect>() : null;
            if (hoverEffect != null)
                hoverEffect.SetHoverScale(1.12f);
        }

private void ConfigureRectButton(Button button, Color color, bool filled)
        {
            if (button == null) return;

            Image image = button.GetComponent<Image>();
            if (image == null)
                image = button.gameObject.AddComponent<Image>();

            image.sprite = GetRectButtonSprite();
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = filled ? color : new Color(0.02f, 0.08f, 0.12f, 0.74f);
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = Color.Lerp(image.color, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(image.color, Color.black, 0.22f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(color.r, color.g, color.b, 0.35f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.enabled = true;
                label.fontStyle = FontStyle.Bold;
                label.color = Color.white;
                label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;

                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }

            SongItemHoverEffect hoverEffect = button.GetComponent<SongItemHoverEffect>();
            if (hoverEffect == null)
                hoverEffect = button.gameObject.AddComponent<SongItemHoverEffect>();
            hoverEffect.SetGraphic(label);
            hoverEffect.SetBackgroundGraphic(image);
            hoverEffect.SetScaleTargets(false, true);
            hoverEffect.SetHoverColor(new Color(0.28f, 0.93f, 1f, 1f));
        }

private void ConfigureBackButton(Button button)
        {
            if (button == null) return;

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = SafeAreaUtility.TopLeft(new Vector2(HeaderBackX, HeaderCenterY), 0f, 0f);
                rect.sizeDelta = new Vector2(150f, 72f);
            }

            Image image = button.GetComponent<Image>();
            if (image == null)
                image = button.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            Graphic target = null;
            Graphic[] graphics = button.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
            {
                if (graphic != image)
                {
                    target = graphic;
                    break;
                }
            }

            if (target == null)
                target = image;

            target.color = Color.white;
            SongItemHoverEffect hoverEffect = button.GetComponent<SongItemHoverEffect>();
            if (hoverEffect == null)
                hoverEffect = button.gameObject.AddComponent<SongItemHoverEffect>();
            hoverEffect.SetGraphic(target);
            hoverEffect.SetBackgroundGraphic(null);
            hoverEffect.SetScaleTargets(true, false);
            hoverEffect.SetHoverScale(1.12f);
            hoverEffect.SetHoverColor(new Color(0.28f, 0.93f, 1f, 1f));
        }



private Sprite GetPillButtonSprite()
        {
            return GetRectButtonSprite();
        }

private Sprite GetRectButtonSprite()
        {
            if (rectButtonSprite != null) return rectButtonSprite;

            const int width = 64;
            const int height = 32;
            const int border = 2;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "RuntimeRectButtonTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool edge = x < border || x >= width - border || y < border || y >= height - border;
                    pixels[y * width + x] = Color.white;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            rectButtonSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            rectButtonSprite.name = "RuntimeRectButtonSprite";
            return rectButtonSprite;
        }


private void StyleButtons()
        {
            ConfigureDifficultyButtons();
            ConfigureConfirmBar();
            RegisterConfirmButtonParallax();
        }

        // The confirm "button" becomes a translucent vertical bar on the right
        // edge — below the top band, above the bottom, inset on both ends — with
        // a stacked "确定" label and a play arrow. Clicking it runs the screen wipe.
private void ConfigureConfirmBar()
        {
            if (confirmButton == null)
                EnsureConfirmButton();
            if (confirmButton == null)
                return;

            confirmButton.onClick.RemoveListener(OnConfirmClicked);
            confirmButton.onClick.AddListener(OnConfirmClicked);
            if (confirmButton.GetComponent<ButtonSFX>() == null)
                confirmButton.gameObject.AddComponent<ButtonSFX>();

            RectTransform rect = confirmButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(-136f, 40f);
            rect.offsetMax = new Vector2(-24f, -150f);

            Image image = confirmButton.GetComponent<Image>();
            if (image == null)
                image = confirmButton.gameObject.AddComponent<Image>();
            image.sprite = GetRectButtonSprite();
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = new Color(confirmButtonColor.r, confirmButtonColor.g, confirmButtonColor.b, 0.68f);
            confirmButton.targetGraphic = image;

            ColorBlock colors = confirmButton.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = image.color;
            colors.pressedColor = image.color;
            colors.selectedColor = image.color;
            colors.disabledColor = new Color(image.color.r, image.color.g, image.color.b, 0.32f);
            colors.fadeDuration = 0.08f;
            confirmButton.colors = colors;
            confirmButton.transition = Selectable.Transition.None;

            Text label = confirmButton.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.enabled = true;
                label.text = "\u786e\n\u5b9a\n\n\u25b6";
                label.color = Color.white;
                label.fontStyle = FontStyle.Bold;
                label.fontSize = 40;
                label.lineSpacing = 1.1f;
                label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;

                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }

            EnsureButtonWashOverlay(confirmButton, new Vector2(9f, 9f), 0.50f);

            SongItemHoverEffect hover = confirmButton.GetComponent<SongItemHoverEffect>();
            if (hover == null)
                hover = confirmButton.gameObject.AddComponent<SongItemHoverEffect>();
            hover.SetGraphic(null);
            hover.SetBackgroundGraphic(image);
            hover.SetScaleTargets(false, true);
            hover.SetColorChangeEnabled(false);
            hover.SetHoverScale(1.05f);
        }

        // Swaps a button's procedural sliced fill for a flat pre-rendered art tile.
        // keepLabel=false: latin label is baked into the art, hide the runtime Text.
        // keepLabel=true: art is text-less, keep the runtime Text (white, centered).
        private void ApplyButtonArt(Button button, string spriteResourcePath, bool keepLabel)
        {
            if (button == null) return;

            Sprite sprite = Resources.Load<Sprite>(spriteResourcePath);
            if (sprite == null) return;

            Image image = button.GetComponent<Image>();
            if (image == null)
                image = button.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                if (keepLabel)
                {
                    label.enabled = true;
                    label.color = Color.white;
                    label.fontStyle = FontStyle.Bold;
                    label.fontSize = 44;
                    label.alignment = TextAnchor.MiddleCenter;
                }
                else
                {
                    label.enabled = false;
                }
            }

            // Hover scales the art; only recolor the label when we keep one.
            SongItemHoverEffect hover = button.GetComponent<SongItemHoverEffect>();
            if (hover != null)
            {
                hover.SetGraphic(keepLabel ? label : null);
                hover.SetBackgroundGraphic(image);
                hover.SetScaleTargets(false, true);
                hover.SetColorChangeEnabled(false);
                hover.SetHoverScale(1.1f);
            }
        }

        private void UpdateCoverFrameColor(Difficulty difficulty)
        {
            if (coverFrame == null) return;

            coverFrame.color = difficulty switch
            {
                Difficulty.Easy => easyFrameColor,
                Difficulty.Hard => hardFrameColor,
                _ => normalFrameColor
            };
        }

        private void OnBackClicked()
        {
            Audio.AudioManager.Instance?.StopPreviewImmediate();
            GameStateManager.Instance.ChangeScene(GameScene.MainMenu);
        }

        private void RemoveSongListClipping()
        {
            if (songListContent == null) return;

            Transform viewport = songListContent.parent;
            if (viewport == null) return;

            Mask mask = viewport.GetComponent<Mask>();
            if (mask != null)
            {
                mask.enabled = false;
            }

            RectMask2D rectMask = viewport.GetComponent<RectMask2D>();
            if (rectMask == null)
            {
                rectMask = viewport.gameObject.AddComponent<RectMask2D>();
            }
            rectMask.padding = new Vector4(-120f, 0f, -120f, 0f);
        }
    

private void ConfigureSongListLayout()
        {
            if (songListContent == null) return;

            Transform viewport = songListContent.parent;
            RectTransform viewportRect = viewport != null ? viewport.GetComponent<RectTransform>() : null;
            if (viewportRect != null)
            {
                viewportRect.anchorMin = new Vector2(0.5f, 0.5f);
                viewportRect.anchorMax = new Vector2(0.5f, 0.5f);
                viewportRect.pivot = new Vector2(0.5f, 0.5f);
                viewportRect.anchoredPosition = new Vector2(SongListX, -18f);
                viewportRect.sizeDelta = new Vector2(SongItemWidth + 28f, 560f);
            }

            RectTransform contentRect = songListContent.GetComponent<RectTransform>();
            if (contentRect != null)
            {
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(0f, 1f);
                contentRect.pivot = new Vector2(0f, 1f);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = new Vector2(SongItemWidth, Mathf.Max(contentRect.sizeDelta.y, 610f + 50f));
            }

            VerticalLayoutGroup layout = songListContent.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 18f;
                layout.padding = new RectOffset(14, 0, 26, 36);
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = false;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }
        }


private void ConfigureLayout()
        {
            ConfigureSongListLayout();

            const float detailY = 88f;

            RectTransform coverFrameRect = coverFrame != null ? coverFrame.rectTransform : null;
            if (coverFrameRect != null)
            {
                coverFrameRect.anchoredPosition = new Vector2(DetailCenterX, detailY);
                coverFrameRect.sizeDelta = new Vector2(388f, 388f);
                if (coverFrame.GetComponent<RectMask2D>() == null)
                    coverFrame.gameObject.AddComponent<RectMask2D>();

            }

            RectTransform coverImageRect = coverImage != null ? coverImage.rectTransform : null;
            if (coverImageRect != null)
            {
                if (coverFrame != null && coverImage.transform.parent == coverFrame.transform)
                    coverImageRect.anchoredPosition = Vector2.zero;
                else
                    coverImageRect.anchoredPosition = new Vector2(DetailCenterX, detailY);
                coverImageRect.sizeDelta = new Vector2(370f, 370f);

            }

            if (titleText != null)
            {
                titleText.rectTransform.anchoredPosition = new Vector2(DetailCenterX, -128f);
                titleText.rectTransform.sizeDelta = new Vector2(560f, 52f);
                titleText.fontSize = 32;
                titleText.fontStyle = FontStyle.Bold;
                titleText.alignment = TextAnchor.MiddleCenter;
            }

            if (artistText != null)
            {
                artistText.rectTransform.anchoredPosition = new Vector2(DetailCenterX, -198f);
                artistText.rectTransform.sizeDelta = new Vector2(560f, 34f);
                artistText.fontSize = 18;
                artistText.alignment = TextAnchor.MiddleCenter;
            }
        }


private void ConfigureTopBand()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null && backButton != null)
                canvas = backButton.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            Transform existing = canvas.transform.Find("TopBand");
            GameObject topBandObject = existing != null ? existing.gameObject : new GameObject("TopBand", typeof(RectTransform), typeof(Image));
            topBandObject.transform.SetParent(canvas.transform, false);
            topBandObject.transform.SetSiblingIndex(0);

            RectTransform rect = topBandObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(0f, 338f);
            rect.offsetMax = new Vector2(0f, 450f);

            Image image = topBandObject.GetComponent<Image>();
            if (image == null)
                image = topBandObject.AddComponent<Image>();
            image.color = new Color(0.02f, 0.08f, 0.12f, 0.78f);
            image.raycastTarget = false;
        }


private void ConfigureParallax()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null && backButton != null)
                canvas = backButton.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            SongSelectParallax parallax = canvas.GetComponent<SongSelectParallax>();
            if (parallax == null)
                parallax = canvas.gameObject.AddComponent<SongSelectParallax>();

            parallax.ClearTargets();

            RegisterParallaxTarget(parallax, canvas.transform.Find("SciFiCurveBackground"), new Vector2(-20f, -12f));
            RegisterParallaxTarget(parallax, canvas.transform.Find("SongListHeader"), new Vector2(6f, 3f));
            RegisterParallaxTarget(parallax, canvas.transform.Find("SongList"), new Vector2(8f, 4f));
            RegisterParallaxTarget(parallax, coverFrame != null ? coverFrame.transform : null, new Vector2(14f, 8f), true, 1.4f);
            RegisterParallaxTarget(parallax, titleText != null ? titleText.transform : null, new Vector2(12f, 7f));
            RegisterParallaxTarget(parallax, artistText != null ? artistText.transform : null, new Vector2(12f, 7f));
            RegisterParallaxTarget(parallax, easyButton != null ? easyButton.transform : null, new Vector2(5f, 3f));
            RegisterParallaxTarget(parallax, normalButton != null ? normalButton.transform : null, new Vector2(5f, 3f));
            RegisterParallaxTarget(parallax, hardButton != null ? hardButton.transform : null, new Vector2(5f, 3f));
            // The confirm bar is edge-anchored and restyled AFTER this runs; the
            // parallax would keep snapping it back to a stale base position that
            // is off-screen under the new anchors. Keep it out of the parallax.
            parallax.ResetBaseTransforms();
        }

private void RegisterConfirmButtonParallax()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null && confirmButton != null)
                canvas = confirmButton.GetComponentInParent<Canvas>();
            if (canvas == null || confirmButton == null) return;

            SongSelectParallax parallax = canvas.GetComponent<SongSelectParallax>();
            if (parallax == null)
                parallax = canvas.gameObject.AddComponent<SongSelectParallax>();

            RegisterParallaxTarget(parallax, confirmButton.transform, new Vector2(5f, 3f));
            parallax.ResetBaseTransforms();
        }


        private static void RegisterParallaxTarget(SongSelectParallax parallax, Transform target, Vector2 motion, bool tilt = false, float tiltDegrees = 0f)
        {
            if (parallax == null || target == null) return;

            RectTransform rect = target as RectTransform;
            if (rect == null)
                rect = target.GetComponent<RectTransform>();

            parallax.RegisterTarget(rect, motion, tilt, tiltDegrees);
        }


private static void EnsureAudioManager()
        {
            if (Audio.AudioManager.Instance != null) return;

            GameObject audioObject = new GameObject("AudioManager");
            audioObject.AddComponent<Audio.AudioManager>();
        }


private void ApplyCoverSprite(SongData song)
        {
            if (coverImage == null) return;

            float targetSize = 370f;
            Transform imageParent = coverImage.transform.parent;
            if (coverFrame != null)
            {
                RectTransform frameRect = coverFrame.rectTransform;
                if (frameRect.sizeDelta.x > 0f && frameRect.sizeDelta.y > 0f)
                    targetSize = Mathf.Min(frameRect.sizeDelta.x, frameRect.sizeDelta.y) - 18f;
                imageParent = EnsureCoverViewport(targetSize).transform;
            }

            if (imageParent != null && coverImage.transform.parent != imageParent)
                coverImage.transform.SetParent(imageParent, false);

            coverImage.sprite = song != null ? song.coverImage : null;
            coverImage.type = Image.Type.Simple;
            coverImage.preserveAspect = true;
            coverImage.raycastTarget = false;

            RectTransform imageRect = coverImage.rectTransform;
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = Vector2.zero;

            if (coverImage.sprite == null)
            {
                imageRect.sizeDelta = new Vector2(targetSize, targetSize);
                return;
            }

            Rect spriteRect = coverImage.sprite.rect;
            float aspect = spriteRect.width / Mathf.Max(1f, spriteRect.height);
            imageRect.sizeDelta = aspect >= 1f
                ? new Vector2(targetSize * aspect, targetSize)
                : new Vector2(targetSize, targetSize / aspect);
        }


private RectTransform EnsureCoverViewport(float size)
        {
            if (coverFrame == null) return null;

            Transform existing = coverFrame.transform.Find("CoverViewport");
            GameObject viewport = existing != null ? existing.gameObject : new GameObject("CoverViewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(coverFrame.transform, false);

            RectTransform rect = viewport.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(size, size);
            return rect;
        }


private void EnsureButtonWashOverlay(Button button, Vector2 inset, float alpha)
        {
            if (button == null) return;

            Transform existing = button.transform.Find("WashOverlay");
            GameObject overlayObject = existing != null ? existing.gameObject : new GameObject("WashOverlay", typeof(RectTransform), typeof(Image));
            overlayObject.transform.SetParent(button.transform, false);

            RectTransform rect = overlayObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = inset;
            rect.offsetMax = -inset;

            Image image = overlayObject.GetComponent<Image>();
            image.sprite = GetRectButtonSprite();
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = new Color(0f, 0f, 0f, alpha);
            image.raycastTarget = false;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
                label.transform.SetAsLastSibling();
        }
}
}
