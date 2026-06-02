using UnityEngine;
using UnityEngine.UI;
using MusicGame.Core;

namespace MusicGame.Scenes
{
    public class DifficultySelectController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button easyButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Text songTitleText;
        [SerializeField] private Text difficultyInfoText;

        private void Start()
        {
            if (easyButton != null)
                easyButton.onClick.AddListener(() => OnDifficultySelected(Difficulty.Easy));
            if (normalButton != null)
                normalButton.onClick.AddListener(() => OnDifficultySelected(Difficulty.Normal));
            if (hardButton != null)
                hardButton.onClick.AddListener(() => OnDifficultySelected(Difficulty.Hard));
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            UpdateSongInfo();
        }

        private void UpdateSongInfo()
        {
            SongData song = GameStateManager.Instance.SelectedSong;
            if (song != null && songTitleText != null)
            {
                songTitleText.text = song.title;
            }

            UpdateDifficultyInfo(Difficulty.Normal);
        }

        private void UpdateDifficultyInfo(Difficulty difficulty)
        {
            SongData song = GameStateManager.Instance.SelectedSong;
            if (song == null) return;

            string path = song.GetChartPath(difficulty);
            if (difficultyInfoText != null)
            {
                difficultyInfoText.text = $"{difficulty}\nChart: {path}";
            }
        }

        private void OnDifficultySelected(Difficulty difficulty)
        {
            GameStateManager.Instance.SetSelectedDifficulty(difficulty);
            Audio.AudioManager.Instance.StopSong();
            GameStateManager.Instance.ChangeScene(GameScene.Gameplay);
        }

        private void OnBackClicked()
        {
            GameStateManager.Instance.ChangeScene(GameScene.SongSelect);
        }
    }
}
