using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using System.ComponentModel;

namespace TextBot
{
    public static class EmailClient
    {
        private static SmtpClient _sendClient;
        private static MailAddress _self = null;
        private static Dictionary<int, MailMessage> _pending;
        private static int _count;

        public static void Initialize()
        {
            if(!NetworkConnectionListener.IsConnectedToInternet)
            {
                NetworkConnectionListener.OnConnectToInternet += Initialize;
                return;
            }
            _self = _self ?? new MailAddress(Settings.Info.GmailUsername, Settings.Info.EmailDisplayName);
            _pending = new Dictionary<int, MailMessage>();
            NetworkCredential cred = new NetworkCredential(Settings.Info.GmailUsername, Settings.Info.GmailPassword);
            _sendClient = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = cred,
                Port = 587
            };
            _sendClient.SendCompleted += SendCompletedCallback;

            NetworkConnectionListener.OnDisconnectFromInternet += Close;
        }

        public static void Close()
        {
            foreach(var val in _pending.Values)
            {
                val.Dispose();
            }
            _sendClient.SendCompleted -= SendCompletedCallback;
            _sendClient.Dispose();
        }

        private static void Initialize(object sender, EventArgs e)
        {
            NetworkConnectionListener.OnConnectToInternet -= Initialize;
            Initialize();
        }

        private static void Close(object sender, EventArgs e)
        {
            Close();
            NetworkConnectionListener.OnDisconnectFromInternet -= Close;
            NetworkConnectionListener.OnConnectToInternet += Initialize;
        }

        public static void HardStop()
        {
            NetworkConnectionListener.OnDisconnectFromInternet -= Close;
            Close();
        }

        public static void SendEmail(string address, string message)
        {
            MailAddress to = new MailAddress(address);
            MailMessage msg = new MailMessage(_self, to)
            {
                Body = message,
                BodyEncoding = Encoding.UTF8
            };
            SendMessage(msg);
        }

        public static void SendEmail(string address, string message, params string[] files)
        {
            MailAddress to = new MailAddress(address);
            MailMessage msg = new MailMessage(_self, to)
            {
                Body = message,
                BodyEncoding = Encoding.UTF8
            };
            foreach(var path in files)
            {
                var att = new Attachment(path);
                msg.Attachments.Add(att);
            }
            SendMessage(msg);

        }

        public static void SendEmail(string[] to, string message)
        {
            MailMessage msg = new MailMessage()
            {
                From = _self,
                Body = message,
                BodyEncoding = Encoding.UTF8
            };
            foreach (var address in to)
            {
                msg.To.Add(new MailAddress(address));
            }
            SendMessage(msg);
        }

        public static void SendEmail(string[] to, string message, params string[] files)
        {
            MailMessage msg = new MailMessage()
            {
                From = _self,
                Body = message,
                BodyEncoding = Encoding.UTF8
            };
            foreach (var address in to)
            {
                msg.To.Add(new MailAddress(address));
            }
            foreach (var path in files)
            {
                var att = new Attachment(path);
                msg.Attachments.Add(att);
            }
            SendMessage(msg);
        }

        private static void SendMessage(MailMessage message)
        {
            _pending.Add(_count, message);
            _sendClient.SendAsync(message, _count);
            _count++;
            if (_count > 10000)
                _count = 0;
        }

        private static void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            int token = (int)e.UserState;
            if(_pending.ContainsKey(token))
            {
                string to = "";
                foreach(var address in _pending[token].To.Select(add => add.Address))
                {
                    if(to == "")
                    {
                        to += address;
                    }
                    else
                    {
                        to += ", " + address; 
                    }
                }

                if (e.Cancelled)
                {
                    Console.WriteLine($"Message {token} cancelled.");
                    Task.Run(async () => await Bot.MessageFailed(to));
                }
                else if (e.Error != null)
                {
                    Console.WriteLine($"Message {token} encountered an error: {e.Error.ToString()}");
                    Task.Run(async () => await Bot.MessageFailed(to));
                }
                else
                {
                    Console.WriteLine($"Message {token} successfully sent.");
                    Task.Run(async () => await Bot.MessageSuccess(to));
                }
                _pending[token].Dispose();
                _pending.Remove(token);
            }
        }
    }
}
