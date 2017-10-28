using System;
using System.Collections.Generic;
using System.Text;

namespace TextBot.Email
{
    public class LoginInfo
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpServerPort { get; set; } = 465;
        public bool EnableSsl { get; set; } = true;
        public string ImapServer { get; set; } = "imap.gmail.com";
        public int ImapServerPort { get; set; } = 993;
    }
}
