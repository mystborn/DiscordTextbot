using System;
using System.Threading.Tasks;
using TextBot.Discord;
using TextBot.Email;

namespace TextBot
{
    class Program
    {
        public static void Main(string[] args)
        {
            var runner = new Program();
            var task = runner.Start();
            task.GetAwaiter().GetResult();
        }

        public async Task Start()
        {
            try
            {
                Contacts.Load();
                await NetworkConnection.Start();
                Settings.Load();
                await Bot.Start();
                EmailClient.Start();
                EmailListener.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            await Task.Delay(-1);
        }
    }
}
