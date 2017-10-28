using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace TextBot
{
    public class Settings
    {
        private static Settings _config = null;
        private static string _fileName = "config.xml";
        public static Settings Config
        {
            get
            {
                if (_config == null)
                    Load();
                return _config;
            }
        }

        public string Prefix { get; set; } = "/";
        public string Token { get; set; } = "";
        public ulong ChannelId { get; set; }
        public Email.LoginInfo Login { get; set; } = new Email.LoginInfo();
        public string[] PhoneDomains { get; set; } = new string[] 
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

        public string[] PossibleAttachments { get; set; } = new string[]
        {
            ".png",
            ".jpg",
            ".gif"
        };

        public static void Load()
        {
            Console.WriteLine("Loading");
            Settings config = null;
            string file = Path.Combine(AppContext.BaseDirectory, _fileName);
            if(!File.Exists(file))
            {
                var path = Path.GetDirectoryName(file);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                config = new Settings();

                Console.WriteLine("Please enter your token.");
                string token = Console.ReadLine();

                var id = GetChannelId();

                Console.WriteLine("\nPlease enter your email address.");
                var address = Console.ReadLine();

                Console.WriteLine("\nPlease enter your email password.");
                var password = Console.ReadLine();

                Console.WriteLine($"The default settings use gmail. Use these? [y/n] (You can always edit this later using the file at: {file}");

                while (true)
                {
                    var answer = Console.ReadLine().ToLower();
                    if (answer == "y")
                        break;
                    else if(answer == "n")
                    {
                        EmailSetup(Config.Login);
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Please answer 'y' or 'n'");
                    }
                }

                config.Login.Username = address;
                config.Login.Password = password;
                config.Token = token;
                config.ChannelId = id;
                config.Save();
            }
            else
            {
                using(var fs = new StreamReader(Path.Combine(AppContext.BaseDirectory, _fileName)))
                {
                    var xml = new XmlSerializer(typeof(Settings));
                    config = (Settings)xml.Deserialize(fs);
                }
            }
            _config = config;
        }

        public void Save()
        {
            using(var fs = new StreamWriter(Path.Combine(AppContext.BaseDirectory, _fileName)))
            {
                var xml = new XmlSerializer(typeof(Settings));
                xml.Serialize(fs, this);
            }
        }

        private static ulong GetChannelId()
        {
            Console.WriteLine();
            Console.WriteLine("Please enter the texting channel id.");
            if (ulong.TryParse(Console.ReadLine(), out var id))
                return id;
            else
                return GetChannelId();
        }

        private static void EmailSetup(Email.LoginInfo info)
        {
            Console.WriteLine("Choose your email service:\n" +
                "[0] Gmail\n" +
                "[1] Hotmail/Live/Outlook\n" +
                "[2] Yahoo Mail\n" +
                "[3] Zoho\n" +
                "[4] Other");
            switch(Console.ReadLine())
            {
                case "0":
                    return;
                case "1":
                    info.ImapServer = "imap-mail.outlook.com";
                    info.ImapServerPort = 993;
                    info.SmtpServer = "smtp-mail.outlook.com";
                    info.SmtpServerPort = 587;
                    break;
                case "2":
                    info.ImapServer = "imap.mail.yahoo.com";
                    info.ImapServerPort = 993;
                    info.SmtpServer = "smtp.mail.yahoo.com";
                    info.SmtpServerPort = 587;
                    break;
                case "3":
                    info.ImapServer = "imap.zoho.com";
                    info.ImapServerPort = 993;
                    info.SmtpServer = "smtp.zoho.com";
                    info.SmtpServerPort = 465;
                    break;
                case "4":
                    Console.WriteLine("Please enter the imap server name");
                    info.ImapServer = Console.ReadLine();
                    Console.WriteLine("Please enter the imap server port");
                    info.ImapServerPort = int.Parse(Console.ReadLine());
                    Console.WriteLine("Please enter the smtp server name");
                    info.SmtpServer = Console.ReadLine();
                    Console.WriteLine("Please enter the smtp server port");
                    info.SmtpServerPort = int.Parse(Console.ReadLine());
                    break;
            }
        }
    }
}
