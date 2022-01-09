using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TeleBot
{
    public class Program
    {      
        public static async Task Main(string[] args)
        {

            StorageManager.Load();

            BotManager.Initialize();            

            CreateHostBuilder(args).Build().Run();

            BotManager.Finish();
            
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
