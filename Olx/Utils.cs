using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using CsvHelper;
using Microsoft.Extensions.Configuration;

namespace Olx
{
    public static class Utils
    {
        public static AppConfig GetAppSettings()
        {
            var config = new ConfigurationBuilder();
            config.SetBasePath(GetBasePath());
            config.AddJsonFile("config.json", false);
            var build = config.Build();
            return build.GetSection("App").Get<AppConfig>();
        }
        
        public static List<string> GetIds(string directory, string listIdFile)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var fileIdPath = $"{directory}\\{listIdFile}";
            return File.Exists(fileIdPath) ? File.ReadAllLines(fileIdPath).ToList() : new List<string>();
        }

        public static void WriteCsvFile(List<OlxProduct> resultList, string filePath)
        {
            while (true)
            {
                try
                {
                    using var stream = File.Open(filePath, FileMode.Append);
                    using var writer = new StreamWriter(stream);
                    using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                    csv.WriteRecords((IEnumerable)resultList);
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Thread.Sleep(5000);
                }
            }
        }
        
        private static string GetBasePath()
        {
            using var processModule = Process.GetCurrentProcess().MainModule;
            return Path.GetDirectoryName(processModule?.FileName);
        }
    }
}