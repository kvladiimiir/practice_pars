using System;
using HtmlAgilityPack;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace ConsoleApp1
{
    public class Event
    {
        public Event()
        {      
        }

        public string Url { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Price { get; set; }
        public string ViewsCount { get; set; }
    }


    class Program
    {
        public static List<Event> eventList; //список событий
        public static HtmlWeb web;
        static void Main()
        {
            eventList = new List<Event>();
            web = new HtmlWeb();
            GetEventLinks(); //заполнение списка событий (устанавливаем url и id для них) 
            FillEventsData(); //заполнение полей
            string serialized = JsonConvert.SerializeObject(eventList);
            File.WriteAllText("EventData.json", serialized);
        }

        static void GetEventLinks()
        {
            var pageUrl = "https://kuda-kazan.ru/event/";
            string titlePhrase = "";
            do
            {
                var pageOfEvents = web.Load(pageUrl);
                if (pageOfEvents != null)
                {
                    var events = pageOfEvents.DocumentNode.SelectNodes("//*[@class='events_list']/div/div[1]/*[@itemprop='url']");

                    if (events != null)
                    {
                        foreach (HtmlNode n in events)
                        {
                            Event newEvent = new Event();
                            newEvent.Url = n.Attributes["href"].Value;
                            var urlList = newEvent.Url.Split('/');
                            newEvent.Id = urlList[urlList.Length - 2];
                            eventList.Add(newEvent);
                        }
                    }
                    var NextNode = pageOfEvents.GetElementbyId("content").SelectSingleNode("//*[@class='next']");
                    pageUrl = NextNode.Attributes["href"].Value;
                    titlePhrase = NextNode.Attributes["title"].Value;
                }
                else
                {
                    break;
                }
            }
            while ((titlePhrase != "Вы на последней странице") && (titlePhrase != ""));
        }

        static void FillEventsData()
        {
            foreach (Event ev in eventList)
            {
                var docOfEvent = web.Load(ev.Url);
                var shortReviewNode = docOfEvent.GetElementbyId("short_review");
                ev.StartDate = shortReviewNode.SelectSingleNode("//*[@itemprop='startDate']").InnerText;
                ev.Price = shortReviewNode.SelectSingleNode("//*[@itemprop='price']").InnerText;
                var endDate = shortReviewNode.SelectSingleNode("//*[@itemprop='endDate']").InnerText;
                if (ev.StartDate != endDate)
                    ev.EndDate = endDate;
                ev.ViewsCount = shortReviewNode.SelectSingleNode("//*[@class='wishes']/span[1]").InnerText;
                var contentNode = docOfEvent.GetElementbyId("content");
                ev.Name = contentNode.SelectSingleNode("//h1").InnerText;
                ev.Description = contentNode.SelectSingleNode("//*[@itemprop='description']/p[1]").InnerText;
                //замена &nbsp &laquo &raquo &mdash
                string space = " ";
                string hyphen = "-";
                char[] chr = { '"' };
                string quotes = new string(chr);
                ev.Description = ev.Description.Replace("&nbsp;", space);
                ev.Description = ev.Description.Replace("&mdash;", hyphen);
                ev.Description = ev.Description.Replace("&laquo;", quotes);
                ev.Description = ev.Description.Replace("&raquo;", quotes);
                ev.Name = ev.Name.Replace("&nbsp;", space);
                ev.Name = ev.Name.Replace("&mdash;", hyphen);
                ev.Name = ev.Name.Replace("&laquo;", quotes);
                ev.Name = ev.Name.Replace("&raquo;", quotes);
            }
        }
    }
}