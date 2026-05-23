using UnityEngine;
using UnityEngine.UI;
using MusicGame.Audio;

namespace MusicGame.UI
{
    public class ButtonSFX : MonoBehaviour
    {
        [SerializeField] private string cueSheet = "cuesheet2";
        [SerializeField] private string cueName = "";

        private void Awake()
        {
            Button btn = GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(Play);
        }

        private void Play()
        {
            if (string.IsNullOrWhiteSpace(cueSheet) || string.IsNullOrWhiteSpace(cueName))
                return;

            AudioManager.Instance?.PlaySFX(cueSheet, cueName);
        }
    }
}
