using UnityEngine;
using UnityEngine.InputSystem;

namespace MusicGame.Input
{
    public class DemoHeadMotionProvider : MonoBehaviour, IHeadMotionProvider
    {
        [Tooltip("Mouse and mobile sensor flick threshold in rad/s.")]
        [SerializeField] private float flickThreshold = 0.25f;

        private Quaternion lastRotation;
        private Vector3 angularVelocity;
        private Vector3 lastAcceleration;
        private Vector3 mobileAngularVelocity;
        private float lastTime;

        private void OnEnable()
        {
            EnableMobileSensors();
        }

        private void Start()
        {
            EnableMobileSensors();
            lastRotation = Quaternion.identity;
            lastAcceleration = ReadAcceleration();
            lastTime = Time.unscaledTime;
        }

        private void Update()
        {
            if (Application.isMobilePlatform)
            {
                UpdateMobileMotion();
                return;
            }

            float deltaTime = Time.unscaledTime - lastTime;
            if (deltaTime > 0.001f)
            {
                Quaternion currentRotation = GetHeadRotation();
                Quaternion deltaRotation = currentRotation * Quaternion.Inverse(lastRotation);
                deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
                angularVelocity = axis * (angle * Mathf.Deg2Rad / deltaTime);
                lastRotation = currentRotation;
                lastTime = Time.unscaledTime;
            }
        }

        public Quaternion GetHeadRotation()
        {
            if (Application.isMobilePlatform)
                return Quaternion.identity;

            Vector2 delta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            float yaw = delta.x * 0.02f;
            float pitch = -delta.y * 0.02f;
            return Quaternion.Euler(pitch, yaw, 0f);
        }

        public Vector3 GetAngularVelocity()
        {
            if (Application.isMobilePlatform)
                return mobileAngularVelocity;

            // Game convention: x>0 = head flicking up, y>0 = head flicking right.
            return new Vector3(-angularVelocity.x, angularVelocity.y, 0f);
        }

        public bool IsActive()
        {
            return true;
        }

        public float GetFlickThreshold()
        {
            return flickThreshold;
        }

        private void UpdateMobileMotion()
        {
            float now = Time.unscaledTime;
            float deltaTime = Mathf.Clamp(now - lastTime, 0.01f, 0.1f);
            lastTime = now;

            Vector3 gyro = ReadGyroAngularVelocity();
            Vector3 acceleration = ReadAcceleration();
            Vector3 accelerationRate = (acceleration - lastAcceleration) / deltaTime;
            lastAcceleration = acceleration;

            Vector3 mapped = new Vector3(-gyro.x, gyro.y, 0f);
            if (mapped.sqrMagnitude < 0.0001f)
                mapped = new Vector3(-accelerationRate.x, accelerationRate.y, 0f) * 0.22f;

            mobileAngularVelocity = Vector3.Lerp(mobileAngularVelocity, mapped, 0.55f);
        }

        private static void EnableMobileSensors()
        {
#if ENABLE_INPUT_SYSTEM
            if (Accelerometer.current != null)
                InputSystem.EnableDevice(Accelerometer.current);
            if (UnityEngine.InputSystem.Gyroscope.current != null)
                InputSystem.EnableDevice(UnityEngine.InputSystem.Gyroscope.current);
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (SystemInfo.supportsGyroscope)
                global::UnityEngine.Input.gyro.enabled = true;
#endif
        }

        private static Vector3 ReadGyroAngularVelocity()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Gyroscope.current != null)
                return UnityEngine.InputSystem.Gyroscope.current.angularVelocity.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (SystemInfo.supportsGyroscope)
                return global::UnityEngine.Input.gyro.rotationRateUnbiased;
#endif
            return Vector3.zero;
        }

        private static Vector3 ReadAcceleration()
        {
#if ENABLE_INPUT_SYSTEM
            if (Accelerometer.current != null)
                return Accelerometer.current.acceleration.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return global::UnityEngine.Input.acceleration;
#else
            return Vector3.zero;
#endif
        }
    }
}
