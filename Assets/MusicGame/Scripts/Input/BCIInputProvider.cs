using UnityEngine;
using MusicGame.Managers;

namespace MusicGame.Input
{
    /// <summary>
    /// BCI 脑机接口数据提供者：将 IPC 通信中的 attention / gyroscope 数据
    /// 映射到游戏的 HoldValue 和 HeadMotion 输入接口。
    /// </summary>
    public class BCIInputProvider : MonoBehaviour, IHoldValueProvider, IHeadMotionProvider
    {
        [Header("IPC 引用")]
        [SerializeField] private IPC ipc;

        [Header("Gyroscope 映射（如果方向反了请勾选 Invert）")]
        [Tooltip("BCI gyroscope_x 对应左右摇头。若实际方向与 Flick 相反则勾选")]
        [SerializeField] private bool invertX = false;
        [Tooltip("BCI gyroscope_y 对应上下点头。若实际方向与 Flick 相反则勾选")]
        [SerializeField] private bool invertY = false;
        [Tooltip("角速度放大倍数，数值越大越容易触发 Flick")]
        [SerializeField] private float gyroScale = 1f;

        [Header("Flick 检测阈值")]
        [Tooltip("角速度绝对值超过此阈值才判定为有效 Flick")]
        

        private bool hasGyroReference;
        private Vector3 gyroReference;
[SerializeField] private float flickThreshold = 1.5f;

        public float FlickThreshold => flickThreshold;

        void Start()
        {
            if (ipc == null)
                ipc = FindObjectOfType<IPC>();

            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetHoldProvider(this);
                InputManager.Instance.SetHeadMotionProvider(this);
                Debug.Log("[BCIInputProvider] 已注册到 InputManager");
            }
            else
            {
                Debug.LogWarning("[BCIInputProvider] InputManager 未找到，请确保场景中有 InputManager");
            }
        }

        // ---------- IHoldValueProvider ----------

        /// <summary>
        /// 返回当前 attention 注意力值 (0~100)，由 BCI 平台实时推送。
        /// HoldNote 会将此值与 NoteData.threshold 比较进行判定。
        /// </summary>
        public int GetHoldValue()
        {
            if (ipc == null) return 0;
            return ipc.AttentionValue;
        }

        public bool IsActive()
        {
            return ipc != null && ipc.IsConnected;
        }

        // ---------- IHeadMotionProvider ----------

        public Quaternion GetHeadRotation()
        {
            // 暂不需要绝对朝向，返回 identity
            return Quaternion.identity;
        }

        /// <summary>
        /// 返回当前角速度，映射关系：
        ///   BCI gyroscope_y (俯仰/上下点头) → angularVel.x (控制 Up/Down Flick)
        ///   BCI gyroscope_x (偏航/左右摇头) → angularVel.y (控制 Left/Right Flick)
        /// </summary>
        public Vector3 GetAngularVelocity()
        {
            if (ipc == null) return Vector3.zero;

            Vector3 sample = ipc.GyroscopeValue;
            if (!hasGyroReference)
            {
                // Input is queried before the first IPC packet arrives; do not calibrate on its zero default.
                if (sample.sqrMagnitude < 0.0001f)
                    return Vector3.zero;

                gyroReference = sample;
                hasGyroReference = true;
                return Vector3.zero;
            }

            // The platform reports orientation around a non-zero neutral pose.
            // Use motion away from the captured neutral pose for directional flick input.
            Vector3 motion = sample - gyroReference;
            float upDown = motion.y * (invertY ? -1f : 1f) * gyroScale;
            float leftRight = motion.x * (invertX ? -1f : 1f) * gyroScale;
            return new Vector3(upDown, leftRight, 0f);
        }
    }
}
