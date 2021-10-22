using System.Threading;
using System.Text.RegularExpressions;
using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.IO;

namespace MangaWebScrape.Websites
{
    public class InStockTrades
    {
        public static List<string> InStockTradesLinks = new List<string>();
        private static List<string[]> dataList = new List<string[]>();

        //https://www.instocktrades.com/search?term=world+trigger
        //https://www.instocktrades.com/search?pg=1&title=World+Trigger&publisher=&writer=&artist=&cover=&ps=true
        private static string GetUrl(byte currPageNum, string bookTitle){
            string url = "https://www.instocktrades.com/search?pg=" + currPageNum +"&title=" + bookTitle.Replace(' ', '+') + "&publisher=&writer=&artist=&cover=&ps=true";
            InStockTradesLinks.Add(url);
            return url;
        }

        public static List<string[]> GetInStockTradesData(string bookTitle, byte currPageNum)
        {
            // Initialize the html doc for crawling
            HtmlDocument doc = new HtmlDocument();

            EdgeOptions edgeOptions = new EdgeOptions();
            edgeOptions.UseChromium = true;
            edgeOptions.PageLoadStrategy = PageLoadStrategy.Eager;
            edgeOptions.AddArgument("headless");
            edgeOptions.AddArgument("disable-gpu");
            edgeOptions.AddArgument("disable-extensions");
            edgeOptions.AddArgument("inprivate");
            EdgeDriver edgeDriver = new EdgeDriver(edgeOptions);

            edgeDriver.Navigate().GoToUrl(GetUrl(currPageNum, bookTitle));
            Thread.Sleep(2000);
            doc.LoadHtml(edgeDriver.PageSource);

            // Get the page data from the HTML doc
            HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//div[@class='title']/a");
            HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//div[@class='price']");
            HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//a[@class='btn hotaction']");

            try{
                string currTitle;
                int volNumIndex;
                for (int x = 0; x < titleData.Count; x++)
                {
                    currTitle = titleData[x].InnerText.Trim();
                    volNumIndex = currTitle.IndexOf("Vol") + 4;
                    dataList.Add(new string[]{!currTitle[volNumIndex].Equals('0') ? currTitle : currTitle.Remove(volNumIndex, 1), priceData[x].InnerText.Trim(), "IS", "InStockTrades"});
                    //Console.WriteLine(volNumIndex != 0 ? currTitle : currTitle.Remove(volNumIndex, 1) + " " + priceData[x].InnerText.Trim() + " " + "InStockTrades");
                }

                if (pageCheck != null){
                    currPageNum++;
                    GetInStockTradesData(bookTitle, currPageNum);
                }
                else{
                    edgeDriver.Quit();
                    foreach (string link in InStockTradesLinks){
                        Console.WriteLine(link);
                    }
                }
            }
            catch(NullReferenceException ex){
                Console.Error.WriteLine(bookTitle + " Does Not Exist at InStockTrades\n" + ex);
            }

            using (StreamWriter outputFile = new StreamWriter(@"C:\TsundeOku\Data\InStockTradesData.txt"))
            {
                if (dataList.Count != 0){
                    foreach (string[] data in dataList){
                        outputFile.WriteLine(data[0] + " " + data[1] + " " + data[2] + " " + data[3]);
                    }
                }
                else{
                    outputFile.WriteLine(bookTitle + " Does Not Exist at InStockTrades");
                }
            }  

            return dataList;
        }
    }
}