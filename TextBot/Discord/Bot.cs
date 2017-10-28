using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using static TextBot.Settings;

namespace TextBot.Discord
{
    public static class Bot
    {
        private static CommandService _service = new CommandService();
        private static DiscordSocketClient _client;
        private static bool _isOnline = false;

        public static IMessageChannel TextingChannel { get; private set; } = null;
        public static string OutgoingAttachments { get; private set; } = null;

        public static async Task Start()
        {
            OutgoingAttachments = Path.Combine(AppContext.BaseDirectory, "OutgoingAttachments");

            if (!Directory.Exists(OutgoingAttachments))
                Directory.CreateDirectory(OutgoingAttachments);

            _client = new DiscordSocketClient();

            NetworkConnection.Connected += Login;
            NetworkConnection.Disconnected += Logoff;

            await Setup();

            if (NetworkConnection.IsConnected)
                Login(null, EventArgs.Empty);

            Console.WriteLine("Finished Starting bot.");
        }

        public static async Task SendMessage(string msg)
        {
            if (TextingChannel == null)
            {
                var id = Config.ChannelId;
                TextingChannel = (ITextChannel)_client.GetChannel(Config.ChannelId);
            }
            await TextingChannel.SendMessageAsync(msg);
        }

        public static async Task SendFile(string filePath)
        {
            if (TextingChannel == null)
            {
                var id = Config.ChannelId;
                TextingChannel = (ITextChannel)_client.GetChannel(Config.ChannelId);
            }
            await TextingChannel.SendFileAsync(filePath);
        }

        private static async void Login(object sender, EventArgs e)
        {
            if (_isOnline)
                return;
            _isOnline = true;
            await _client.LoginAsync(TokenType.Bot, Config.Token);
            await _client.StartAsync();
        }

        private static async void Logoff(object sender, EventArgs e)
        {
            if (!_isOnline)
                return;
            _isOnline = false;
            await _client.LogoutAsync();
            await _client.StopAsync();
        }

        private static async Task Setup()
        {
            await _service.AddModulesAsync(Assembly.GetExecutingAssembly());
            _client.MessageReceived += HandleCommand;
        }

        private static async Task HandleCommand(SocketMessage parameterMessage)
        {
            var message = parameterMessage as SocketUserMessage;

            if (message == null)
                return;

            int pos = 0;

            if (!(message.HasMentionPrefix(_client.CurrentUser, ref pos) || message.HasStringPrefix(Config.Prefix, ref pos)))
                return;

            var context = new SocketCommandContext(_client, message);

            var result = await _service.ExecuteAsync(context, pos);

            if (!result.IsSuccess && message.HasStringPrefix(Config.Prefix, ref pos))
                await message.Channel.SendMessageAsync($"Error: {result.ErrorReason}");
        }
    }
}
