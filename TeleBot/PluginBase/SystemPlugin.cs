using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PluginBase
{
    public class SystemPlugin : IPlugin
    {
        public void Init()
        {
            //...
        }


        public async Task ExecuteAsync(IPluginScope scope)
        {
            PickOption request = new PickOption()
            {
                Text = "Selecione:",
                Options = new List<Option>()
                {
                    new Option()
                    {
                        Text = "Terminal",
                        Value = "terminal",
                    },                    
                    new Option()
                    {
                        Text = "Desligar",
                        Value = "shutdown",
                    },                    
                    new Option()
                    {
                        Text = "Reiniciar",
                        Value = "restart",
                    },
                }
            };

            var ret = await scope.RequestPickOption(request);

            switch (ret)
            {
                case "shutdown":
                    Shutdown();
                    await scope.SendMessage("O sistem sera desligado em 30 segundos!");
                    break;
                default:
                    break;
            }
        }

        private void Shutdown()
        {
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.Arguments = "/c shutdown -s -t 30";
                proc.StartInfo.UseShellExecute = false;
                proc.Start();
            }
        }

        public async Task Term(IPluginScope scope)
        {
            string command = await scope.RequestMessage("Digite o comando:", "cancelar");
            
            if (command == "cancelar")
                return;

            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.Arguments = "/c " + command;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();

                var resp = proc.StandardOutput.ReadToEnd();

                if(!String.IsNullOrEmpty(resp))
                    await scope.SendMessage(resp ?? "Sem output");
            }

        }

        public string GetName()
        {
            return "System";
        }

        public string GetCommand()
        {
            return "/system";
        }
    }

}
