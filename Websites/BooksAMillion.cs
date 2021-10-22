using System.Threading;
using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace MangaWebScrape.Websites
{
    public class BooksAMillion
    {

        public static List<string> BooksAMillionLinks = new List<string>();
        private static List<string[]> dataList = new List<string[]>();

        //https://www.booksamillion.com/search?query=World+Trigger&filter=product_type%3Abooks%7Cbook_categories%3ACGN&sort=date
        //https://www.booksamillion.com/search?query=World%20Trigger;filter=product_type%3Abooks%7Cbook_categories%3ACGN;sort=date;page=1
        //https://www.booksamillion.com/search?query=Jujutsu+Kaisen&filter=product_type%3Abooks%7Cbook_categories%3ACGN&sort=date
        //https://www.booksamillion.com/search?query=O7-Ghost;filter=product_type%3Abooks%7Cbook_categories%3ACGN;sort=date;page=1

        private static string GetUrl(string bookTitle, byte currPageNum){
            string url = "https://www.booksamillion.com/search?query=" + bookTitle.Replace(" ", "%20") + "filter=product_type%3Abooks%7Cbook_categories%3ACGN;sort=date;page=" + currPageNum;
            BooksAMillionLinks.Add(url);
            return url;
        }

        public static List<string[]> GetBooksAMillionData(string bookTitle, byte currPageNum)
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

            edgeDriver.Navigate().GoToUrl(GetUrl(bookTitle, currPageNum));
            Thread.Sleep(2000);
            doc.LoadHtml(edgeDriver.PageSource);

            // Get the page data from the HTML doc
            HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//div[@class='search-item-title']//a");
            HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes("//div[contains(@title, 'Preorder') and @class='availability_search_results'] | //span[@class='stockOrange']/text()[1] | //span[@class='stockGreen']");
            HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//span[@class='our-price']");
            HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//a[@title='Next']");
            Console.WriteLine(stockStatusData == null);
            try{
                string stockStatus;
                for (int x = 0; x < titleData.Count; x++){
                    //currTitle = titleData[x].InnerText.Trim();
                    
                    stockStatus = stockStatusData[x].InnerText;
                    if (stockStatus.Contains("In Stock")){
                        stockStatus = "IS";
                    }
                    else if (stockStatus.Contains("Pre-Order")){
                        stockStatus = "PO";
                    }
                    else if (stockStatus.Contains("On Order")){
                        stockStatus = "OO";
                    }

                    Console.WriteLine(titleData[x].InnerText.Trim() + " " + priceData[x].InnerText.Trim() + " " + stockStatus + " " + "Books-A-Million");
                }

                if (pageCheck != null){
                    currPageNum++;
                    GetBooksAMillionData(bookTitle, currPageNum);
                }
                else{
                    edgeDriver.Quit();
                    foreach (string link in BooksAMillionLinks){
                        Console.WriteLine(link);
                    }
                }
            }
            catch(NullReferenceException ex){
                Console.Error.WriteLine(bookTitle + " Does Not Exist at Books-A-Million\n" + ex);
            }

            // using (StreamWriter outputFile = new StreamWriter(@"C:\TsundeOku\Data\BooksAMillionData.txt"))
            // {
            //     if (dataList.Count != 0){
            //         foreach (string[] data in dataList){
            //             outputFile.WriteLine(data[0] + " " + data[1] + " " + data[2] + " " + data[3]);
            //         }
            //     }
            //     else{
            //         outputFile.WriteLine(bookTitle + " Does Not Exist at Books-A-Million");
            //     }
            // }  

            return dataList;
        }
    }
}