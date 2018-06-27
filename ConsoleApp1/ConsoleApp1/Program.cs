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
            Logger.Info("Method GetCityEventLinks started ...");
            watch.Start();
            var resultListEvent = GetCityEventLinks(); //заполнение списка событий (устанавливаем url и id для них) 
            watch.Stop();
            Console.WriteLine($"Elapsed time {watch.Elapsed}");
            Logger.Info("Json write and serialized started ...");
            string serialized = JsonConvert.SerializeObject(resultListEvent);
            File.WriteAllText("CityEventData.json", serialized);
            Logger.Info("Json write and serialized complete.");
            Console.ReadLine();
        }

        static List<CityEvent> GetCityEventLinks()
        {
            List<CityEvent> rawEventList = new List<CityEvent>();
            HtmlWeb web = new HtmlWeb();
            const int retryAttempsCount = 3;
            var pageUrl = "https://kuda-kazan.ru/event/";
            string titlePhrase = "";
            do
            {
                HtmlDocument pageOfCityEvents = null;
                for (int i = 0; i < retryAttempsCount; i++)
                {
                    pageOfCityEvents = web.Load(pageUrl);
                    if ((i + 1 == 3) && (pageOfCityEvents == null))
                    {
                        Logger.Error($"Can't get web event with url: {pageUrl}");
                        break;
                    }
                    if (pageOfCityEvents == null)
                        continue;
                    break;
                }
                var events = pageOfCityEvents.DocumentNode.SelectNodes("//*[@class='events_list']/div/div[1]/*[@itemprop='url']");
                foreach (HtmlNode n in events)
                {
                    CityEvent newCityEvent = new CityEvent();
                    newCityEvent.Url = n.Attributes["href"].Value;
                    var urlList = newCityEvent.Url.Split('/');
                    newCityEvent.ExternalId = urlList[urlList.Length - 2];
                    rawEventList.Add(newCityEvent);
                }

                var NextNode = pageOfCityEvents.GetElementbyId("content").SelectSingleNode("//*[@class='next']");
                pageUrl = NextNode.Attributes["href"].Value;
                titlePhrase = NextNode.Attributes["title"].Value;
            }
            while ((titlePhrase != "Вы на последней странице") && (titlePhrase != ""));
            Logger.Info("Method GetCityEventLinks complete!");
            Stopwatch watch = new Stopwatch();
            Logger.Info("Method FillCityEventsData started ...");
            watch.Start();
            var result = FillCityEventsData(rawEventList); //заполнение полей
            watch.Stop();
            Logger.Info("Method FillCityEventsData complete!");
            Console.WriteLine($"Elapsed time {watch.Elapsed}");
            return result;
        }

        static List<CityEvent> FillCityEventsData(List<CityEvent> rawEventList)
        {
            List<CityEvent> resultEvents = new List<CityEvent>();
            HtmlWeb web = new HtmlWeb();
            int n = 1;
            const int retryAttempsCount = 3;
            foreach (CityEvent ev in rawEventList)
            {
                try
                {
                    HtmlDocument docOfCityEvent = null;
                    for (int i = 0; i < retryAttempsCount; i++)
                    {
                        docOfCityEvent = web.Load(ev.Url);
                        if ((i + 1 == 3) && (docOfCityEvent == null))
                        {
                            Logger.Error($"Can't get web event with url: {ev.Url}");
                            break;
                        }
                        if (docOfCityEvent == null)
                            continue;
                        break;
                    }

                    Console.WriteLine("Обработка события №" + n);

                    var shortReviewNode = docOfCityEvent.GetElementbyId("short_review");
                    var startDateNode = shortReviewNode.SelectSingleNode("//*[@itemprop='startDate']");
                    if (startDateNode == null)
                    {
                        throw new Exception("Failed to get 'StartDate'");
                    }
                    else
                    {
                        ev.StartDate = startDateNode.InnerText;
                    }

                    var minPriceNode = shortReviewNode.SelectSingleNode("//*[@itemprop='price']");
                    if (minPriceNode == null)
                    {
                        throw new Exception("Failed to get 'minPrice'");
                    }
                    else
                    {
                        if (minPriceNode.InnerText.Contains("от"))
                        {
                            ev.MinPrice = int.Parse(minPriceNode.InnerText.Substring(minPriceNode.InnerText.IndexOf(' ') + 1));
                        }
                        else
                        {
                            if (minPriceNode.InnerText == "0.00")
                            {
                                ev.MinPrice = 0;
                            }
                            else
                            {
                                ev.MinPrice = int.Parse(minPriceNode.InnerText);
                            }
                        }
                    }

                    var endDateNode = shortReviewNode.SelectSingleNode("//*[@itemprop='endDate']");
                    var endDateNodeText = endDateNode.InnerText;
                    if ((endDateNode != null) && (ev.StartDate != endDateNodeText))
                    {
                        ev.EndDate = endDateNode.InnerText;
                    }

                    var viewsCountNode = shortReviewNode.SelectSingleNode("//*[@class='wishes']/span[1]");
                    if (viewsCountNode == null)
                    {
                        throw new Exception("Failed to get 'ViewsCount'");
                    }
                    else
                    {
                        ev.ViewsCount = int.Parse(viewsCountNode.InnerText);
                    }

                    var contentNode = docOfCityEvent.GetElementbyId("content");

                    var nameNode = contentNode.SelectSingleNode("//h1");
                    if (nameNode == null)
                    {
                        throw new Exception("Failed to get 'Name'");
                    }
                    else
                    {
                        ev.Name = WebUtility.HtmlDecode(nameNode.InnerText);
                    }

                    var descriptionNode = contentNode.SelectSingleNode("//*[@itemprop='description']/p[1]");

                    if (descriptionNode == null)
                    {
                        throw new Exception("Failed to get 'Description'");
                    }
                    else
                    {
                        ev.Description = WebUtility.HtmlDecode(descriptionNode.InnerText);
                    }
                    n++;
                    resultEvents.Add(ev);
                }
                catch (Exception ex)
                {
                    string guid = Guid.NewGuid().ToString();
                    Logger.Error($"{ex.Message}");
                    string filename = $"{guid}";
                }
            }
            return resultEvents;
        }
    }
}