using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using HtmlAgilityPack;
using Microsoft.Toolkit.Uwp.Notifications;

namespace Olx
{
    internal static class Program
    {
        private static List<string> _idList;
        private static AppConfig AppConfig => Utils.GetAppSettings();

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
            
            _idList = Utils.GetIds(AppConfig.Directory, AppConfig.ListIdFile);
            
            var timer = new Timer(e => ScrapeWebsite(), null, 0, 60000);
            Console.ReadLine();
        }

        static void ScrapeWebsite()
        {
            Console.WriteLine($"Start Scrape {DateTime.Now:HH:mm:ss}");
            foreach (var searchItem in AppConfig.SearchItems)
            {
                var web = new HtmlWeb();
                var doc = web.Load(searchItem.Url);
                var productsList = doc.DocumentNode.SelectNodes("//*[@id=\"mainContent\"]/div[2]/form/div[5]/div/div[2]/div[@data-cy=\"l-card\"]");

                var resultList = productsList.Where(e => !_idList.Contains(e.Attributes["id"].Value))
                    .Select(myTripsNode =>
                        new OlxProduct
                        {
                            SysDate = DateTime.Now,
                            Id = myTripsNode.Id,
                            Name = myTripsNode.SelectNodes(".//div[contains(@class, 'css-u2ayx9')]").First().ChildNodes.First(e => e.Name == "h6").InnerText,
                            Location = myTripsNode.SelectNodes(".//p[@data-testid='location-date']").First().InnerText.Split(" - ")[0],
                            Data = myTripsNode.SelectNodes(".//p[@data-testid='location-date']").First().InnerText.Split(" - ")[1],
                            Pret = myTripsNode.SelectNodes(".//div[contains(@class, 'css-u2ayx9')]").First().ChildNodes.Where(e => e.Name == "p").First()
                                .InnerText,
                            Url = "https://www.olx.ro" + myTripsNode.ChildNodes.First(e => e.Name == "a").Attributes["href"].Value
                        })
                    .ToList();

                if (resultList.Count <= 0) continue;
                Console.WriteLine($"{searchItem.Name} - {DateTime.Now:HH:mm:ss} - result count: {resultList.Count}");
                var idList = resultList.Select(x => x.Id);
                _idList.AddRange(resultList.Select(x => x.Id));
                File.AppendAllLines(AppConfig.ListIdFilePath, idList);
                CreateNotification(searchItem.Name, resultList);
                Utils.WriteCsvFile(resultList, $"{AppConfig.Directory}\\{searchItem.FilePath}_{DateTime.Now:MM_dd}.csv");
            }
        }

        private static void CreateNotification(string title, List<OlxProduct> results)
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
    }
    
    public sealed record SearchItem(string Name, string Url, string FilePath);
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}