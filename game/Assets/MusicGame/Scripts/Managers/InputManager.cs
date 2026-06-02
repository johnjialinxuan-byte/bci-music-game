using UnityEngine;
using MusicGame.Core;

namespace MusicGame.Managers
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Flick 检测")]
        [Tooltip("角速度绝对值超过此阈值才判定为有效 Flick。BCI 模式下建议根据 gyroScale 同步调整")]
        [SerializeField] private float flickThreshold = 0.25f;

        private Input.IHoldValueProvider holdProvider;
        private Input.IHeadMotionProvider headMotionProvider;

        public int CurrentHoldValue => holdProvider?.GetHoldValue() ?? 0;
        public Vector3 CurrentAngularVelocity => headMotionProvider?.GetAngularVelocity() ?? Vector3.zero;
        public bool IsHoldProviderActive => holdProvider?.IsActive() ?? false;
        public bool IsHeadMotionProviderActive => headMotionProvider?.IsActive() ?? false;
        public float FlickThreshold => flickThreshold;

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

        public void SetHoldProvider(Input.IHoldValueProvider provider)
        {
            holdProvider = provider;
        }

        public void SetHeadMotionProvider(Input.IHeadMotionProvider provider)
        {
            headMotionProvider = provider;
        }

        /// <summary>
        /// Detects flick direction based on angular velocity.
        /// </summary>
        public FlickDirection DetectFlickDirection()
        {
            Vector3 angularVel = CurrentAngularVelocity;

            if (Mathf.Abs(angularVel.x) < flickThreshold && Mathf.Abs(angularVel.y) < flickThreshold)
                return (FlickDirection)(-1); // Invalid

            if (Mathf.Abs(angularVel.x) > Mathf.Abs(angularVel.y))
            {
                return angularVel.x > 0 ? FlickDirection.Up : FlickDirection.Down;
            }
            else
            {
                return angularVel.y > 0 ? FlickDirection.Right : FlickDirection.Left;
            }
        }
    }
}
