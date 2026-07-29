using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppsettingsDiff
{
    public class JsonConfigReader
    {
        public Dictionary<string, string> ReadJsonConfig(string jsonConfigPath)
        {
            // Implement JSON file reading logic here
            // For example:
            var json = File.ReadAllText(jsonConfigPath);
            var config = JsonDocument.Parse(json).RootElement;
            var values = new Dictionary<string, string>();
            // Populate the dictionary with key-value pairs from the JSON config
            return values;
        }
    }
}