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
        [SerializeField] private Image coverImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text artistText;
        [SerializeField] private Button easyButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;

        [Header("Data")]
        [SerializeField] private List<SongData> availableSongs = new List<SongData>();

        private SongData selectedSong;
        private List<GameObject> songItems = new List<GameObject>();

        private void Start()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
            if (easyButton != null)
                easyButton.onClick.AddListener(() => OnDifficultySelected(Difficulty.Easy));
            if (normalButton != null)
                normalButton.onClick.AddListener(() => OnDifficultySelected(Difficulty.Normal));
            if (hardButton != null)
                hardButton.onClick.AddListener(() => OnDifficultySelected(Difficulty.Hard));

            ConfigureHeaderEffects();
            LoadSongs();
        }

        private void LoadSongs()
        {
            foreach (var item in songItems)
            {
                Destroy(item);
            }
            songItems.Clear();

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

            songItems.Add(item);
        }

        private void ConfigureHeaderEffects()
        {
            Text[] texts = GetComponentsInChildren<Text>(true);
            foreach (Text text in texts)
            {
                if (text.name == "SongListHeader")
                {
                    AddGlow(text, 30, new Color(0.05f, 0.95f, 1f, 0.85f));
                }
                else if (text.name == "CollectionLabel")
                {
                    AddGlow(text, 28, new Color(0.35f, 0.35f, 1f, 0.8f));
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

            if (titleText != null) titleText.text = song.title;
            if (artistText != null) artistText.text = song.artist;
            if (coverImage != null) coverImage.sprite = song.coverImage;

            Audio.AudioManager.Instance.StopSong();
        }

        private void OnDifficultySelected(Difficulty difficulty)
        {
            if (selectedSong == null) return;
            GameStateManager.Instance.SetSelectedDifficulty(difficulty);
            Audio.AudioManager.Instance.StopSong();
            GameStateManager.Instance.ChangeScene(GameScene.Gameplay);
        }

        private void OnBackClicked()
        {
            Audio.AudioManager.Instance.StopSong();
            GameStateManager.Instance.ChangeScene(GameScene.MainMenu);
        }
    }
}
