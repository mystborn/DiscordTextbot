using System;
using System.IO;

namespace TextBot
{
    public static class Settings
    {
        private static string _folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "BotData");
        private static LoginInfo _info = null;

        public static string FolderPath
        {
            get { return _folderPath; }
        }

        public static LoginInfo Info
        {
            get { return _info; }
        }

        public static bool TryInitialize()
        {
            _folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "TextbotData");
            if (!Directory.Exists(_folderPath))
            {
                Directory.CreateDirectory(_folderPath);
                CreateDefaultBotInfo();
                return false;
            }
            else
            {
                if(File.Exists(Path.Combine(_folderPath, "TextbotSettings.xml")))
                {
                    _info = LoginInfo.LoadLoginInfo();
                    return true;
                }
                else
                {
                    CreateDefaultBotInfo();
                    return false;
                }
            }
        }

        private static void CreateDefaultBotInfo()
        {
            LoginInfo info = new LoginInfo()
            {
                GmailUsername = "username@gmail.com",
                GmailPassword = "password",
                EmailDisplayName = "Your Name",
                Token = "BotToken",
                DiscordChannelId = 0
            };
            LoginInfo.SaveLoginInfo(info);
        }
    }
}
