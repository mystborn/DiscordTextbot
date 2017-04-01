using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace TextBot
{
    public static class Bot
    {
        #region Fields

        private static DiscordSocketClient _client;
        private static bool _isConnected = false;
        private static bool _isDisposed = false;
        private static bool _isInitialized = false;
        private static Dictionary<string, string> _toContacts;
        private static Dictionary<string, string> _fromContacts;
        private static Dictionary<string, Func<SocketMessage, string>> _commands;
        private static List<string> _imgFiles;
        private static List<string> _emailDomains;
        private static string _lastEmailSender = "";
        private static SocketTextChannel _textChannel = null;

        #endregion

        #region Properties

        public static DiscordSocketClient Client
        {
            get { return _client; }
        }

        public static bool IsConnected
        {
            get { return _isConnected; }
        }

        #endregion

        #region Initialization

        public static void Initialize()
        {
            _isInitialized = true;
            (_toContacts, _fromContacts) = ContactInfo.LoadAddressBooks();

            _commands = new Dictionary<string, Func<SocketMessage, string>>()
            {
#if DEBUG
                { "/echo", Echo },
#endif
                { "/add_contact", AddContact },
                { "/remove_contact", RemoveContact },
                { "/list_contacts", ListContacts },
                { "/send", SendMessage }
            };

            _imgFiles = new List<string>()
            {
                ".png",
                ".jpg",
                ".gif"
            };

            _emailDomains = new List<string>()
            {
                "sms.alltelwireless.com",
                "txt.att.net",
                "mms.att.net",
                "sms.myboostmobile.com",
                "myboostmobile.com",
                "sms.mycricket.com",
                "mms.mycricket.com",
                "messaging.sprintpcs.com",
                "tmomail.net",
                "vtext.com",
                "vzwpix.com",
                "vmobl.com"
            };
        }

        public static async Task Start()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            if(!NetworkConnectionListener.IsConnectedToInternet)
            {
                NetworkConnectionListener.OnConnectToInternet += Start;
                return;
            }

            Console.WriteLine("Started");
            _client = new DiscordSocketClient();
            _isDisposed = false;

            _client.MessageReceived += MessageReceived;
            NetworkConnectionListener.OnDisconnectFromInternet += Stop;

            _client.Connected += GetTextChannel;

            _client.LoginAsync(TokenType.Bot, Settings.Info.Token).Wait();
            _client.StartAsync().Wait();

            _isConnected = true;

            while (_isConnected)
            {
                await Task.Delay(1000);
            }

            if (!_isDisposed)
            {
                _client.Dispose();
                _isDisposed = true;
            }

            Console.WriteLine("Finished");
        }

        private static void Start(object sender, EventArgs e)
        {
            NetworkConnectionListener.OnConnectToInternet -= Start;
            Task.Run(Start);
        }

        private static async Task Stop()
        {
            await _client.LogoutAsync();
            await _client.StopAsync();
            _client.Dispose();
            _isConnected = false;
        }

        private static async void Stop(object sender, EventArgs e)
        {
            NetworkConnectionListener.OnDisconnectFromInternet -= Stop;
            NetworkConnectionListener.OnConnectToInternet += Start;
            await Task.Run(Stop);
            Console.WriteLine("Client Stopped");
        }

        public static void HardStop()
        {
            NetworkConnectionListener.OnDisconnectFromInternet -= Stop;
            _client.LogoutAsync().Wait();
            _client.StopAsync().Wait();
            _isConnected = false;
        }

#endregion

#region Events

        private async static Task MessageReceived(SocketMessage message)
        {
            if(message.Author.Id != _client.CurrentUser.Id)
            {
                _lastEmailSender = "";
            }
            if(message.Channel.Name == "texting" && TryGetCommand(message, out string command))
            {
                var sendMessage = _commands[command](message);
                if (sendMessage != "")
                    await message.Channel.SendMessageAsync(sendMessage);
            }
        }

        public static async Task OnEmailReceived(AE.Net.Mail.MailMessage mail)
        {
            string address = mail.From.Address;
            string number = mail.From.Address.Substring(0, address.IndexOf('@'));
            if(_fromContacts.ContainsValue(number))
            {
                string sender = "";
                foreach(var name in _fromContacts.Where(kvp => kvp.Value == number).Select(kvp => kvp.Key))
                {
                    sender = name;
                    break;
                }
                if(_lastEmailSender != sender)
                {
                    _lastEmailSender = sender;
                    await _textChannel.SendMessageAsync($"{sender}:");
                }

                await RelayMessage(mail, address);
            }
            else if (_emailDomains.Contains(address.Substring(address.IndexOf('@'))))
            {
                if(_lastEmailSender != address)
                {
                    _lastEmailSender = address;
                    await _textChannel.SendMessageAsync($"{address}:");
                }

                await RelayMessage(mail, address);
            }
        }

        private static async Task RelayMessage(AE.Net.Mail.MailMessage mail, string address)
        {
            string message;
            foreach (var att in mail.Attachments)
            {
                string filetype = att.Filename.Substring(att.Filename.LastIndexOf('.')).ToLower();
                string local = Path.Combine(Settings.FolderPath, att.Filename);
                att.Save(local);
                if (_imgFiles.Contains(filetype))
                {
                    await _textChannel.SendFileAsync(local, "");
                }
                else if(filetype == ".txt")
                {
                    message = "";
                    using (var stream = File.OpenRead(local))
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            while(!reader.EndOfStream)
                            {
                                var line = await reader.ReadLineAsync();
                                message += line + '\n';
                            }
                            await _textChannel.SendMessageAsync(message);
                        }
                    }
                }
            }
            if(mail.Body != null)
            {
                message = "";
                string body = mail.Body;
                if(address.Substring(address.IndexOf('@')).Contains("att"))
                {
                    string temp = mail.Body;
                    int start = body.IndexOf("<td>");
                    body = "";
                    while(start != -1)
                    {
                        start += 4;
                        int end = temp.IndexOf("</td>");
                        body += temp.Substring(start, end - start).Trim() + '\n';
                        temp = temp.Remove(0, end + 4);
                        start = temp.IndexOf("<td>");
                    }
                }
                body.Trim();
                message += body;

                await _textChannel.SendMessageAsync(body);
            }
        }

#endregion

#region Helpers

        private static async Task GetTextChannel()
        {
            await Task.Run(async () =>
            {
                var servers = _client.Guilds.ToList();
                while (servers[0].Name == null)
                {
                    await Task.Delay(500);
                    servers = _client.Guilds.ToList();
                }
                _textChannel = (SocketTextChannel)_client.GetChannel(Settings.Info.DiscordChannelId);
            });
            _client.Connected -= GetTextChannel;
            if(_textChannel == null)
            {
                Idle.HardStop();
                EmailClient.HardStop();
                Console.WriteLine($"Could not find a text channel with the requested id: {Settings.Info.DiscordChannelId}");
            }
        }

        private static string[] GetAttachedFiles(SocketMessage msg)
        {
            List<string> files = new List<string>();
            foreach(var url in msg.Attachments.Select(val => val.Url))
            {
                var ftype = url.Remove(0, url.LastIndexOf('.'));
                if(_imgFiles.Contains(ftype))
                {
                    string local = Path.Combine(Settings.FolderPath, url.Remove(0, url.LastIndexOf("/") + 1));
                    using(WebClient wc = new WebClient())
                    {
                        try
                        {
                            wc.DownloadFile(url, local);
                            files.Add(local);
                        }
                        catch(WebException we)
                        {
                            Console.WriteLine($"Could not retrieve files from discord embed url: {url}'\n'Exception: {we.InnerException}");
                        }
                    }
                }
            }
            return files.ToArray();
        }

        private static bool TryGetCommand(SocketMessage message, out string command)
        {
            try
            {
                command = message.Content.Split(' ')[0];
                if(_commands.ContainsKey(command))
                {
                    return true;
                }
                return false;
            }
            catch(ArgumentOutOfRangeException)
            {
                command = string.Empty;
                return false;
            }
        }

        private static void UpdateContacts()
        {
            ContactInfo.SaveAddressBooks(_toContacts, _fromContacts);
        }

        public static async Task MessageSuccess(string address)
        {
            await _textChannel.SendMessageAsync($"Message sent to: {address.Substring(0, address.IndexOf('@'))}");
        }

        public static async Task MessageFailed(string address)
        {
            await _textChannel.SendMessageAsync($"Message failed to send to: {address.Substring(0, address.IndexOf('@'))}");
        }

#endregion

#region Commands

        static string AddContact(SocketMessage msg)
        {
            try
            {
                var finalMessage = msg.Content.Replace("/add_contact ", "");
                var info = finalMessage.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                var name = info[0];
                var number = info[1];
                _toContacts[name] = number;
                _fromContacts[name] = number.Substring(0, number.IndexOf('@'));

                UpdateContacts();
                return $"Added {name} to contacts.";
            }
            catch(ArgumentOutOfRangeException)
            {
                return "Did not receive a valid contact to add.";
            }
        }

        static string RemoveContact(SocketMessage msg)
        {
            try
            {
                var name = msg.Content.Replace("/remove_contact ", "");
                if(_toContacts.ContainsKey(name))
                {
                    _fromContacts.Remove(name);
                    _toContacts.Remove(name);
                    UpdateContacts();
                    return $"Removed {name} from contacts.";
                }
                else
                {
                    return "Name not in address book.";
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                return "Did not receive a contact to remove.";
            }
        }

        static string ListContacts(SocketMessage msg)
        {
            string list = "";
            foreach(var kvp in _fromContacts)
            {
                list += $"{kvp.Key}: {kvp.Value}\n";
            }
            if (list == "")
                list = "No contacts in address book.";
            return list;
        }

        static string SendMessage(SocketMessage msg)
        {
            try
            {
                string contact = msg.Content.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1];
                string message = msg.Content.Remove(0, msg.Content.IndexOf(contact));
                message = message.Replace(contact + " ", "");
                if(_toContacts.ContainsKey(contact) || _emailDomains.Contains(contact.Substring(contact.IndexOf('@'))))
                {
                    EmailClient.SendEmail(_toContacts[contact], message, GetAttachedFiles(msg));
                    return "";
                }
                else
                {
                    return "Contact is not valid.";
                }
            }
            catch(ArgumentOutOfRangeException)
            {
                return "No contact received.";
            }
        }

        static string Echo(SocketMessage msg)
        {
            try
            {
                var toSend = msg.Content.Replace("/echo ", "");
                return toSend;
            }
            catch(ArgumentOutOfRangeException)
            {
                return "echo";
            }
        }

        static string DebugTasks(SocketMessage msg)
        {
            var threads = Process.GetCurrentProcess().Threads;
            string toSend = "";
            foreach(ProcessThread thread in threads)
            {
                if (toSend == "")
                {
                    toSend += thread.Id;
                }
                else
                {
                    toSend += ", " + thread.Id;
                }
            }
            return toSend;
        }

#endregion
    }
}
