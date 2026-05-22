namespace MusicGame.Input
{
    public interface IHoldValueProvider
    {
        /// <summary>
        /// Returns a value from 0 to 100 representing the current hold input strength.
        /// </summary>
        int GetHoldValue();

        /// <summary>
        /// Returns true if the provider is currently active/connected.
        /// </summary>
        bool IsActive();
    }
}
