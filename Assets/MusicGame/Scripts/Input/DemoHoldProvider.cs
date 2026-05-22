using UnityEngine;

namespace MusicGame.Input
{
    public class DemoHoldProvider : MonoBehaviour, IHoldValueProvider
    {
        [SerializeField] private KeyCode simulateKey = KeyCode.Space;
        [SerializeField] private int simulatedValue = 80;

        public int GetHoldValue()
        {
            if (UnityEngine.Input.GetKey(simulateKey))
                return simulatedValue;
            return 0;
        }

        public bool IsActive()
        {
            return true;
        }
    }
}
