using System;
using HtmlAgilityPack;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using NLog;
using System.Diagnostics;

namespace ConsoleApp1
{
    class Program
    {
        private static ILogger Logger = LogManager.GetCurrentClassLogger();
        static void Main()
        {
            Stopwatch watch = new Stopwatch();

            Logger.Info("Method GetCityEventList started ...");
            watch.Start();
            var resultListEvent = GetCityEventList(); //заполнение всех полей
            watch.Stop();
            Console.WriteLine($"Elapsed time {watch.Elapsed}");

            Logger.Info("Json write and serialized started ...");
            string serializedCityEvents = JsonConvert.SerializeObject(resultListEvent); //вывод в файл json
            File.WriteAllText(@"result\CityEventData.json", serializedCityEvents);
            Logger.Info("Json write and serialized complete.");

            Console.ReadLine();
        }

        static List<CityEvent> GetCityEventList()
        {
            List<CityEvent> rawResultEventList = new List<CityEvent>();

            HtmlWeb web = new HtmlWeb();
            const int retryAttempsCount = 3;
            int m = 1;

            var pageUrl = "https://kuda-kazan.ru/event/";
            string titlePhrase = "";
            do
            {
                HtmlDocument docOfCityEvent = null;
                HtmlDocument pageOfCityEvents = null;
                for (int i = 0; i < retryAttempsCount; i++) //получение страницы и проверка на её существование
                {
                    pageOfCityEvents = web.Load(pageUrl);

                    if ((i + 1 == 3) && (pageOfCityEvents == null))
                    {
                        Logger.Error($"Can't get web event with url: {pageUrl}");
                        break;
                    }

                    if (pageOfCityEvents != null)
                        break;
                }

                var events = pageOfCityEvents.DocumentNode.SelectNodes("//*[@class='events_list']/div/div[1]/*[@itemprop='url']");
                foreach (HtmlNode n in events)
                {
                    CityEvent newCityEvent = new CityEvent();
                    newCityEvent.Url = n.Attributes["href"].Value;
                    var urlList = newCityEvent.Url.Split('/');
                    newCityEvent.ExternalId = urlList[urlList.Length - 2];

                    for (int i = 0; i < retryAttempsCount; i++)
                    {
                        docOfCityEvent = web.Load(newCityEvent.Url);

                        if ((i + 1 == 3) && (docOfCityEvent == null))
                        {
                            Logger.Error($"Can't get web event with url: {newCityEvent.Url}");
                            break;
                        }

                        if (docOfCityEvent != null)
                            break;
                    }

                    Console.WriteLine("Обработка события №" + m);

                    var shortReviewNode = docOfCityEvent.GetElementbyId("short_review");
                    var startDateNode = shortReviewNode.SelectSingleNode("//*[@itemprop='startDate']");

                    newCityEvent.StartDate = (startDateNode == null) ? throw new Exception("Failed to get 'StartDate'") : startDateNode.InnerText;

                    var minPriceNode = shortReviewNode.SelectSingleNode("//*[@itemprop='price']");

                    if (minPriceNode == null)
                    {
                        throw new Exception("Failed to get 'minPrice'");
                    }
                    else
                    {
                        if (minPriceNode.InnerText.Contains("от"))
                        {
                            newCityEvent.MinPrice = int.Parse(minPriceNode.InnerText.Substring(minPriceNode.InnerText.IndexOf(' ') + 1));
                        }
                        else
                        {
                            newCityEvent.MinPrice = (minPriceNode.InnerText == "0.00") ? 0 : int.Parse(minPriceNode.InnerText);
                        }
                    }

                    var endDateNode = shortReviewNode.SelectSingleNode("//*[@itemprop='endDate']");
                    var endDateNodeText = endDateNode.InnerText;

                    if ((endDateNode != null) && (newCityEvent.StartDate != endDateNodeText))
                    {
                        newCityEvent.EndDate = endDateNode.InnerText;
                    }

                    var viewsCountNode = shortReviewNode.SelectSingleNode("//*[@class='wishes']/span[1]");
                    newCityEvent.ViewsCount = (viewsCountNode == null) ? throw new Exception("Failed to get 'ViewsCount'") : int.Parse(viewsCountNode.InnerText);

                    var contentNode = docOfCityEvent.GetElementbyId("content");

                    var nameNode = contentNode.SelectSingleNode("//h1");
                    newCityEvent.Name = (nameNode == null) ? throw new Exception("Failed to get 'Name'") : WebUtility.HtmlDecode(nameNode.InnerText);

                    var descriptionNode = contentNode.SelectSingleNode("//*[@itemprop='description']/p[1]");
                    newCityEvent.Description = (descriptionNode == null) ? throw new Exception("Failed to get 'Description'") : WebUtility.HtmlDecode(descriptionNode.InnerText);

                    m++;

                    rawResultEventList.Add(newCityEvent);
                }

                var nextNode = pageOfCityEvents.GetElementbyId("content").SelectSingleNode("//*[@class='next']"); //берем ссылку на следующую страницу
                pageUrl = nextNode.Attributes["href"].Value;
                titlePhrase = nextNode.Attributes["title"].Value;
            }
            while ((titlePhrase != "Вы на последней странице") && (titlePhrase != ""));
            Logger.Info("Method GetCityEventList complete!");

            return rawResultEventList;
        }
    }
}
