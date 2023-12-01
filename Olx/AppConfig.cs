using System.Collections.Generic;

namespace Olx
{
    public sealed class AppConfig
    {
        public string Directory { get; set; }
        public string ListIdFile { get; set; }
        public string ListIdFilePath => $"{Directory}\\{ListIdFile}";
        public List<SearchItem> SearchItems { get; set; }
        public int SearchInterval { get; set; }
    }
}