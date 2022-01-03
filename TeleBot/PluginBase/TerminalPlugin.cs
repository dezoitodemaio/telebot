using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace PluginBase
{
    public class TerminalPlugin : IPlugin
    {
        public void Init()
        {
            //...
        }


        public async void ExecuteAsync(IPluginScope scope)
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

                await scope.SendMessage(resp ?? "Sem output");
            }

        }

        public string GetName()
        {
            return "Terminal";
        }

        public string GetCommand()
        {
            return "/terminal";
        }
    }

}
