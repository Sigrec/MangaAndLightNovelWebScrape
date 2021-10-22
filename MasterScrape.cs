using System;
using System.IO;
using MangaWebScrape.Websites;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MangaWebScrape
{
    class MasterScrape
    { 
        private static List<string[]> RightStufAnimeData = new List<string[]>();
        private static List<string[]> RobertsAnimeCornerStoreData = new List<string[]>();
        private static List<string[]> InStockTradesData = new List<string[]>();
        private static List<string[]> BooksAMillionData = new List<string[]>();
        private static List<string> SelectedWebsites = new List<string>();
        private static Regex defaultTitlePattern = new Regex(@"(\d+$)");
        private static string bookTitle;
        private static char bookType;

        /*
            First checks to see which website has fewer entires then compares the pricing for volumes and outputs a list of the volumes with the lowest price and the retailer
            @param biggerList, 
            @param smallerList
            @return List<string[]>
        */
        private static List<string[]> PriceComparison(List<string[]> biggerList, List<string[]> smallerList){
            bool checker = false;
            List<string[]> FinalData = new List<string[]>();
            for (int x = 0; x < biggerList.Count; x++){
                for(int y = 0; y < smallerList.Count; y++)
                {
                    if(defaultTitlePattern.Match(biggerList[x][0]).Groups[1].Value.Equals(defaultTitlePattern.Match(smallerList[y][0]).Groups[1].Value)){
                        if (Convert.ToDouble(biggerList[x][1].Substring(1)) > Convert.ToDouble(smallerList[y][1].Substring(1))){
                            FinalData.Add(smallerList[y]);
                            smallerList.RemoveAt(y);
                            checker = true;
                        }
                    }
                }
                if (!checker) { 
                    FinalData.Add(biggerList[x]); 
                }
                checker = false;
            }
            return FinalData;
        }

        static void Main(string[] args){
            Console.Write("What is the Manga/Light Novel Title: ");
            bookTitle = Console.ReadLine();
            
            Console.Write("Are u searching for a Manga (M) or Light Novel (N): ");
            bookType = char.Parse(Console.ReadLine());

            Stopwatch watch = new Stopwatch();
            watch.Start();

            // var RightStufAnimeTask = Task.Factory.StartNew(() => RightStufAnime.GetRightStufAnimeData(bookTitle, bookType, true, 1));

            // var RobertsAnimeCornerStoreTask = Task.Factory.StartNew(() => RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData(bookTitle, bookType));

            // var InStockTradesTask = Task.Factory.StartNew(() => InStockTrades.GetInStockTradesData(bookTitle, 1));

            var BooksAMillionTask = Task.Factory.StartNew(() => BooksAMillion.GetBooksAMillionData(bookTitle, 1));

            // Task.WhenAll(RightStufAnimeTask, RobertsAnimeCornerStoreTask, InStockTradesTask, BooksAMillionTask);

            // RightStufAnimeData = RightStufAnimeTask.Result;
            // RobertsAnimeCornerStoreData = RobertsAnimeCornerStoreTask.Result;
            // InStockTradesData = InStockTradesTask.Result;
            BooksAMillionData = BooksAMillionTask.Result;


            List<string[]> FinalData = BooksAMillionData;//PriceComparison(RightStufAnimeData, PriceComparison(InStockTradesData, RobertsAnimeCornerStoreData));
            using (StreamWriter outputFile = new StreamWriter(@"C:\TsundeOku\Data.txt"))
            {
                foreach (string[] data in FinalData){
                    outputFile.WriteLine(data[0] + " " + data[1] + " " + data[2] + " " + data[3]);
                }
            }  

            watch.Stop();
            Console.WriteLine($"Time in Miliseconds: {watch.ElapsedMilliseconds}");
        }
    }
}