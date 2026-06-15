using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MusicGame.UI
{
    public class SongSelectParallax : MonoBehaviour
    {
        private class LayerTarget
        {
            public RectTransform Rect;
            public Vector2 Motion;
            public bool Tilt;
            public float TiltDegrees;
            public Vector2 BasePosition;
            public Quaternion BaseRotation;
        }

        [SerializeField] private bool enableDesktopMouse = true;
        [SerializeField] private bool enableMobileTilt = true;
        [SerializeField] private float inputSmoothing = 8f;
        [SerializeField] private float mobileTiltSensitivity = 3.5f;
        [SerializeField] private float mobileGyroSensitivity = 0.08f;
        [SerializeField] private float mobileDeadZone = 0.01f;
        [SerializeField] private Vector2 maximumOffset = new Vector2(24f, 14f);

        private readonly List<LayerTarget> targets = new List<LayerTarget>();
        private Vector2 smoothedInput;
        private Vector3 mobileNeutralAcceleration;
        private Vector3 lastMobileAcceleration;
        private bool mobileCalibrated;

        private void OnEnable()
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
            CalibrateMobileTilt();
            ResetBaseTransforms();
        }

        private void LateUpdate()
        {Vector2 targetInput = ReadParallaxInput();
            float lerp = 1f - Mathf.Exp(-inputSmoothing * Time.unscaledDeltaTime);
            smoothedInput = Vector2.Lerp(smoothedInput, targetInput, lerp);

            for (int i = 0; i < targets.Count; i++)
            {
                LayerTarget target = targets[i];
                if (target == null || target.Rect == null) continue;

                Vector2 offset = new Vector2(
                    Mathf.Clamp(smoothedInput.x * target.Motion.x, -maximumOffset.x, maximumOffset.x),
                    Mathf.Clamp(smoothedInput.y * target.Motion.y, -maximumOffset.y, maximumOffset.y));

                target.Rect.anchoredPosition = target.BasePosition + offset;

                if (target.Tilt)
                {
                    float xTilt = Mathf.Clamp(-smoothedInput.y * target.TiltDegrees, -target.TiltDegrees, target.TiltDegrees);
                    float yTilt = Mathf.Clamp(smoothedInput.x * target.TiltDegrees, -target.TiltDegrees, target.TiltDegrees);
                    target.Rect.localRotation = target.BaseRotation * Quaternion.Euler(xTilt, yTilt, 0f);
                }
            }
        }

public void ClearTargets()
        {
            targets.Clear();
        }

        public void RegisterTarget(RectTransform rect, Vector2 motion, bool tilt = false, float tiltDegrees = 0f)
        {
            if (rect == null) return;

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].Rect == rect)
                {
                    targets[i].Motion = motion;
                    targets[i].Tilt = tilt;
                    targets[i].TiltDegrees = tiltDegrees;
                    targets[i].BasePosition = rect.anchoredPosition;
                    targets[i].BaseRotation = rect.localRotation;
                    return;
                }
            }

            targets.Add(new LayerTarget
            {
                Rect = rect,
                Motion = motion,
                Tilt = tilt,
                TiltDegrees = tiltDegrees,
                BasePosition = rect.anchoredPosition,
                BaseRotation = rect.localRotation
            });
        }

        public void ResetBaseTransforms()
        {
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].Rect == null) continue;
                targets[i].BasePosition = targets[i].Rect.anchoredPosition;
                targets[i].BaseRotation = targets[i].Rect.localRotation;
            }
        }

        public void CalibrateMobileTilt()
        {
            mobileNeutralAcceleration = ReadAcceleration();
            lastMobileAcceleration = mobileNeutralAcceleration;
            mobileCalibrated = true;
        }

        private Vector2 ReadParallaxInput()
        {
            if (enableMobileTilt && Application.isMobilePlatform)
                return ReadMobileTiltInput();
            if (!enableDesktopMouse || Screen.width <= 0 || Screen.height <= 0)
                return Vector2.zero;

            Vector2 mouse = ReadMousePosition();
            float x = Mathf.Clamp((mouse.x / Screen.width - 0.5f) * 2f, -1f, 1f);
            float y = Mathf.Clamp((mouse.y / Screen.height - 0.5f) * 2f, -1f, 1f);
            return new Vector2(x, y);
        }

        private Vector2 ReadMobileTiltInput()
        {
            if (!mobileCalibrated)
                CalibrateMobileTilt();

            Vector3 acceleration = ReadAcceleration();
            Vector3 delta = acceleration - mobileNeutralAcceleration;
            Vector3 accelerationRate = acceleration - lastMobileAcceleration;
            lastMobileAcceleration = acceleration;
            float x = ApplyDeadZone(delta.y * mobileTiltSensitivity);
            float y = ApplyDeadZone(-delta.x * mobileTiltSensitivity);
            Vector3 gyro = ReadGyroRotationRate();
            x += Mathf.Clamp((gyro.y + accelerationRate.y * 2.2f) * mobileGyroSensitivity, -0.45f, 0.45f);
            y += Mathf.Clamp((-gyro.x - accelerationRate.x * 2.2f) * mobileGyroSensitivity, -0.45f, 0.45f);
            return new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
        }

        private Vector2 ReadMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return global::UnityEngine.Input.mousePosition;
#else
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
#endif
        }

        private Vector3 ReadAcceleration()
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

        private Vector3 ReadGyroRotationRate()
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

        private float ApplyDeadZone(float value)
        {
            return Mathf.Abs(value) < mobileDeadZone ? 0f : value;
        }


    }

    public static class SafeAreaUtility
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;

        public static Vector2 TopLeft(Vector2 basePosition, float extraX = 0f, float extraY = 0f)
        {
            Vector4 margins = GetReferenceMargins();
            return new Vector2(basePosition.x + margins.x + extraX, basePosition.y - margins.z - extraY);
        }

        public static Vector2 TopRight(Vector2 basePosition, float extraX = 0f, float extraY = 0f)
        {
            Vector4 margins = GetReferenceMargins();
            return new Vector2(basePosition.x - margins.y - extraX, basePosition.y - margins.z - extraY);
        }

        public static Vector4 GetReferenceMargins()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
                return Vector4.zero;

            Rect safe = Screen.safeArea;
            float left = safe.xMin / Screen.width * ReferenceWidth;
            float right = (Screen.width - safe.xMax) / Screen.width * ReferenceWidth;
            float top = (Screen.height - safe.yMax) / Screen.height * ReferenceHeight;
            float bottom = safe.yMin / Screen.height * ReferenceHeight;
            return new Vector4(left, right, top, bottom);
        }
    }
}
