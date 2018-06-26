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
        public static HtmlWeb web;
        static void Main()
        {
            Stopwatch watch = new Stopwatch();
            Logger.Info("Method GetCityEventLinks started ...");
            watch.Start();
            var rawEventList = GetCityEventLinks(); //заполнение списка событий (устанавливаем url и id для них) 
            watch.Stop();
            Logger.Info("Method GetCityEventLinks complete!");
            Console.WriteLine($"Elapsed time {watch.Elapsed}");
            watch = new Stopwatch();
            Logger.Info("Method FillCityEventsData started ...");
            watch.Start();
            FillCityEventsData(rawEventList); //заполнение полей
            watch.Stop();
            Logger.Info("Method FillCityEventsData complete!");
            Console.WriteLine($"Elapsed time {watch.Elapsed}");
            Logger.Info("Json write and serialized started ...");
            string serialized = JsonConvert.SerializeObject(rawEventList);
            File.WriteAllText("CityEventData.json", serialized);
            Logger.Info("Json write and serialized complete.");
            Console.ReadLine();
        }

        static List<CityEvent> GetCityEventLinks()
        {
            List<CityEvent> rawEventList = new List<CityEvent>();
            web = new HtmlWeb();
            const int retryAttempsCount = 3;
            var pageUrl = "https://kuda-kazan.ru/event/";
            string titlePhrase = "";
            do
            {
                for (int i = 0; i < retryAttempsCount; i++)
                {
                    var pageOfCityEvents = web.Load(pageUrl);
                    if ((i+1 == 3) && (pageOfCityEvents == null))
                    {
                        Logger.Error($"Can't get web page with url: {pageUrl}");
                        break;
                    }
                    if (pageOfCityEvents == null)
                        continue;
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
            }
            while ((titlePhrase != "Вы на последней странице") && (titlePhrase != ""));
            return rawEventList;
        }

        static void FillCityEventsData(List<CityEvent> rawEventList)
        {
            List<CityEvent> resultEvents = new List<CityEvent>();
            web = new HtmlWeb();
            //const int retryAttempsCount = 3;
            foreach (CityEvent ev in rawEventList)
            {
                //for (int i = 0; i < retryAttempsCount; i++)
                //{
                   var docOfCityEvent = web.Load(ev.Url);
                   //if ((i + 1 == 3) && (docOfCityEvent == null))
                   //{
                       // Logger.Error($"Can't get web event with url: {ev.Url}");
                     //   break;
                   //}
                  // if (docOfCityEvent == null)
                    //    continue;
                    Logger.Info("Fill event started ... ");
                    var shortReviewNode = docOfCityEvent.GetElementbyId("short_review");
                    var startDateNode = shortReviewNode.SelectSingleNode("//*[@itemprop='startDate']");
                    if (startDateNode != null)
                        ev.StartDate = startDateNode.InnerText;
                    var minPriceNode = shortReviewNode.SelectSingleNode("//*[@itemprop='price']");
                    if (minPriceNode != null)
                        ev.MinPrice = minPriceNode.InnerText;
                    //if ((minPriceNode != null) && (minPriceNode.InnerText == "0.00"))
                    //{
                    //if (minPriceNode.InnerText == "0.00")
                    //{
                    //ev.MinPrice = 0;
                    //} цены 3 видов 0.00 , от 2000, 1000
                    //if ()
                    //{
                    //ev.MinPrice = int.Parse(minPriceNode.InnerText.Substring(minPriceNode.InnerText.IndexOf(' ') + 1));
                    // }

                    // }
                    var endDateNode = shortReviewNode.SelectSingleNode("//*[@itemprop='endDate']");
                    if ((endDateNode != null) && (startDateNode != endDateNode))
                        ev.EndDate = endDateNode.InnerText;
                    var viewsCountNode = shortReviewNode.SelectSingleNode("//*[@class='wishes']/span[1]");
                    if (viewsCountNode != null)
                        ev.ViewsCount = int.Parse(viewsCountNode.InnerText);
                    var contentNode = docOfCityEvent.GetElementbyId("content");
                    var nameNode = contentNode.SelectSingleNode("//h1");
                    if (nameNode != null)
                        ev.Name = WebUtility.HtmlDecode(nameNode.InnerText);
                    var descriptionNode = contentNode.SelectSingleNode("//*[@itemprop='description']/p[1]");
                    if (descriptionNode != null)
                        ev.Description = WebUtility.HtmlDecode(descriptionNode.InnerText);
                    Logger.Info("Fill event complete!");
                
                resultEvents.Add(ev);
            }
        }
    }
}