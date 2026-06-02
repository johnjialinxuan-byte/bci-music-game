using UnityEngine;

namespace MusicGame.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Canvases")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private Camera uiCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ShowPanel(GameObject panel)
        {
            if (panel != null)
                panel.SetActive(true);
        }

        public void HidePanel(GameObject panel)
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }
}
