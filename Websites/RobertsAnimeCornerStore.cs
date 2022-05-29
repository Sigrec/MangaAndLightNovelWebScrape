using System.Threading;
using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace MangaWebScrape.Websites
{
    class RobertsAnimeCornerStore
    {
        public static List<string> robertsAnimeCornerStoreLinks = new List<String>();
        private static List<string[]> robertsAnimeCornerStoreDataList = new List<string[]>();
        private static bool doubleCheck = false;
        
        private static string GetUrl(string htmlString, bool pageExists)
        {
            Dictionary<string, Regex> urlMapDict = new Dictionary<string, Regex>()
            {
                {"mangrapnovag", new Regex(@"^[a-bA-B\d]")},
                {"mangrapnovhp", new Regex(@"^[c-dC-D]")},
                {"mangrapnovqz", new Regex(@"^[e-gE-G]")},
                {"magrnomo", new Regex(@"^[h-kH-K]")},
                {"magrnops", new Regex(@"^[l-nL-N]")},
                {"magrnotz", new Regex(@"^[o-qO-Q]")},
                {"magrnors", new Regex(@"^[r-sR-S]")},
                {"magrnotv", new Regex(@"^[t-vT-V]")},
                {"magrnowz", new Regex(@"^[w-zW-Z]")}
            };

            string url = "";
            if (!pageExists) // Gets the starting page based on first letter and checks if we are looking for the 1st webpage (false) or 2nd webpage containing the actual item data (true)
            {
                Parallel.ForEach(urlMapDict, (link, state) =>
                {
                    if (link.Value.Match(htmlString).Success)
                    {
                        url = "https://www.animecornerstore.com/" + link.Key + ".html";
                        robertsAnimeCornerStoreLinks.Add(url);
                        state.Stop();
                    }
                });
            }
            else
            { //Gets the actual page that houses the data the user is looking for
                url = "https://www.animecornerstore.com/" + htmlString;
                robertsAnimeCornerStoreLinks.Add(url);
            }
            Console.WriteLine(url);
            return url;
        }

        /**
         * TODO: Figure out a way to when checking for title for it to ignore case
         */
        private static string GetPageData(EdgeDriver edgeDriver, string bookTitle, char bookType, HtmlDocument doc)
        {
            string link = "";
            string typeCheck = bookType == 'N' ? "not(contains(text()[2], ' Graphic'))" : "contains(text()[2], ' Graphic')";
            edgeDriver.Navigate().GoToUrl(GetUrl(bookTitle, false));
            Thread.Sleep(2000);
            doc.LoadHtml(edgeDriver.PageSource);

            Console.WriteLine($"//b//a[1][contains(text()[1], '{bookTitle}')][{typeCheck}]");
            HtmlNode seriesTitle = doc.DocumentNode.SelectSingleNode($"//b//a[1][contains(text()[1], '{bookTitle}')][{typeCheck}]");
            try
            {
                if (seriesTitle == null)
                {
                    if (doubleCheck == false)
                    {
                        doubleCheck = true;
                        if (bookType == 'N')
                        {
                            Console.WriteLine($"Couldn't Find Novel Page for {bookTitle} -> Looking in the Manga Page if Available");
                            return GetPageData(edgeDriver, bookTitle, 'M', doc);
                        }
                        else if (bookType == 'M')
                        {
                            Console.WriteLine($"Couldn't Find Manga Page for {bookTitle} -> Looking in the Novel Page if Available");
                            return GetPageData(edgeDriver, bookTitle, 'N', doc);
                        }
                    }
                    else
                    {
                        return "DNE";
                    }
                }
                link = GetUrl(seriesTitle.Attributes["href"].Value, true);
            }
            catch(NullReferenceException ex)
            {
                Console.Error.WriteLine(ex);
            }
            return link;
        }

        public static List<string[]> GetRobertsAnimeCornerStoreData(string bookTitle, char bookType, EdgeOptions edgeOptions)
        {
            EdgeDriver edgeDriver = new EdgeDriver(edgeOptions);

            // Initialize the html doc for crawling
            HtmlDocument doc = new HtmlDocument();

            string linkPage = GetPageData(edgeDriver, bookTitle, bookType, doc);
            string errorMessage;
            if (string.IsNullOrEmpty(linkPage))
            {
                errorMessage = "Error! Invalid Series Title";
                Console.WriteLine(errorMessage);
                edgeDriver.Quit();
            }
            else
            {
                try
                {
                    Console.WriteLine(linkPage);
                    // Start scraping the URL where the data is found
                    edgeDriver.Navigate().GoToUrl(linkPage);
                    Thread.Sleep(2000);

                    // Get the html doc for crawling
                    doc.LoadHtml(edgeDriver.PageSource);

                    //Gets the title for each available item
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//font[@face='dom bold, arial, helvetica']/b/text()[1]");

                    // Gets the lowest price for each item, for loop removes the larger price
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//form[@method='POST'][contains(text()[2], '$')]/text()[2] | //font[2][@color='#ffcc33']");
                    for(int x = 0; x < priceData.Count; x++)
                    {
                        if (priceData[x].InnerText[0].Equals(' '))
                        {
                            priceData.RemoveAt(x);
                        }
                    }

                    edgeDriver.Quit();
                    string currTitle, stockStatus;
                    for (int x = 0; x < titleData.Count; x++)
                    {
                        if ((titleData[x].InnerText.Contains("[Novel]") && bookType == 'M') || (titleData[x].InnerText.Contains("Graphic") && bookType == 'N')) // If the user is looking for manga and the page returned a novel data set and vice versa skip that data set
                        {
                            continue;
                        }

                        stockStatus = (titleData[x].InnerText.IndexOf("Pre Order") | titleData[x].InnerText.IndexOf("Backorder")) != -1 ? "PO" : "IS";
                        currTitle = Regex.Replace(Regex.Replace(titleData[x].InnerText, @",|#|Graphic Novel| :|\(.*?\)|\[Novel\]", ""), @"[ ]{2,}", " ").Trim();

                        if (currTitle.Contains("Omnibus") && currTitle.Contains("Vol"))
                        {
                            if (currTitle.Contains("One Piece") && currTitle.Contains("Vol 10-12")) // Fix naming issue with one piece
                            {
                                currTitle = currTitle.Substring(0, currTitle.IndexOf(" Vol")) + " 4";
                            }
                            else
                            {
                                currTitle = currTitle.Substring(0, currTitle.IndexOf("Vol"));
                            }
                            currTitle = currTitle.Substring(0, currTitle.IndexOf("Omnibus ") + "Omnibus ".Length) + "Vol " + currTitle.Substring(currTitle.IndexOf("Omnibus ") + "Omnibus ".Length);
                        }
                        
                        robertsAnimeCornerStoreDataList.Add(new string[]{currTitle, priceData[x].InnerText.Trim(), stockStatus, "RobertsAnimeCornerStore"});
                    }

                    foreach (string link in robertsAnimeCornerStoreLinks)
                    {
                        Console.WriteLine(link);
                    }

                    robertsAnimeCornerStoreDataList.Sort(new VolumeSort(bookTitle));
                    using (StreamWriter outputFile = new StreamWriter(@"Data\RobertsAnimeCornerStoreData.txt"))
                    {
                        if (robertsAnimeCornerStoreDataList.Count != 0)
                        {
                            foreach (string[] data in robertsAnimeCornerStoreDataList)
                            {
                                outputFile.WriteLine("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]");
                            }
                        }
                        else
                        {
                            errorMessage = bookTitle + " Does Not Exist at RobertsAnimeCornerStore";
                            outputFile.WriteLine(errorMessage);
                        }
                    } 
                }
                catch(NullReferenceException ex)
                {
                    Console.Error.WriteLine(bookTitle + " Does Not Exist at RobertsAnimeCornerStore\n" + ex);
                }
            }
            
            return robertsAnimeCornerStoreDataList;
        }
    }
}