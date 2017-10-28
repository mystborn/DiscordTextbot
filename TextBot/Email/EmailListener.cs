using MailKit;
using MailKit.Net.Imap;
using MimeKit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using TextBot.Discord;

namespace TextBot.Email
{
    public class EmailListener
    {
        private static bool _isInitialized = false;
        private static bool _isOnline = false;
        private static ImapClient _imap;
        private static string _lastSender = "";
        private static string _attachmentFolder = "";
        private static int _messages = 0;
        private static CancellationTokenSource _source;

        public static void Start()
        {
            if (_isInitialized)
                return;

            Initialize();
        }

        private static void Initialize()
        {
            _isInitialized = true;

            _attachmentFolder = Path.Combine(AppContext.BaseDirectory, "Attachments");
            if (!Directory.Exists(_attachmentFolder))
                Directory.CreateDirectory(_attachmentFolder);

            _imap = new ImapClient();

            if (NetworkConnection.IsConnected)
                Start(null, EventArgs.Empty);

            NetworkConnection.Connected += Start;
            NetworkConnection.Disconnected += Stop;
        }

        private static void Start(object sender, EventArgs e)
        {
            if (_isOnline)
                return;
            _isOnline = true;

            var login = Settings.Config.Login;

            _imap.Connect(login.ImapServer, login.ImapServerPort, login.EnableSsl);

            _imap.AuthenticationMechanisms.Remove("XOAUTH2");
            _imap.Authenticate(login.Username, login.Password);

            _imap.Inbox.Open(FolderAccess.ReadOnly);

            _messages = _imap.Inbox.Count;

            _imap.Inbox.MessageExpunged += OnMessageRemoved;
            _imap.Inbox.CountChanged += OnCountChanged;

            Task.Run((Action)Run);
        }

        private static void Stop(object sender, EventArgs e)
        {
            if (!_isOnline)
                return;
            _isOnline = false;
            try
            {
                _source.Cancel();
            }
            catch(ObjectDisposedException)
            {
                // The token already cancelled itself.
            }
        }

        private static void OnMessageRemoved(object sender, MessageEventArgs e)
        {
            Console.WriteLine("Message removed.");
            _messages -= 1;
        }

        private static void OnCountChanged(object sender, EventArgs e)
        {
            var folder = (ImapFolder)sender;

            if (folder.Count > _messages)
            {
                if(!_source.IsCancellationRequested)
                    _source.Cancel();
            }
        }

        private static async void Run()
        {
            while (_isOnline)
            {
                if (_messages < _imap.Inbox.Count)
                    Console.WriteLine("Received a message while processing other mesages");
                using (_source = new CancellationTokenSource())
                {
                    try
                    {
                        await Task.Run(() => IdleLoop(new IdleState(_imap, _source.Token)));
                        if (_isOnline)
                        {
                            var messages = await _imap.Inbox.FetchAsync(_messages, -1, MessageSummaryItems.BodyStructure | MessageSummaryItems.Body | MessageSummaryItems.Envelope | MessageSummaryItems.UniqueId);
                            _messages = _imap.Inbox.Count;
                            ProcessMessages(messages);
                        }
                    }
                    catch(ServiceNotConnectedException)
                    {
                        Console.WriteLine("Not Connected. Whoopsie");
                    }
                }
            }
        }

        private static void ProcessMessages(IList<IMessageSummary> messages)
        {
            foreach (var message in messages)
            {
                var address = message.Envelope.From[0].ToString();
                var host = address.Substring(address.LastIndexOf('@') + 1);
                if (Contacts.TryGetContact(address, out var contact))
                {
                    if (_lastSender != contact)
                    {
                        _lastSender = contact;
                        Bot.SendMessage($"{contact}:");
                    }
                }
                else if (Settings.Config.PhoneDomains.Contains(host))
                {
                    if (_lastSender != address)
                    {
                        _lastSender = address;
                        Bot.SendMessage($"{address}:");
                    }
                }
                else
                    return;

                HandleAttachments(message);
                HandleMessage(message, address);
            }
        }

        private static void HandleAttachments(IMessageSummary mail)
        {
            Dictionary<BodyPartBasic, MimeEntity> set = new Dictionary<BodyPartBasic, MimeEntity>();
            foreach (var att in GetAttachments(mail))
            {
                set.Add(att, _imap.Inbox.GetBodyPart(mail.UniqueId, att));
            }
            foreach(var kvp in set)
            {
                Task.Run(() => ProcessAttachments(kvp.Key, kvp.Value));
            }
        }

        private static IEnumerable<BodyPartBasic> GetAttachments(IMessageSummary mail)
        {
            foreach(var att in mail.BodyParts.Where(x => x.ContentDisposition != null && (x.ContentDisposition.IsAttachment || x.ContentDisposition.FileName != null))) {
                yield return att;
            }
        }

        private static void ProcessAttachments(BodyPartBasic part, MimeEntity attachment)
        {
            if (attachment is MessagePart rfc822)
            {
                var path = Path.Combine(_attachmentFolder, part.PartSpecifier + ".eml");

                rfc822.Message.WriteTo(path);
            }
            else
            {
                var att = (MimePart)attachment;

                var fname = att.FileName;

                var path = Path.Combine(_attachmentFolder, fname);

                using (var stream = File.Create(path))
                    att.ContentObject.DecodeTo(stream);

                var ext = Path.GetExtension(fname);

                if (Settings.Config.PossibleAttachments.Contains(ext))
                {
                    Bot.SendFile(path);
                }
                else if (ext == ".txt")
                {
                    StringBuilder sb = new StringBuilder();
                    using (var reader = new StreamReader(path))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            sb.Append(line);
                            sb.Append('\n');
                        }
                    }
                    Bot.SendMessage(sb.ToString());
                }
                else
                {
                    Bot.SendMessage($"Received an attachment from a text message that couldn't be sent to Discord. You can find the file here: {path}");
                }
            }
        }

        private static void HandleMessage(IMessageSummary mail, string address)
        {
            var part = mail.TextBody ?? mail.HtmlBody;
            if (part != null)
            {
                var body = ((TextPart)_imap.Inbox.GetBodyPart(mail.UniqueId, part)).Text;
                Task.Run(() => ProcessMessage(body, address));
            }
        }

        private static void ProcessMessage(string body, string address)
        {
            if (address.Contains("att"))
            {
                StringBuilder sb = new StringBuilder();
                int start = body.IndexOf("<td>");
                while (start != -1)
                {
                    start += 4;
                    int end = body.IndexOf("</td>", start);
                    sb.Append(body.Substring(start, end - start).Trim());
                    sb.Append('\n');
                    start = body.IndexOf("<td>", start);
                }
                Bot.SendMessage(sb.ToString());
            }
            else
            {
                Bot.SendMessage(body);
            }
        }

        class IdleState
        {
            readonly object mutex = new object();
            CancellationTokenSource timeout;

            public CancellationToken CancellationToken { get; private set; }

            public CancellationToken DoneToken { get; private set; }

            public ImapClient Client { get; private set; }

            public bool IsCancellationRequested
            {
                get
                {
                    return CancellationToken.IsCancellationRequested || DoneToken.IsCancellationRequested;
                }
            }

            public IdleState(ImapClient client, CancellationToken doneToken, CancellationToken cancellationToken = default(CancellationToken))
            {
                CancellationToken = cancellationToken;
                DoneToken = doneToken;
                Client = client;
                doneToken.Register(CancelTimeout);
            }

            void CancelTimeout()
            {
                lock(mutex)
                {
                    if (timeout != null)
                        timeout.Cancel();
                }
            }

            public void SetTimeoutSource(CancellationTokenSource source)
            {
                lock(mutex)
                {
                    timeout = source;
                    if (timeout != null && IsCancellationRequested)
                        timeout.Cancel();
                }
            }
        }

        private static void IdleLoop(IdleState idle)
        {
            lock (idle.Client.SyncRoot)
            {
                while (!idle.IsCancellationRequested)
                {
                    using (var timeout = new CancellationTokenSource())
                    {
                        using (var timer = new System.Timers.Timer(60000))
                        {
                            timer.Elapsed += (s, e) => timeout.Cancel();
                            timer.AutoReset = false;
                            timer.Enabled = true;

                            try
                            {
                                idle.SetTimeoutSource(timeout);
                                if (idle.Client.Capabilities.HasFlag(ImapCapabilities.Idle))
                                {
                                    idle.Client.Idle(timeout.Token, idle.CancellationToken);
                                }
                                else
                                {
                                    idle.Client.NoOp(idle.CancellationToken);

                                    WaitHandle.WaitAny(new[] { timeout.Token.WaitHandle, idle.CancellationToken.WaitHandle });
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                            catch (ImapProtocolException)
                            {
                                break;
                            }
                            catch (ImapCommandException)
                            {
                                break;
                            }
                            catch(IOException)
                            {
                                Console.WriteLine("Lost connection.");
                            }
                            catch(ServiceNotConnectedException)
                            {
                                Console.WriteLine("Whoopsie! Resetting connection.");
                            }
                            finally
                            {
                                idle.SetTimeoutSource(null);
                            }
                        }
                    }
                }
            }
        }
    }
}
