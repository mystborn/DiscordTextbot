using System;
using System.Threading;
using System.Threading.Tasks;
using AE.Net.Mail;
using AE.Net.Mail.Imap;

namespace TextBot
{
    public static class Idle
    {
        #region Fields

        private static CancellationTokenSource _source = null;
        private static Func<MailMessage, Task> _onEmailReceived;
        private static bool _isInitialized = false;
        private static Task loop = null;

        #endregion

        #region Initialize

        public static void Initialize(Func<MailMessage, Task> onEmailReceived)
        {
            if (!_isInitialized)
            {
                _onEmailReceived = onEmailReceived;
                _isInitialized = true;
            }
            else
            {
                throw new InvalidOperationException("Idle client already initialized.");
            }
        }

        #endregion

        #region Idle Loop

        public static async Task IdleStart()
        {
            if (_isInitialized == false)
                throw new Exception("Idle not Initialized.");

            if(!NetworkConnectionListener.IsConnectedToInternet)
            {
                NetworkConnectionListener.OnConnectToInternet += IdleStart;
                return;
            }

            if (_source != null)
            {
                if (!_source.IsCancellationRequested)
                    throw new InvalidOperationException("Idle loop is currently running. Do not start another.");
                else
                    _source.Dispose();
            }

            if(loop != null && loop.Status != TaskStatus.Running)
            {
                loop.Wait();
                loop.Dispose();
            }

            _source = new CancellationTokenSource();
            NetworkConnectionListener.OnDisconnectFromInternet += IdleEnd;
#if DEBUG
            Console.WriteLine("Idle Loop started.");
#endif
            loop = IdleLoop();
            await loop;

            //await IdleLoop();
#if DEBUG
            Console.WriteLine("Idle official end");
#endif
        }

        private static void IdleEnd()
        {
            _source.Cancel();
        }

        private static void IdleStart(object sender, EventArgs e)
        {
            NetworkConnectionListener.OnConnectToInternet -= IdleStart;
            Task.Run(async () => await IdleStart());
        }

        private static void IdleEnd(object sender, EventArgs e)
        {
            NetworkConnectionListener.OnDisconnectFromInternet -= IdleEnd;
            NetworkConnectionListener.OnConnectToInternet += IdleStart;
            IdleEnd();
        }

        public static void HardStop()
        {
            NetworkConnectionListener.OnDisconnectFromInternet -= IdleEnd;
            _source.Cancel();
            loop.Wait();
            loop.Dispose();
            loop = null;
        }

        private static Task IdleLoop()
        {
            return Task.Run(() =>
            {
                while (!_source.IsCancellationRequested)
                {
                    try
                    {
                        using (ImapClient imap = new ImapClient("imap.gmail.com", Settings.Info.GmailUsername, Settings.Info.GmailPassword, port: 993, secure: true))
                        {
                            EventHandler<MessageEventArgs> onNewMessage = delegate (object sender, MessageEventArgs e)
                            {
                                var msg = imap.GetMessage(e.MessageCount - 1);
                                Task.Run(async () => await _onEmailReceived(msg));
                            };
                            imap.NewMessage += onNewMessage;
                            Task t;
                            try
                            {
                                t = Task.Delay(480000, _source.Token);
                                t.Wait();
                            }
                            catch (AggregateException e)
                            {
                                foreach (var inner in e.InnerExceptions)
                                {
                                    if (inner is TaskCanceledException)
                                    {
#if DEBUG
                                        Console.WriteLine("Idle loop finished.");
#endif
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Idle loop threw unhandled exception: {e.GetType().Name}");
                                        throw new Exception($"Idle loop threw unhandled exception: {e.GetType().Name}", e);
                                    }
                                }
                            }
                            try
                            {
                                imap.NewMessage -= onNewMessage;
                            }
                            catch (System.IO.IOException)
                            {

                            }
                        }
                    }
                    catch(System.IO.IOException)
                    {

                    }
                }
#if DEBUG
                Console.WriteLine("Idle Loop fully closed.");
#endif
            });
        }

        #endregion
    }
}
