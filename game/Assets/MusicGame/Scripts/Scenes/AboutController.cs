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
                    "v0.1.2";
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
            rect.anchoredPosition = new Vector2(-690f, 394f);
            rect.sizeDelta = new Vector2(135f, 52f);
            Image hitImage = backObj.GetComponent<Image>();
            hitImage.color = new Color(0.12f, 0.16f, 0.21f, 0f);

            Button button = backObj.GetComponent<Button>();
            button.targetGraphic = hitImage;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OnBackClicked);
            backObj.AddComponent<ButtonSFX>();

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(backObj.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(40f, 40f);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = backArrowSprite;
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            SongItemHoverEffect hover = backObj.AddComponent<SongItemHoverEffect>();
            hover.SetGraphic(icon);
        }

        private void OnBackClicked()
        {
            GameStateManager.Instance.ChangeScene(GameScene.MainMenu);
        }
    }
}
