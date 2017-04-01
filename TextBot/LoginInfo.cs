using System.Xml.Serialization;
using System.IO;

namespace TextBot
{
    public class LoginInfo
    {
        #region Xml Properties

        public string Token { get; set; }
        public string GmailUsername { get; set; }
        public string GmailPassword { get; set; }
        public string EmailDisplayName { get; set; }
        public ulong DiscordChannelId { get; set; }

        #endregion

        #region Saving/Loading

        public static LoginInfo LoadLoginInfo()
        {
            var path = Path.Combine(Settings.FolderPath, "TextbotSettings.xml");
            LoginInfo info = null;
            if (File.Exists(path))
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        XmlSerializer cereal = new XmlSerializer(typeof(LoginInfo));
                        info = (LoginInfo)cereal.Deserialize(reader);
                    }
                }
                return info;
            }
            throw new FileNotFoundException("Could not find the file containing Login Info", "LoginInfo.xml");
        }

        public static void SaveLoginInfo(LoginInfo info)
        {
            var path = Path.Combine(Settings.FolderPath, "TextbotSettings.xml");
            if (File.Exists(path))
                File.Delete(path);
            using (FileStream stream = File.Create(path))
            {
                using (StreamWriter reader = new StreamWriter(stream))
                {
                    XmlSerializer cereal = new XmlSerializer(typeof(LoginInfo));
                    cereal.Serialize(reader, info);
                }
            }
        }

        #endregion
    }
}
