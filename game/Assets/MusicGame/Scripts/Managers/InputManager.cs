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

        // One physical head motion = one flick event. The current burst direction
        // is latched here and cleared only after motion settles below threshold,
        // so a single swing cannot judge several notes in a row.
        private bool hasActiveFlick;
        private FlickDirection activeFlickDirection;
        private bool activeFlickConsumed;

        public int CurrentHoldValue => holdProvider?.GetHoldValue() ?? 0;
        public Vector3 CurrentAngularVelocity => headMotionProvider?.GetAngularVelocity() ?? Vector3.zero;
        public bool IsHoldProviderActive => holdProvider?.IsActive() ?? false;
        public bool IsHeadMotionProviderActive => headMotionProvider?.IsActive() ?? false;
        public float FlickThreshold => headMotionProvider?.GetFlickThreshold() ?? flickThreshold;

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

        private void Update()
        {
            UpdateFlickState();
        }

        private void UpdateFlickState()
        {
            if (!TryComputeFlickDirection(out FlickDirection direction))
            {
                // Motion settled below threshold: the burst ended, arm the next flick.
                hasActiveFlick = false;
                activeFlickConsumed = false;
                return;
            }

            if (!hasActiveFlick || activeFlickDirection != direction)
            {
                // New burst (or direction change mid-motion) = a fresh flick event.
                hasActiveFlick = true;
                activeFlickDirection = direction;
                activeFlickConsumed = false;
            }
        }

        /// <summary>
        /// Consumes the current flick event if it matches the expected direction.
        /// Each physical motion burst can judge exactly one note; the next flick
        /// requires the angular velocity to settle below threshold first.
        /// </summary>
        public bool TryConsumeFlick(FlickDirection expected)
        {
            if (!hasActiveFlick || activeFlickConsumed || activeFlickDirection != expected)
                return false;

            activeFlickConsumed = true;
            return true;
        }

        /// <summary>
        /// Detects flick direction based on angular velocity.
        /// </summary>
        public FlickDirection DetectFlickDirection()
        {
            return TryComputeFlickDirection(out FlickDirection direction)
                ? direction
                : (FlickDirection)(-1); // Invalid
        }

        private bool TryComputeFlickDirection(out FlickDirection direction)
        {
            direction = (FlickDirection)(-1);
            Vector3 angularVel = CurrentAngularVelocity;
            float threshold = FlickThreshold;

            if (Mathf.Abs(angularVel.x) < threshold && Mathf.Abs(angularVel.y) < threshold)
                return false;

            if (Mathf.Abs(angularVel.x) > Mathf.Abs(angularVel.y))
                direction = angularVel.x > 0 ? FlickDirection.Up : FlickDirection.Down;
            else
                direction = angularVel.y > 0 ? FlickDirection.Right : FlickDirection.Left;
            return true;
        }
    }
}
