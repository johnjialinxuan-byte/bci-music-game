using UnityEngine;
using UnityEngine.UI;
using MusicGame.Core;

namespace MusicGame.Scenes
{
    public class AboutController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text aboutText;
        [SerializeField] private Button backButton;

        private void Start()
        {
            if (aboutText != null)
            {
                aboutText.text = "Music Game\n\n" +
                    "A Unity 2D Rhythm Game with pseudo-3D spatial presentation.\n\n" +
                    "Powered by CRIWARE Audio Middleware.\n\n" +
                    "Developed for BCI Music Game Project.\n\n" +
                    "v0.1.0";
            }
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnBackClicked()
        {
            GameStateManager.Instance.ChangeScene(GameScene.MainMenu);
        }
    }
}
