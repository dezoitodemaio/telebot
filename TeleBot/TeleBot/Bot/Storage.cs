using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft;
using Newtonsoft.Json;

namespace TeleBot
{
    public class Storage
    {
        public List<string> Tokens = new List<string>();
    }

    public static class StorageManager
    {
        public static Storage Data;
        public static string FileName = $"{Path.DirectorySeparatorChar}storage.json";
        public static string BinLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        public static string FullName = $"{BinLocation}{FileName}";

        public static Storage Load()
        {
            if (File.Exists(FullName))
            {
                string json = File.ReadAllText(FullName);
                Data = JsonConvert.DeserializeObject<Storage>(json);
            }
            else
            {
                Data = new Storage();
                Save();
            }

            return Data;

        }

        public static void Save()
        {
            var json = JsonConvert.SerializeObject(Data);
            File.WriteAllText(FullName, json);
        }
    }
}
