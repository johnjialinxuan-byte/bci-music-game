using UnityEngine;
using UnityEngine.UI;
using MusicGame.Core;

namespace MusicGame.Scenes
{
    public class SettingsController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider inputOffsetSlider;
        [SerializeField] private Text inputOffsetText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button calibrateButton;

        private void Start()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = 0.8f;
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = 0.8f;
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = 0.8f;
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
            if (inputOffsetSlider != null)
            {
                inputOffsetSlider.value = 0f;
                inputOffsetSlider.minValue = -200f;
                inputOffsetSlider.maxValue = 200f;
                inputOffsetSlider.onValueChanged.AddListener(OnInputOffsetChanged);
            }
            if (calibrateButton != null)
                calibrateButton.onClick.AddListener(OnCalibrate);
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnMasterVolumeChanged(float value)
        {
            Debug.Log($"[Settings] Master Volume: {value}");
        }

        private void OnMusicVolumeChanged(float value)
        {
            Debug.Log($"[Settings] Music Volume: {value}");
        }

        private void OnSFXVolumeChanged(float value)
        {
            Debug.Log($"[Settings] SFX Volume: {value}");
        }

        private void OnInputOffsetChanged(float value)
        {
            if (inputOffsetText != null)
                inputOffsetText.text = $"Offset: {value:F0}ms";
        }

        private void OnCalibrate()
        {
            Debug.Log("[Settings] Input calibration placeholder.");
        }

        private void OnBackClicked()
        {
            GameStateManager.Instance.ChangeScene(GameScene.MainMenu);
        }
    }
}
