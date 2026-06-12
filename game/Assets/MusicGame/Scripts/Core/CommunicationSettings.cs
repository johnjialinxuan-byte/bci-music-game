using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
                string value = PlayerPrefs.GetString(RemoteIpKey, string.Empty);
                if (string.IsNullOrWhiteSpace(value) || value.Trim() == DefaultRemoteIp)
                    return DeviceIpv4Default;
                return value.Trim();
            }
            set
            {
                PlayerPrefs.SetString(RemoteIpKey, string.IsNullOrWhiteSpace(value) ? DeviceIpv4Default : value.Trim());
                PlayerPrefs.Save();
            }
        }

        public static string DeviceIpv4Default
        {
            get
            {
                string ip = GetCurrentDeviceIpv4();
                return string.IsNullOrWhiteSpace(ip) ? DefaultRemoteIp : ip;
            }
        }

        public static string GetCurrentDeviceIpv4()
        {
            try
            {
                foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (networkInterface.OperationalStatus != OperationalStatus.Up)
                        continue;

                    IPInterfaceProperties properties = networkInterface.GetIPProperties();
                    foreach (UnicastIPAddressInformation address in properties.UnicastAddresses)
                    {
                        IPAddress ip = address.Address;
                        if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip) && !ip.ToString().StartsWith("169.254."))
                            return ip.ToString();
                    }
                }
            }
            catch
            {
                // Fall back to DNS lookup below on platforms where NetworkInterface is restricted.
            }

            try
            {
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip) && !ip.ToString().StartsWith("169.254."))
                        return ip.ToString();
                }
            }
            catch
            {
                // Keep the configured fallback if the platform does not expose local addresses.
            }

            return DefaultRemoteIp;
        }

        public static string CurrentIp => Mode == CommunicationMode.Local ? LocalIp : RemoteIp;
    }
}
