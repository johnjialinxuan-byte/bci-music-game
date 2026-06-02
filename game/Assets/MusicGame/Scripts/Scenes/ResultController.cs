using UnityEngine;
using UnityEngine.UI;
using MusicGame.Core;

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

            DisplayResult();
        }

        private void DisplayResult()
        {
            if (coverImage == null)
            {
                GameObject coverObject = GameObject.Find("CoverImage");
                if (coverObject != null)
                {
                    coverImage = coverObject.GetComponent<Image>();
                    coverImage.transform.SetAsFirstSibling();
                }
            }

            if (ScoreManager.Instance == null)
            {
                Debug.LogError("[ResultController] ScoreManager not found!");
                return;
            }

            SongData song = GameStateManager.Instance.SelectedSong;
            if (resultTitleText != null)
                resultTitleText.text = song != null ? song.title : "Result";

            if (coverImage != null && song != null)
            {
                coverImage.sprite = song.coverImage;
                coverImage.color = new Color(1f, 1f, 1f, 0.65f);
                coverImage.preserveAspect = true;
            }


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
            GameStateManager.Instance.ChangeScene(GameScene.MainMenu);
        }
    }
}
