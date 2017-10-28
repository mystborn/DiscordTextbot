using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TextBot.Email;

namespace TextBot.Discord.Modules
{
    [Name("Texting")]
    public class TextingModule : ModuleBase<SocketCommandContext>
    {
        private CommandService _service;
        private static Regex _add = new Regex(@"\s*?(?<name>\S.*?)\s*?\|\s*?(?<contact>\d{10}@.+)");
        private static Regex _number = new Regex(@"\d{10}@.+");

        public TextingModule(CommandService service)
        {
            _service = service;
        }

        [Command("add")]
        [Summary("Adds a person to your contact list. Usage: [contact name]|[contact_number_as_email]")]
        public async Task AddCmd(string contact)
        {
            await Add(contact);
        }

        [Command("add")]
        [Summary("Adds a person to your contact list. Usage: [contact name]|[contact_number_as_email]")]
        public async Task AddCmd(string contact, [Remainder] string remainder)
        {
            contact += " " + remainder;
            await Add(contact);
        }

        [Command("remove")]
        [Summary("Removes a person from your contact list. Usage: [contact name]")]
        public async Task RemoveCmd(string contactName)
        {
            await Remove(contactName);
        }

        [Command("remove")]
        [Summary("Removes a person from your contact list. Usage: [contact name]")]
        public async Task RemoveCmd(string contactName, [Remainder] string remainder)
        {
            contactName += " " + remainder;
            await Remove(contactName);
        }

        [Command("list")]
        [Summary("Displays your address book.")]
        public async Task ListCmd()
        {
            var builder = new EmbedBuilder();
            bool success = false;
            foreach(var contact in Contacts.GetContacts())
            {
                success = true;
                builder.AddField(contact.Key, contact.Value);
            }
            if(success)
                await ReplyAsync("", false, builder.Build());
            else
                await ReplyAsync("You have no contacts.");
        }

        [Command("send")]
        [Summary("Sends a text message. Usage: [contact name/phone_number_as_email]")]
        public async Task SendCmd(string contact, [Remainder] string message)
        {
            var match = _number.Match(contact);
            var files = await GetAttachedFiles(Context.Message);


            if(match.Success && match.Index == 0)
            {
                if (files.Length == 0)
                    EmailClient.SendEmail(match.Value, message);
                else
                    EmailClient.SendEmail(match.Value, message, files);
            }
            else if(TryGetContact(contact, ref message, out var value))
            {
                if (files.Length == 0)
                    EmailClient.SendEmail(value, message);
                else
                    EmailClient.SendEmail(value, message, files);
            }
            else
                await ReplyAsync($"The name {contact} does not exist.");
        }

        private async Task<string[]> GetAttachedFiles(SocketUserMessage message)
        {
            List<string> files = new List<string>(message.Attachments.Count);
            using (var wc = new WebClient())
            {
                foreach (var url in message.Attachments.Select(val => val.Url))
                {
                    var ext = Path.GetExtension(url);
                    if (Settings.Config.PossibleAttachments.Contains(ext))
                    {
                        string local = Path.Combine(Bot.OutgoingAttachments, url.Remove(0, url.LastIndexOf("/") + 1));
                        try
                        {
                            await wc.DownloadFileTaskAsync(url, local);
                            files.Add(local);
                        }
                        catch(WebException we)
                        {
                            await ReplyAsync($"Could not retrieve the files from the embed url: {url}\n   Exception: {we.Message}");
                        }
                    }
                }
            }
            return files.ToArray();
        }

        private async Task Add(string contact)
        {
            var result = _add.Match(contact);

            if (!result.Success)
            {
                await ReplyAsync("The contact was not formatted properly. Proper formatting: contact name | contact_number_as_email");
                return;
            }

            if (!Contacts.TryAddContact(result.Groups["name"].Value, result.Groups["contact"].Value, out var error))
            {
                await ReplyAsync(error);
                return;
            }

            await ReplyAsync($"{result.Groups["name"]} was successfully added to your contacts.");
        }

        private async Task Remove(string contact)
        {
            contact = contact.Trim();

            if (!Contacts.TryRemoveContact(contact, out var error))
            {
                await ReplyAsync(error);
                return;
            }

            await ReplyAsync($"Successfully removed {contact} from your contacts");
        }

        private static bool TryGetContact(string command, ref string message, out string contact)
        {
            if(Contacts.TryGetNumber(command, out contact))
            {
                return true;
            }
            message = message.Insert(0, " ");
            int index = 0;
            int i = 2;
            while (i++ <= Contacts.LongestName)
            {
                index = 0;
                while (true)
                {
                    var newIndex = message.IndexOf(' ', index);
                    if (newIndex == index)
                    {
                        ++index;
                        continue;
                    }
                    index = newIndex;
                    break;
                }
                command += message.Substring(0, index);
                message.Remove(0, index);
                if (Contacts.TryGetNumber(command, out contact))
                    return true;
            }
            return false;
        }
    }
}
