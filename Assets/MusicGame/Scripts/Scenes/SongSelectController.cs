using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MusicGame.Core;

namespace MusicGame.Scenes
{
    public class SongSelectController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform songListContent;
        [SerializeField] private GameObject songItemPrefab;
        [SerializeField] private Button backButton;
        [SerializeField] private Button playPreviewButton;
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
            if (playPreviewButton != null)
                playPreviewButton.onClick.AddListener(OnPlayPreview);
            if (easyButton != null)
                easyButton.onClick.AddListener(() => OnDifficultySelected(Difficulty.Easy));
            if (normalButton != null)
                normalButton.onClick.AddListener(() => OnDifficultySelected(Difficulty.Normal));
            if (hardButton != null)
                hardButton.onClick.AddListener(() => OnDifficultySelected(Difficulty.Hard));

            LoadSongs();
        }

        private void LoadSongs()
        {
            foreach (var item in songItems)
            {
                Destroy(item);
            }
            songItems.Clear();

            if (availableSongs.Count == 0)
            {
                SongData[] songs = Resources.LoadAll<SongData>("Songs");
                availableSongs.AddRange(songs);
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

        private void CreateSongItem(SongData song)
        {
            if (songItemPrefab == null || songListContent == null) return;

            GameObject item = Instantiate(songItemPrefab, songListContent);
            Button btn = item.GetComponent<Button>();
            Text txt = item.GetComponentInChildren<Text>();
            if (txt != null) txt.text = song.title;
            if (btn != null)
            {
                SongData capturedSong = song;
                btn.onClick.AddListener(() => OnSongSelected(capturedSong));
            }
            songItems.Add(item);
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

            Audio.CriAudioManager.Instance.StopSong();
        }

        private void OnPlayPreview()
        {
            if (selectedSong == null) return;
            Audio.CriAudioManager.Instance.PlaySong(selectedSong.cueSheetName, selectedSong.cueName);
        }

        private void OnDifficultySelected(Difficulty difficulty)
        {
            if (selectedSong == null) return;
            GameStateManager.Instance.SetSelectedDifficulty(difficulty);
            Audio.CriAudioManager.Instance.StopSong();
            GameStateManager.Instance.ChangeScene(GameScene.Gameplay);
        }

        private void OnBackClicked()
        {
            Audio.CriAudioManager.Instance.StopSong();
            GameStateManager.Instance.ChangeScene(GameScene.MainMenu);
        }
    }
}
