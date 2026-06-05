using UnityEngine;

namespace MusicGame.Core
{
    public enum CommunicationMode
    {
        Local,
        Remote
    }

    public static class CommunicationSettings
    {
        public const string LocalIp = "127.0.0.1";
        public const string DefaultRemoteIp = "172.23.172.176";

        private const string ModeKey = "Communication.Mode";
        private const string RemoteIpKey = "Communication.RemoteIp";

        public static CommunicationMode Mode
        {
            get => (CommunicationMode)PlayerPrefs.GetInt(ModeKey, (int)CommunicationMode.Remote);
            set
            {
                PlayerPrefs.SetInt(ModeKey, (int)value);
                PlayerPrefs.Save();
            }
        }

        public static string RemoteIp
        {
            get
            {
                string value = PlayerPrefs.GetString(RemoteIpKey, DefaultRemoteIp);
                return string.IsNullOrWhiteSpace(value) ? DefaultRemoteIp : value.Trim();
            }
            set
            {
                PlayerPrefs.SetString(RemoteIpKey, string.IsNullOrWhiteSpace(value) ? DefaultRemoteIp : value.Trim());
                PlayerPrefs.Save();
            }
        }

        public static string CurrentIp => Mode == CommunicationMode.Local ? LocalIp : RemoteIp;
    }
}
