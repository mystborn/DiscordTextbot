using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;

namespace TextBot
{
    public static class NetworkConnection
    {
        #region Constants

        private const int TIMEOUT = 2000;
        private static byte[] _buffer = new byte[32];
        private static IPAddress _google = IPAddress.Parse("8.8.8.8");

        #endregion

        #region Fields

        private static bool _isConnected;
        private static bool _isStarted = false;

        #endregion

        #region Properties

        public static bool IsConnected => _isConnected;

        #endregion

        #region Events

        public static event EventHandler Connected;
        public static event EventHandler Disconnected;

        #endregion

        #region Public Api

        public static async Task Start()
        {
            if (_isStarted)
                throw new InvalidOperationException("Cannot start the NetworkConnection more than once.");

            _isStarted = true;
            _isConnected = await PingGoogle();
            NetworkChange.NetworkAddressChanged += CheckConnection;
            Connected += (s, e) => Console.WriteLine("Connected to internet.");
            Disconnected += (s, e) => Console.WriteLine("Disconnected from internet.");
        }

        #endregion

        #region Private Api

        private static async void CheckConnection(object sender, EventArgs e)
        {
            Console.WriteLine("Checking Connection.");
            var temp = await PingGoogle();
            if (temp == _isConnected)
                return;
            _isConnected = temp;
            if (_isConnected)
                Connected?.Invoke(null, EventArgs.Empty);
            else
                Disconnected?.Invoke(null, EventArgs.Empty);
        }

        private static async Task<bool> PingGoogle()
        {
            Ping _ping = new Ping();
            try
            {
                var reply = await _ping.SendPingAsync(_google, TIMEOUT);
                return reply.Status == IPStatus.Success;
            }
            catch(PingException e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        #endregion
    }
}
