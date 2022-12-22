using System.Threading;
using System;
using OpenQA.Selenium.Edge;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace MangaLightNovelWebScrape.Websites
{
    public class InStockTrades
    {
        public static List<string> inStockTradesLinks = new List<string>(); //List of links used to get data from InStockTrades
        private static List<string[]> inStockTradesDataList = new List<string[]>(); //List of all the series data from InStockTrades

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        //https://www.instocktrades.com/search?term=world+trigger
        //https://www.instocktrades.com/search?pg=1&title=World+Trigger&publisher=&writer=&artist=&cover=&ps=true
        private static string GetUrl(byte currPageNum, string bookTitle){
            string url = "https://www.instocktrades.com/search?pg=" + currPageNum +"&title=" + bookTitle.Replace(' ', '+') + "&publisher=&writer=&artist=&cover=&ps=true";
            inStockTradesLinks.Add(url);
            Logger.Debug(url);
            return url;
        }

        public static List<string[]> GetInStockTradesData(string bookTitle, byte currPageNum, EdgeOptions edgeOptions)
        {
            // Initialize the html doc for crawling
            HtmlDocument doc = new HtmlDocument();
            EdgeDriver edgeDriver = new EdgeDriver(edgeOptions);

            edgeDriver.Navigate().GoToUrl(GetUrl(currPageNum, bookTitle));
            Thread.Sleep(2000);
            doc.LoadHtml(edgeDriver.PageSource);

            // Get the page data from the HTML doc
            HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//div[@class='title']/a");
            HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//div[@class='price']");
            HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//a[@class='btn hotaction']");

            edgeDriver.Quit();
            
            if (titleData != null)
            {
                string currTitle;
                int volNumIndex;
                for (int x = 0; x < titleData.Count; x++)
                {
                    currTitle = titleData[x].InnerText.Trim().Replace("GN ", "");
                    volNumIndex = currTitle.IndexOf("Vol") + 4;
                    inStockTradesDataList.Add(new string[]{!currTitle[volNumIndex].Equals('0') ? currTitle : currTitle.Remove(volNumIndex, 1), priceData[x].InnerText.Trim(), "IS", "InStockTrades"});
                    //Logger.Debug(volNumIndex != 0 ? currTitle : currTitle.Remove(volNumIndex, 1) + " " + priceData[x].InnerText.Trim() + " " + "InStockTrades");
                }

                if (pageCheck != null){
                    currPageNum++;
                    GetInStockTradesData(bookTitle, currPageNum, edgeOptions);
                }
                else{
                    inStockTradesDataList.Sort(new VolumeSort(bookTitle));
                    foreach (string link in inStockTradesLinks){
                        Logger.Debug(link);
                    }
                }

                //Print data to a txt file
                using (StreamWriter outputFile = new StreamWriter(@"Data\InStockTradesData.txt"))
                {
                    if (inStockTradesDataList.Count != 0){
                        foreach (string[] data in inStockTradesDataList){
                            outputFile.WriteLine("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]");
                        }
                    }
                    else{
                        outputFile.WriteLine(bookTitle + " Does Not Exist at InStockTrades");
                    }
                } 
            }
            else
            {
                Logger.Debug(bookTitle + " Does Not Exist at InStockTrades");
            }

            return inStockTradesDataList;
        }
    }
}