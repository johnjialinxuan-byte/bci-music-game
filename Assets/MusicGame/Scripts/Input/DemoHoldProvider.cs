using UnityEngine;
using UnityEngine.InputSystem;


namespace MusicGame.Input
{
    public class DemoHoldProvider : MonoBehaviour, IHoldValueProvider
    {
        [SerializeField] private Key simulateKey = Key.Space;
        [SerializeField] private int simulatedValue = 80;

                                public int GetHoldValue()
        {
            if (Keyboard.current != null && Keyboard.current[simulateKey].isPressed)
                return simulatedValue;
            return 0;
        }

        public bool IsActive()
        {
            return true;
        }
    }
}
