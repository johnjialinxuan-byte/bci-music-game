using UnityEngine;
using UnityEngine.UI;
using MusicGame.Core;

namespace MusicGame.Scenes
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button aboutButton;
        [SerializeField] private Button quitButton;

        private void Start()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
            if (aboutButton != null)
                aboutButton.onClick.AddListener(OnAboutClicked);
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnStartClicked()
        {
            GameStateManager.Instance.ChangeScene(GameScene.SongSelect);
        }

        private void OnSettingsClicked()
        {
            GameStateManager.Instance.ChangeScene(GameScene.Settings);
        }

        private void OnAboutClicked()
        {
            GameStateManager.Instance.ChangeScene(GameScene.About);
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
