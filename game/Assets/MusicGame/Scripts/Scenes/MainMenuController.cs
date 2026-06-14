using UnityEngine;
using UnityEngine.UI;
using MusicGame.Core;
using MusicGame.UI;

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
            ConfigurePresentation();
            SetButtonLabel(startButton, "\u5f00\u59cb");
            SetButtonLabel(settingsButton, "\u8bbe\u7f6e");
            SetButtonLabel(aboutButton, "\u5173\u4e8e");
            SetButtonLabel(quitButton, "\u9000\u51fa");

            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
            if (aboutButton != null)
                aboutButton.onClick.AddListener(OnAboutClicked);
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
        }

private void ConfigurePresentation()
        {
            Canvas canvas = startButton != null ? startButton.GetComponentInParent<Canvas>() : null;
            if (canvas == null) return;

            Transform title = canvas.transform.Find("Title");
            if (title != null)
                title.gameObject.SetActive(false);

            Image background = canvas.transform.Find("StartBackground")?.GetComponent<Image>();
            if (background == null)
            {
                GameObject backgroundObject = new GameObject("StartBackground");
                backgroundObject.transform.SetParent(canvas.transform, false);
                background = backgroundObject.AddComponent<Image>();
            }

            RectTransform backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            background.sprite = Resources.Load<Sprite>("Images/start");
            if (background.sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>("Images/start");
                if (texture != null)
                {
                    background.sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                }
            }
            background.preserveAspect = false;
            background.raycastTarget = false;
            background.transform.SetAsFirstSibling();

            StartMenuMotionBackground motionBackground = canvas.GetComponent<StartMenuMotionBackground>();
            if (motionBackground == null)
                motionBackground = canvas.gameObject.AddComponent<StartMenuMotionBackground>();
            motionBackground.SetLayerSiblingIndex(background.transform.GetSiblingIndex() + 1);
            motionBackground.EnsureBuilt();

            StyleMenuButton(startButton, new Vector2(-400f, -20f));
            StyleMenuButton(settingsButton, new Vector2(-400f, -100f));
            StyleMenuButton(aboutButton, new Vector2(-400f, -180f));
            StyleMenuButton(quitButton, new Vector2(-400f, -260f));
            UIThemeFont.ApplyAll(canvas.transform);
        }

private static void StyleMenuButton(Button button, Vector2 position)
        {
            if (button == null) return;
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300f, 76f);

            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = Color.clear;
            button.transition = Selectable.Transition.None;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label == null) return;

            label.font = UIThemeFont.Font;
            label.fontSize = 40;
            label.fontStyle = FontStyle.Normal;
            label.color = Color.white;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Outline outline = label.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = false;

            SongItemHoverEffect hoverEffect = button.GetComponent<SongItemHoverEffect>();
            if (hoverEffect == null)
                hoverEffect = button.gameObject.AddComponent<SongItemHoverEffect>();
            hoverEffect.SetLabel(label);

            ButtonSFX sfx = button.GetComponent<ButtonSFX>();
            if (sfx == null)
                button.gameObject.AddComponent<ButtonSFX>();
        }

        private static void ReplaceLabelArt(Button button, string resourcePath)
        {
            if (button == null) return;
            Text label = button.GetComponentInChildren<Text>(true);
            UI.TextArt.ReplaceWithSprite(label, resourcePath);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null) return;

            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null)
                text.text = label;
        }

private void OnStartClicked()
        {
            if (PlayerPrefs.GetInt("TutorialStartedOnce", 0) == 0 && PlayerPrefs.GetInt("TutorialCompleted", 0) == 0)
            {
                SongData[] songs = Resources.LoadAll<SongData>("Songs");
                System.Array.Sort(songs, (a, b) => string.Compare(a != null ? a.songId : string.Empty, b != null ? b.songId : string.Empty, System.StringComparison.OrdinalIgnoreCase));
                SongData tutorialSong = songs.Length > 0 ? songs[0] : null;
                if (tutorialSong != null)
                {
                    PlayerPrefs.SetInt("TutorialStartedOnce", 1);
                    PlayerPrefs.Save();
                    GameStateManager.Instance.BeginTutorial(tutorialSong);
                    return;
                }
            }

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
