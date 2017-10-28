using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using System.ComponentModel;
using TextBot.Discord;

namespace TextBot.Email
{
    public static class EmailClient
    {
        private static SmtpClient _sender;
        private static MailAddress _self = null;
        private static HashSet<MailMessage> _pending;
        private static bool _isOnline = false;
        private static bool _isInitialized = false;

        public static void Start()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

            if (NetworkConnection.IsConnected)
                Login(null, EventArgs.Empty);

            NetworkConnection.Connected += Login;
            NetworkConnection.Disconnected += Logoff;
            Console.WriteLine("Email Client Started");
        }

        public static void SendEmail(string address, string message)
        {
            var to = new MailAddress(address);
            var msg = new MailMessage(_self, to)
            {
                Body = message,
                BodyEncoding = Encoding.UTF8
            };
            SendMessage(msg);
        }

        public static void SendEmail(string address, string message, params string[] files)
        {
            var to = new MailAddress(address);
            var msg = new MailMessage(_self, to)
            {
                Body = message,
                BodyEncoding = Encoding.UTF8
            };

            foreach (var path in files)
                msg.Attachments.Add(new Attachment(path));

            SendMessage(msg);
        }

        public static void SendEmail(string[] addresses, string message)
        {
            var msg = new MailMessage()
            {
                Body = message,
                BodyEncoding = Encoding.UTF8,
                From = _self
            };

            foreach (var address in addresses)
                msg.To.Add(new MailAddress(address));

            SendMessage(msg);
        }

        public static void SendEmail(string[] addresses, string message, params string[] files)
        {
            var msg = new MailMessage()
            {
                Body = message,
                BodyEncoding = Encoding.UTF8,
                From = _self
            };

            foreach (var address in addresses)
                msg.To.Add(new MailAddress(address));

            foreach (var path in files)
                msg.Attachments.Add(new Attachment(path));

            SendMessage(msg);
        }

        private static void Login(object sender, EventArgs e)
        {
            if (_isOnline)
                return;
            _isOnline = true;
            var login = Settings.Config.Login;
            _self = new MailAddress(login.Username);
            _pending = new HashSet<MailMessage>();
            NetworkCredential cred = new NetworkCredential(login.Username, login.Password);
            _sender = new SmtpClient(login.SmtpServer, login.SmtpServerPort)
            {
                EnableSsl = login.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = cred
            };
            _sender.SendCompleted += SendCompleted;
        }

        private static void Logoff(object sender, EventArgs e)
        {
            if (!_isOnline)
                return;
            _isOnline = false;
            _sender.SendAsyncCancel();
            _sender.Dispose();
        }

        private static void SendMessage(MailMessage message)
        {
            if (!_isOnline)
                Console.WriteLine("Tried to send an email while not connected to the internet.");
            _pending.Add(message);
            _sender.SendAsync(message, message);
        }

        private static async void SendCompleted(object sender, AsyncCompletedEventArgs e)
        {
            var token = (MailMessage)e.UserState;
            if(_pending.Contains(token))
            {
                StringBuilder sb = new StringBuilder();
                foreach(var address in token.To.Select(sentTo => sentTo.Address))
                {
                    if (sb.Length != 0)
                        sb.Append(',');
                    sb.Append(address);
                }

                var to = sb.ToString();

                if(e.Cancelled)
                {
                    await Bot.SendMessage($"The message to {to} was cancelled.");
                }
                else if(e.Error != null)
                {
                    await Bot.SendMessage($"The message to {to} encountered an error: {e.Error.ToString()}");
                }
                else
                {
                    await Bot.SendMessage($"The message to {to} was successfully sent.");
                }
            }
            _pending.Remove(token);
            try
            {
                token.Dispose();
            }
            catch(ObjectDisposedException) { }
        }
    }
}
