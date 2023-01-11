using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using CsvHelper;
using HtmlAgilityPack;
using Microsoft.Toolkit.Uwp.Notifications;

namespace Olx
{
    class Program
    {
        private static List<string> _idList;
        private static List<SearchItem> searchList;
        private const string IdListFile = "idlist.txt";

        static void Main(string[] args)
        {
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                var args = ToastArguments.Parse(toastArgs.Argument);
                var process = new Process();
                process.StartInfo.FileName = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
                process.StartInfo.Arguments = args["url"] + " --new-window";
                process.Start();
            };
            
            _idList = File.Exists(IdListFile) ? File.ReadAllLines(IdListFile).ToList() : new List<string>();
            
            searchList = new List<SearchItem>()
            {
                new SearchItem("1660","https://www.olx.ro/d/electronice-si-electrocasnice/telefoane-mobile/iphone/?currency=RON", "olx1660"),
                // new SearchItem("1660","https://www.olx.ro/electronice-si-electrocasnice/q-1660/", "olx1660"),
                // new SearchItem("1070","https://www.olx.ro/electronice-si-electrocasnice/q-1070/", "olx1070"),
                // new SearchItem("6600","https://www.olx.ro/electronice-si-electrocasnice/q-6600/", "olx6600"),
                // new SearchItem("1060","https://www.olx.ro/electronice-si-electrocasnice/q-1060/", "olx1060"),
                // new SearchItem("1650S","https://www.olx.ro/electronice-si-electrocasnice/q-1650-super/", "olx1650S"),
                // new SearchItem("2060","https://www.olx.ro/electronice-si-electrocasnice/q-2060/", "olx2060"),
                // new SearchItem("iphone13pro","https://www.olx.ro/iasi_39939/q-iphone-13-pro/", "olxIphone13pro")
            };
            
            var timer = new Timer(e => ScrapeWebsite(), null, 0, 60000);
            Console.ReadLine();
        }

        static void ScrapeWebsite()
        {
            foreach (var searchItem in searchList)
            {
                var web = new HtmlWeb();
                var doc = web.Load(searchItem.Url);
                var productsList = doc.DocumentNode.SelectNodes("//*[@id='offers_table']/tbody/tr[@class='wrap']");

                var resultList = productsList.Select(htmlNode => HtmlNode.CreateNode(htmlNode.InnerHtml))
                    .Where(myTripsNode => !_idList.Contains(myTripsNode.SelectSingleNode("//td/div/table").Attributes["data-id"].Value))
                    .Select(myTripsNode => new OlxProduct()
                    {
                        SysDate = DateTime.Now,
                        Id = myTripsNode.SelectSingleNode("//td/div/table").Attributes["data-id"].Value,
                        Name = myTripsNode.SelectSingleNode("//div/h3/a/strong").InnerHtml,
                        Location = myTripsNode.SelectSingleNode("//td/div/table/tbody/tr[2]/td[1]/div/p/small[1]/span").InnerText,
                        Data = myTripsNode.SelectSingleNode("//td/div/table/tbody/tr[2]/td[1]/div/p/small[2]/span").InnerText,
                        Pret = myTripsNode.SelectSingleNode("//td/div/table/tbody/tr[1]/td[3]/div/p/strong").InnerText,
                        Url = myTripsNode.SelectSingleNode("//td/div/table/tbody/tr[1]/td[2]/div/h3/a").Attributes["href"].Value
                    })
                    .ToList();
                Console.WriteLine($"{searchItem.Name} - {DateTime.Now:HH:mm:ss} - result count: {resultList.Count}{(resultList.Count > 0 ? "  >>>> !!!!! " : "")}");
                if (resultList.Count <= 0) continue;
                var idList = resultList.Select(x => x.Id);
                _idList.AddRange(resultList.Select(x => x.Id));
                File.AppendAllLines(IdListFile, idList);
                CreateNotification(searchItem.Name, resultList);
                WriteCsvFile(resultList, $"{searchItem.FilePath}_{DateTime.Now:MM_dd}.csv");
            }
        }

        static void CreateNotification(string title, List<OlxProduct> results)
        {
            new ToastContentBuilder()
                .AddText(title, hintMaxLines: 1)
                .AddText($"S-au gasit {results.Count} anunturi noi")
                .AddButton(new ToastButton()
                    .SetContent("See details")
                    .AddArgument("action", "viewDetails")
                    .AddArgument("url", results.LastOrDefault().Url))
                .Show();
        }

        static void WriteCsvFile(List<OlxProduct> resultList, string filePath)
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

        class OlxProduct
        {
            public string Pret { get; set; }
            public string Name { get; set; }
            public string Location { get; set; }
            public string Data { get; set; }
            public string Url { get; set; }
            public string Id { get; set; }
            public DateTime SysDate { get; set; }
        }

        private record SearchItem(string Name, string Url, string FilePath);
    }
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit {}
}