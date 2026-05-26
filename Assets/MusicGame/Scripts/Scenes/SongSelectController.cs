using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MusicGame.Core;
using MusicGame.UI;

namespace MusicGame.Scenes
{
    public class SongSelectController : MonoBehaviour
    {
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
        [SerializeField, Range(0f, 1f)] private float pillFillAlpha = 0.24f;
        [SerializeField] private float pillBorderWidth = 2.5f;

        [Header("Data")]
        [SerializeField] private List<SongData> availableSongs = new List<SongData>();

        private SongData selectedSong;
        private bool isStartingGameplay;
        private List<GameObject> songItems = new List<GameObject>();
        private readonly Dictionary<SongData, SongItemHoverEffect> songItemEffects = new Dictionary<SongData, SongItemHoverEffect>();
        private Sprite pillButtonSprite;

        private void Start()
        {
            EnsureConfirmButton();
            
            ConfigureBackButton();
ConfigureDifficultyButtons();

            if (backButton != null)
            {
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
            if (GameStateManager.Instance != null)
                UpdateCoverFrameColor(GameStateManager.Instance.SelectedDifficulty);
            LoadSongs();
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
            Text txt = item.GetComponentInChildren<Text>(true);
            if (txt != null)
            {
                txt.text = song.title;
                txt.enabled = true;
                txt.color = Color.white;
                txt.fontSize = 24;
                txt.alignment = TextAnchor.MiddleLeft;

                RectTransform textRect = txt.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = new Vector2(24f, 0f);
                    textRect.offsetMax = new Vector2(-24f, 0f);
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
            hoverEffect.SetLabel(txt);
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
                if (text.name == "SongSelectTitle")
                {
                    text.text = "\u9009\u62e9\u97f3\u4e50";
                    AddGlow(text, 42, new Color(0.05f, 0.95f, 1f, 0.85f));
                }
                else if (text.name == "SongListHeader")
                {
                    AddGlow(text, 30, new Color(0.05f, 0.95f, 1f, 0.85f));
                }
                else if (text.name == "CollectionLabel")
                {
                    text.text = "";
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
            selectedSong = song;
            GameStateManager.Instance.SetSelectedSong(song);
            UpdateSelectedSongEffect();

            if (titleText != null) titleText.text = song.title;
            if (artistText != null) artistText.text = song.artist;
            if (coverImage != null) coverImage.sprite = song.coverImage;
            if (GameStateManager.Instance != null)
                UpdateCoverFrameColor(GameStateManager.Instance.SelectedDifficulty);

            Audio.AudioManager.Instance.StopSong();
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
            Audio.AudioManager.Instance?.StopSong();
            GameStateManager.Instance.ChangeScene(GameScene.Gameplay);
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
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 20;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
        }

        private void ConfigureDifficultyButtons()
        {
            ConfigurePillButton(easyButton, easyButtonColor, new Vector2(120f, -226f), new Vector2(148f, 62f));
            ConfigurePillButton(normalButton, normalButtonColor, new Vector2(286f, -226f), new Vector2(148f, 62f));
            ConfigurePillButton(hardButton, hardButtonColor, new Vector2(452f, -226f), new Vector2(148f, 62f));
            ConfigurePillButton(confirmButton, confirmButtonColor, new Vector2(286f, -302f), new Vector2(232f, 62f));
        }

private void ConfigureBackButton()
        {
            if (backButton == null) return;

            RectTransform rect = backButton.GetComponent<RectTransform>();
            if (rect != null)
                rect.sizeDelta = new Vector2(174f, 56f);

            PillButtonStyle.Apply(backButton, PillButtonStyle.Cyan);
            Text label = backButton.GetComponentInChildren<Text>(true);
            if (label == null)
                label = PillButtonStyle.CreateLabel(backButton.transform, "<  BACK", 19);
            else
            {
                label.text = "<  BACK";
                label.fontSize = 19;
                label.color = Color.white;
            }

            SongItemHoverEffect hover = backButton.GetComponent<SongItemHoverEffect>();
            if (hover == null)
                hover = backButton.gameObject.AddComponent<SongItemHoverEffect>();
            hover.SetLabel(label);
        }


        private void ConfigurePillButton(Button button, Color color, Vector2 position, Vector2 size)
        {
            if (button == null) return;

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
                image.sprite = GetPillButtonSprite();
                image.type = Image.Type.Sliced;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.25f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(color.r, color.g, color.b, 0.35f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        private Sprite GetPillButtonSprite()
        {
            if (pillButtonSprite != null) return pillButtonSprite;

            const int width = 128;
            const int height = 48;
            const float radius = height * 0.5f;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "RuntimePillButtonTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float centerX = x < radius ? radius : x > width - radius ? width - radius : x;
                    float centerY = radius;
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    float outerAlpha = Mathf.Clamp01(radius - distance + 0.5f);
                    float borderMask = Mathf.Clamp01(distance - (radius - pillBorderWidth) + 0.5f);
                    float alpha = outerAlpha * Mathf.Lerp(pillFillAlpha, 1f, borderMask);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            pillButtonSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            pillButtonSprite.name = "RuntimePillButtonSprite";
            return pillButtonSprite;
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
            Audio.AudioManager.Instance.StopSong();
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
    }
}
