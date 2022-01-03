using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TeleBot
{
    public class Program
    {
        private static List<Bot> Bots;

        public static async Task Main(string[] args)
        {
            Bots = new List<Bot>() {
                new Bot("5044972933:AAErPuXedJzXcmUXt_SZg2ZR1Ney7KVzRYw")
            };

            //var plugins = new Plugin();

            //var commands = plugins.Load();

            //commands.First().ExecuteAsync(scope);


            CreateHostBuilder(args).Build().Run();

            foreach (var bot in Bots)
            {
                bot.Stop();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
