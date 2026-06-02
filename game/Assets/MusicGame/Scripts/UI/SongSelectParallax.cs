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
        [SerializeField] private float mobileTiltSensitivity = 2.2f;
        [SerializeField] private float mobileDeadZone = 0.025f;
        [SerializeField] private Vector2 maximumOffset = new Vector2(24f, 14f);

        private readonly List<LayerTarget> targets = new List<LayerTarget>();
        private Vector2 smoothedInput;
        private Vector3 mobileNeutralAcceleration;
        private bool mobileCalibrated;

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            if (Accelerometer.current != null)
                InputSystem.EnableDevice(Accelerometer.current);
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

            Vector3 delta = ReadAcceleration() - mobileNeutralAcceleration;
            float x = ApplyDeadZone(delta.x * mobileTiltSensitivity);
            float y = ApplyDeadZone(delta.y * mobileTiltSensitivity);
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

        private float ApplyDeadZone(float value)
        {
            return Mathf.Abs(value) < mobileDeadZone ? 0f : value;
        }


    }
}

