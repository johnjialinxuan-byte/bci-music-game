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
                aboutText.text = "BCI music game\n\n" +
                    "A Unity 2D Rhythm Game with pseudo-3D spatial presentation.\n\n" +
                    "Powered by CRIWARE Audio Middleware.\n\n" +
                    "developed by students from TJU\n\n" +
                    "Music provided by [乌鸦Producer].\n\n" +
                    "v0.1.1";
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
