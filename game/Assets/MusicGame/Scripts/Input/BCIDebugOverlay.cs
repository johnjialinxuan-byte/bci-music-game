using UnityEngine;

namespace MusicGame.Input
{
    /// <summary>
    /// 【临时调试】实时显示 BCI 专注度与陀螺仪数据。
    /// 直接挂到场景任意 GameObject 上即可，删除本脚本与 GameObject 对项目无任何影响。
    /// </summary>
    public class BCIDebugOverlay : MonoBehaviour
    {
        [Header("IPC 引用（空则自动查找）")]
        [SerializeField] private IPC ipc;

        [Header("显示设置")]
        [SerializeField] private Vector2 position = new Vector2(10, 10);
        [SerializeField] private int fontSize = 20;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.5f);

        private GUIStyle style;

        void Start()
        {
            if (ipc == null)
                ipc = FindAnyObjectByType<IPC>();
        }

        void OnGUI()
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fontSize,
                    normal = { textColor = textColor }
                };
            }

            string attText = ipc != null ? $"Attention: {ipc.AttentionValue}" : "Attention: (No IPC)";
            string gyroText = ipc != null
                ? $"Gyro: {ipc.GyroscopeValue.x:F2}, {ipc.GyroscopeValue.y:F2}, {ipc.GyroscopeValue.z:F2}"
                : "Gyro: (No IPC)";

            // What the judgment actually sees: mapped angular rate vs threshold,
            // plus the currently latched direction — for calibrating flickThreshold.
            string rateText = "Rate: (No InputManager)";
            string flickText = "Flick: -";
            var input = MusicGame.Managers.InputManager.Instance;
            if (input != null)
            {
                Vector3 rate = input.CurrentAngularVelocity;
                rateText = $"Rate: x={rate.x:F2} y={rate.y:F2} (thr {input.FlickThreshold:F2})";
                var dir = input.DetectFlickDirection();
                flickText = $"Flick: {((int)dir < 0 ? "-" : dir.ToString())}";
            }

            string content = $"{attText}\n{gyroText}\n{rateText}\n{flickText}";

            Vector2 size = style.CalcSize(new GUIContent(content));
            float lineHeight = style.lineHeight;
            float height = lineHeight * 4.4f;

            GUI.color = backgroundColor;
            GUI.DrawTexture(new Rect(position.x, position.y, size.x + 20, height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(position.x + 10, position.y + 5, size.x + 10, height), content, style);
        }
    }
}
