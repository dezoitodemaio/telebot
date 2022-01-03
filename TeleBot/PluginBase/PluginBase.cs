using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PluginBase
{
    public interface IPlugin
    {
        void Init();

        string GetName();

        string GetCommand();

        public void ExecuteAsync(IPluginScope scope);
    }

    public interface IPluginScope
    {
        public Task SendMessage(string message);

        public Task<string> RequestMessage(string caption, string cancelText);

        public Task<string> RequestPickOption(PickOption request);

        public IEnumerable<IPlugin> GetPlugins();
    }


    //public class BaseCommand
    //{
    //    public string Name { get; set; }
    //    public string Command { get; set; }

    //    public virtual void ExecuteAsync(IPluginScope scope)
    //    {
    //    }
    //}

    public class Option
    {
        public string Text { get; set; }
        public string Value { get; set; }
    }

    public class PickOption
    {
        public string Text = "Selec a value:";
        public List<Option> Options { get; set; }
    }

}
