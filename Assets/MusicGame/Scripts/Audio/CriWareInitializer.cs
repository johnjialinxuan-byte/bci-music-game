using UnityEngine;
using CriWare;

namespace MusicGame.Audio
{
    [DisallowMultipleComponent]
    public sealed class CriWareInitializer : MonoBehaviour
    {
        private bool initializedHere;

        private void Awake()
        {
            if (CriAtomPlugin.IsLibraryInitialized()) return;

            try
            {
                CriAtomPlugin.InitializeLibrary();
                initializedHere = true;
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[CriWareInitializer] CRIWARE initialization failed: {exception.Message}");
            }
        }

        private void OnDestroy()
        {
            if (!initializedHere || !CriAtomPlugin.IsLibraryInitialized()) return;

            CriAtomPlugin.FinalizeLibrary();
            initializedHere = false;
        }
    }
}
