using UnityEngine;
using UnityEngine.UI;
using MusicGame.Core;
using MusicGame.UI;

namespace MusicGame.Scenes
{
    public class AboutController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text aboutText;
        [SerializeField] private Sprite backArrowSprite;

        private void Start()
        {
            if (aboutText != null)
            {
                aboutText.text = "BCI music game\n\n" +
                    "A Unity 2D Rhythm Game with pseudo-3D spatial presentation\n\n" +
                    "Powered by CRIWARE Audio Middleware\n\n" +
                    "developed by students from TJU\n\n" +
                    "Music provided by [乌鸦Producer]\n\n" +
                    "v0.1.1";
            }
            SetupBackButton();
        }

private void SetupBackButton()
        {
            GameObject existing = GameObject.Find("BackButton");
            if (existing != null)
                Destroy(existing);

            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            GameObject backObj = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            backObj.transform.SetParent(canvas.transform, false);

            RectTransform rect = backObj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(-680f, 394f);
            rect.sizeDelta = new Vector2(174f, 56f);

            Button button = backObj.GetComponent<Button>();
            PillButtonStyle.Apply(button, PillButtonStyle.Cyan);
            Text label = PillButtonStyle.CreateLabel(backObj.transform, "<  BACK", 19);
            SongItemHoverEffect hover = backObj.AddComponent<SongItemHoverEffect>();
            hover.SetLabel(label);
            backObj.AddComponent<ButtonSFX>();
            button.onClick.AddListener(OnBackClicked);
        }

        private void OnBackClicked()
        {
            GameStateManager.Instance.ChangeScene(GameScene.MainMenu);
        }
    }
}
