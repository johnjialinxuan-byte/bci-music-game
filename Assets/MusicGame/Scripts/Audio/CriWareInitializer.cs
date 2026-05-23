using UnityEngine;
using CriWare;

namespace MusicGame.Audio
{
    public class CriWareInitializer : MonoBehaviour
    {
        private void Awake()
        {
            InitializeCriAtom();
        }

        private void InitializeCriAtom()
        {
            try
            {
                CriAtomPlugin.InitializeLibrary();
                Debug.Log("[CriWareInitializer] CriAtomPlugin initialized.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CriWareInitializer] CriAtomPlugin init issue: {e.Message}");
            }
        }

        private void OnDestroy()
        {
            // Do not finalize here - managed by CriAudioManager lifecycle
        }
    }
}
