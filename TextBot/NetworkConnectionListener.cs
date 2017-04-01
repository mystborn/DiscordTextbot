using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;

namespace TextBot
{
    public static class NetworkConnectionListener
    {
        private static bool _isConnectedToInternet = true;

        public static bool IsConnectedToInternet
        {
            get { return _isConnectedToInternet; }
        }

        public static event EventHandler OnDisconnectFromInternet;
        public static event EventHandler OnConnectToInternet;

        public static async Task Start()
        {
            _isConnectedToInternet = await PingGoogle();
            if(_isConnectedToInternet)
            {
                NetworkChange.NetworkAddressChanged += AddressChangeCheckDisconnect;
            }
            else
            {
                NetworkChange.NetworkAddressChanged += AddressChangedCheckConnect;
            }
        }

        private static async void AddressChangeCheckDisconnect(object sender, EventArgs e)
        {
            _isConnectedToInternet = await PingGoogle();
            if(!_isConnectedToInternet)
            {
                OnDisconnectFromInternet?.Invoke(new object(), EventArgs.Empty);
                NetworkChange.NetworkAddressChanged -= AddressChangeCheckDisconnect;
                NetworkChange.NetworkAddressChanged += AddressChangedCheckConnect;
            }
        }

        private static async void AddressChangedCheckConnect(object sender, EventArgs e)
        {
            _isConnectedToInternet = await PingGoogle();
            if(_isConnectedToInternet)
            {
                OnConnectToInternet?.Invoke(new object(), EventArgs.Empty);
                NetworkChange.NetworkAddressChanged -= AddressChangedCheckConnect;
                NetworkChange.NetworkAddressChanged += AddressChangeCheckDisconnect;
            }
        }

        private async static Task<bool> PingGoogle()
        {
            Ping ping = new Ping();
            IPAddress address = IPAddress.Parse("8.8.8.8");
            byte[] buffer = new byte[32];
            int timeout = 2000;
            PingOptions options = new PingOptions();
            var reply = await ping.SendPingAsync(address, timeout, buffer, options);
            return reply.Status == IPStatus.Success;
        }
    }
}
