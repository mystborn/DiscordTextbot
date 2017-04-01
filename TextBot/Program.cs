using System;
using System.Threading.Tasks;

namespace TextBot
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Settings.TryInitialize())
            {
                Task.Run(async () => await NetworkConnectionListener.Start()).Wait();

                if(!NetworkConnectionListener.IsConnectedToInternet)
                {
                    Console.WriteLine("Not connected to internet. Connect to internet to start bot.");
                }

                Idle.Initialize(Bot.OnEmailReceived);
                EmailClient.Initialize();

                Task.Run(async () =>
                {
                    await Bot.Start();
                });
                Task.Run(async () =>
                {
                    try
                    {
                        await Idle.IdleStart();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Exception thrown in Idle Thread: {e.GetType().Name}");
                        throw new Exception("Exception thrown in Idle Thread.", e);
                    }
                });
            }
            else
            {
                Console.WriteLine($"Please fill out the TextbotSettings.xml file created at:\n {Settings.FolderPath}\nRestart the program when it's completed.");
            }
            while(true)
            {
                Task.Delay(1000).Wait();
            }
        }
    }
}
