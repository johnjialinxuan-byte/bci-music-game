using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace MusicGame.Gameplay
{
    public class GameInput : MonoBehaviour
    {
        public static GameInput Instance { get; private set; }

        public bool TouchDown { get; private set; }
        public bool TouchHold { get; private set; }
        public bool TouchUp { get; private set; }
        public Vector2 TouchPosition { get; private set; }
        public Vector2 TouchDelta { get; private set; }
        public Vector2 FlickDirection { get; private set; }
        public bool IsFlicking { get; private set; }
        [SerializeField] private float flickMinDistance = 30f;

        private Vector2 touchStartPos;
        private Vector2 lastTouchPos;
        private bool wasTouching;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            TouchDown = false;
            TouchUp = false;
            IsFlicking = false;
            FlickDirection = Vector2.zero;
            TouchDelta = Vector2.zero;

            bool isTouching = false;
            Vector2 currentPos = Vector2.zero;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                isTouching = true;
                currentPos = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            else if (Mouse.current != null)
            {
                isTouching = Mouse.current.leftButton.isPressed;
                currentPos = Mouse.current.position.ReadValue();
            }

            if (Touch.activeTouches.Count > 0)
            {
                isTouching = true;
                currentPos = Touch.activeTouches[0].screenPosition;
            }

            if (isTouching && !wasTouching)
            {
                TouchDown = true;
                touchStartPos = currentPos;
                lastTouchPos = currentPos;
            }
            else if (isTouching && wasTouching)
            {
                TouchHold = true;
                TouchDelta = currentPos - lastTouchPos;
                lastTouchPos = currentPos;

                Vector2 totalDelta = currentPos - touchStartPos;
                if (totalDelta.magnitude >= flickMinDistance)
                {
                    IsFlicking = true;
                    FlickDirection = totalDelta.normalized;
                }
            }
            else if (!isTouching && wasTouching)
            {
                TouchUp = true;
                TouchHold = false;
            }
            else
            {
                TouchHold = false;
            }

            TouchPosition = currentPos;
            wasTouching = isTouching;
        }
    }
}
