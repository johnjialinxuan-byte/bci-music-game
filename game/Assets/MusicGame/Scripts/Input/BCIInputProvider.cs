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
        [Tooltip("角速度绝对值超过此阈值才判定为有效 Flick。单位 = 平台姿态单位/秒（若平台为角度制，典型甩头约 50~200，建议 30 起调；弧度制则约 0.5）")]
        [SerializeField] private float flickThreshold = 30f;

        [Tooltip("平台 gyroscope 数据语义：OrientationPose = 姿态角（默认，对相邻包做差分求角速度）；AngularVelocity = 平台直接给角速度（原样使用）")]
        [SerializeField] private GyroDataMode gyroDataMode = GyroDataMode.OrientationPose;

        [Tooltip("一次差分速率的保鲜时长（秒）。超过未收到新包则归零，防止旧数据持续触发判定")]
        [SerializeField] private float rateHoldSeconds = 0.25f;

        public enum GyroDataMode
        {
            OrientationPose,
            AngularVelocity
        }

        private Vector3 lastSample;
        private bool hasLastSample;
        private float lastSampleTime;
        private Vector3 currentRate;
        private float rateExpireTime;

        public float FlickThreshold => flickThreshold;

        void Start()
        {
            if (ipc == null)
                ipc = FindAnyObjectByType<IPC>();

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

        public float GetFlickThreshold()
        {
            return flickThreshold;
        }

        // ---------- IHeadMotionProvider ----------

        public Quaternion GetHeadRotation()
        {
            // 暂不需要绝对朝向，返回 identity
            return Quaternion.identity;
        }

        private void Update()
        {
            if (ipc == null) return;
            ProcessGyroSample(ipc.GyroscopeValue);
        }

        // The platform reports orientation as pose angles around a non-zero
        // neutral. The old code compared against a fixed "neutral reference",
        // which meant a head HELD tilted read as永远在甩头 (false positives) and,
        // combined with the flick consume/hysteresis, blocked re-arming for real
        // flicks (false negatives). Differentiating consecutive packets instead
        // yields true motion: still head → 0 at any pose; flick → short spike.
        private void ProcessGyroSample(Vector3 sample)
        {
            if (!hasLastSample)
            {
                // Ignore the zero default before the first real packet arrives.
                if (sample.sqrMagnitude < 0.0001f)
                    return;

                lastSample = sample;
                hasLastSample = true;
                lastSampleTime = Time.unscaledTime;
                return;
            }

            if ((sample - lastSample).sqrMagnitude > 1e-8f)
            {
                float now = Time.unscaledTime;
                float dt = Mathf.Clamp(now - lastSampleTime, 0.02f, 0.3f);
                currentRate = gyroDataMode == GyroDataMode.AngularVelocity
                    ? sample
                    : (sample - lastSample) / dt;
                lastSample = sample;
                lastSampleTime = now;
                rateExpireTime = now + Mathf.Max(0.05f, rateHoldSeconds);
            }
            else if (Time.unscaledTime > rateExpireTime)
            {
                // No fresh packet: stale motion must not keep triggering judgments.
                currentRate = Vector3.zero;
            }
        }

        /// <summary>
        /// 返回当前角速度（由相邻陀螺仪数据包差分得到），映射关系：
        ///   BCI gyroscope_y (俯仰/上下点头) → angularVel.x (控制 Up/Down Flick)
        ///   BCI gyroscope_x (偏航/左右摇头) → angularVel.y (控制 Left/Right Flick)
        /// </summary>
        public Vector3 GetAngularVelocity()
        {
            if (ipc == null || !hasLastSample) return Vector3.zero;

            float upDown = currentRate.y * (invertY ? -1f : 1f) * gyroScale;
            float leftRight = currentRate.x * (invertX ? -1f : 1f) * gyroScale;
            return new Vector3(upDown, leftRight, 0f);
        }
    }
}
