Legend
✅ -> Completed new feature/update
🔥 -> Completed bug/hot fix
⌛ -> Completed performance/library update
✍️ -> Additional Info about a Change
📜 -> Higher level identifier for feature changes
❌ -> In-progress feature will be fixed in later release

Planned
❌See if Indigo can be fixed or not
❌Add some more sites

v4.0.3 - 

v4.0.2 - Jan 20th, 2025
🔥Remove Playwright package

v4.0.1 - Jan 20th, 2025
⌛Upgrade to .NET 9.0
✅Initialization method now accepts parametrized list
✅Added toggable option on whether to keep a Selenium WebDriver persistent instead of creating and disposing one every run (by default it is false)
✅Correctly fixed output type so it's "Library"
✅FireFox is the new default webdriver used for scrapes
✅SpeedyHen removed due to Cloudflare Captcha blocking scraping
🔥Fixed issue where FireFox would not work when site required a user-agent
🔥Fixed issue where scrape would sometimes fail because it can't parse the price correctly
📜Crunchyroll Fixes/Updates
-🔥Fixed url generation to prevent unwanted product from being parsed
-🔥"Back Order" stock status added
-⌛All-around performance improvements
📜InStockTrades Fixes/Updates
-🔥Fixed novel parsing
-🔥Fixed parsing for volumes with decimals (Vol 9.5)
-⌛All-around performance improvements
📜RobertsAnimeCornerStore Fixes/Updates
-🔥Fixed backorder stockstatus showing as OOS
-⌛All-around performance improvements
📜Books-A-Million Fixes/Updates
-🔥Fixed url generation to get correct entries
-🔥Fixed box set checking logic
-🔥Fixed naruto LN parsing when looking for manga
-🔥Fixed parse of volumes that don't have "Vol" in initial title (ex Jujutsu Kaisen 0)
-⌛All-around performance improvements
📜KinokuniyaUSA Fixes/Updates
-🔥Fixed issue where Kino would not load on FireFox
-🔥Fixed issue where parsing titles where volume number was proceeded by some text like "special edition" would not parse correctly
-🔥Correctly label items that are "available for order" as "Backorder (BO)" instead of "In Stock (IS)"
-🔥Fixed title parsing for light novels
-⌛All-around performance improvements
📜SciFier Fixes/Updates
-🔥Fixed issue where incorrect price would get scraped for entries
-🔥Properly ignores used copies
-🔥Fixed issue where scraping for light novel would not work for series that are not LN originals
-✅Link output for Scifier now outputs the exact links where entries are found
📜MangaMate Fixes/Updates
-🔥Properly gets the AUD currency
📜TravellingMan Fixes/Updates
-🔥Small fix to parsing for novels
📜ForbiddenPlanet Fixes/Updates
-🔥Fixed issue where it would not go to the next page correctly
-🔥Fixed issue where statue entries would be picked up when they shouldn't
-🔥Fixed issue where "sale" was being picked up as a book format
📜AmazonUSA Fixes/Updates
-🔥Complete scrape logic rewrite to fix a bunch of issues for manga
-✍️Parsing novels for some series still has issues, it's really hard to cover every possible case

v3.1.1 - Sept 16th, 2024
✅Added new helper function to print websites
🔥FireFox is now in headless mode

v3.1.0 - Sept 5th, 2024
✅Added new StockStatus, "Coming Soon (CS)"
✅PriceComparison now takes into accounts bundles where volumes are in range like "7-9"
✅Added comments to Website enum to include there region(s) 
📜Books-A-Million Fixes
-✅Volumes that are in stock but are "On Order" so will take time are now labeled as "BO" (Backorder) instead of "OOS" (Out of Stock)
-🔥Fixed issue where some omnibus titles would not be formatted correctly
-🔥Fixed issue where not all links would be printed at the end if for instance a series has boxsets the link is different
-🔥Fixed issue where it would loop forever due to pop up without cookies
-🔥Fixed issue where list would not sort if there was a crash or error during the scrape run
📜Crunchyroll Fixes
-🔥Bundles now are correctly parsed
-🔥Coming-Soon entries are correctly labeled as "CS" instead of "NA"
📜MerryManga Fixes (Website itself has issues loadings somestimes)
-🔥Fixed stockstatus xpath to also look for new MerryManga status "available at warehouse" which will be another "IS" status for this library
-🔥Fixed issue where it would not properly load all entries causing a crash
-🔥Fixed issue where the box-set link would be returned after a scrape run when the series has no box sets
-🔥Fixed issue where both the box-set and non box-set link would not both be returned if a series has entries for both
-🔥Fixed issue where box sets would not be added
📜Indigo Fixes (Still not working 100% correctly)
-🔥Fixed issue where it would always return no data

v3.0.0 - June 23rd, 2024
✅Added AmazonUSA
✅Removed Barnes & Noble
✅Removed Wordery
✅Changed default browser to FireFox from Chrome
🔥Fixed issue where removing duplicates would only remove the first duplicate instead of all of them
🔥Fixed issue where light novel scrape would fail to get the volume number and not sort
📜RobertsAnimeCornerStore Fixes
-🔥Website url changes applied so it runs without crashing
-🔥Fixed text formatting when series is a manga adaptation
-🔥Searching for light novels no longer crashes
📜ForbiddenPlanet Fixes
-🔥Fixed html parsing causing no data to be returned

v2.2.0 - March 8th
✅Removed Wordery from scrape until further notice
✅Fix membership list output in AsciiTable
✅Completed TravellingMan website for Britain Region 
✅Completed Forbidden Planet website for Britain Region
✅Extended "IsWebsiteListValid" method to accept enumerable of the Website enum
✅Added "Norgami" series formatting
⌛UserAgent no longer grabbed from selenium but HtmlWeb instead for faster initialization and no hanging chrome tasks
✍️Added website and region request issue templates (https://github.com/Sigrec/MangaAndLightNovelWebScrape/issues)
🔥Throws error when inputted list is not valid (list contains websites that are not supported by the current region that is set)
🔥Fixed "IsWebsiteListValid" methods to return correct value, before it was flipped
🔥Final data correctly sorted even if entry names without vol numbers are not exactly the same but very similar this includes case as well
📜RobertsAnimeCornerStore Fixes
-🔥Correctly gets series that have longer names in the website but user inputs shorthand (ex: Noragami vs Noragami Stray God)
-🔥Removes text between title and "Omnibus" tag
📜Barnes & Nobles Fixes
-🔥Correctly removes extra text after Omnibus Vol #
📜Books-A-Million Fixes
-🔥Correctly removes extra text after Omnibus Vol #
📜MerryManga Fixes
-🔥Fixed issue where series that did not contain any BoxSets would sometimes not return any data
📜Wordery Fixes
-🔥Correctly formats entries that have extra text after the vol #
-🔥Fixed issue where it would not wait to load the initial page
📜MangaMate Fixes
-🔥Correctly removes extra text after Omnibus Vol #
📜SpeedyHen Fixes
-🔥Correctly removes extra text after Omnibus Vol #

v2.1.2 - February 29th
✅Added new method to get websites that have a membership for a specific region

v2.1.0 - February 29th
✅Updated NuGet package desc
✅Added new param to Ascii Format table method and prints to determine whether to include links at the bottom
🔥Fixed Enum namespace so its MangaAndLightNovelWebScrape
🔥Fixed Model namespace for StockStatusFilter class so its MangaAndLightNovelWebScrape

v2.0.0 - February 29th 
✅Memberships are now bound to the MasterScrape object instead of the method
✅Moved Enums out of constants into it's own namespace
✅Added new method to get the results of a scrape formatted in a Ascii table, and options to print it as a ascii table to all print result methods
🔥Helper methods that return website list not return correctly as a HashSet
🔥Correctly throws error when user inputs a multi-flag Region
✍️Removed all websites from GetWebsite Helper methods that are not in a complete state
📜RobertsAnimeCornerStore Fixes
-🔥Correctly filters out unintended volumes like Coloring Books
-🔥Now checks all possible matching series lists on Rob's & prints all links
📜InStockTrades Fixes
-🔥Correctly filteres out entries that contain a # in the inputted title that corresponds to the vol # and not the # in the title of the inputted series

v1.3.2 - February 24th
✅Added unit test for region check
🔥Fixed Region for SpeedyHen

v1.3.1 - February 24th
🔥Fixed Region enum flags for Region

v1.3.0 - February 24th
✅Added helper method to validate a list against a given region
✅Added helper method to get a StockStatusFilter from a given string

v1.2.1 - February 3rd
🔥Fixed issue where SpeedyHen would not be added as a website when calling "GenerateWebsiteList"

v1.2.0 - February 3rd
✅Updated "GetStockStatusFromString" to include "Backorder" stock status
✅Completed MangaMate website for Australia Region
✅Completed SpeedyHen website for Britain Region
🔥Fix issue where MerryManga is not returned from website helper methods for America
✍️Fixing my versioning strategy to comply with .NET standards (https://learn.microsoft.com/en-us/dotnet/csharp/versioning)

v1.1.6 - January 31st
🔥Update Helper Methods, fixes issue from v1.1.5

v1.1.5 - January 31st (Has Major Bug where Helpers Methods are not Updated)
✅Added new Region "Australia"
✅Added Australia Region to SciFier website
✅Completed Wordery website for America, Britain, Canada, Europe, & Australia Regions
✍️Fixed tests to accomdate changes
📜SciFier
-🔥Fixed issue where StockStatus would not be picked up if it was OOS

V1.1.4 - December 27th
⌛Updated to .NET 8.0
✅Integrated StockStatus as it's own class and is now part of the MasterScrape
✅Added "Backorder (BO)" as a StockStatus
✅Updated print methods to check if the data is empty and print corresponding message
✅Added new print method to print to a file
✅Completed MerryManga website for America Region
✅Completed Waterstones website for Britain Region
✅Moved filter to its own object class and moved it up to the MasterScrape object variable level instead of method variable
✅Added 3 new stock status filters
🔥Website const variiable region is now public instead of private
📜Crunchyroll Fixes
-🔥"Volume 0's" the "0" is no longer cutoff
-🔥Omnibus parsing fixed for 2in1 and 2in1
-🔥Fixed AoT Special Edition volume parsing
-🔥Fixed issue where sometimes if a entry is on sale the stock status would return NA (Not Available) instead of correct stock status
📜SciFier
 -⌛Performance improvement when book title has a lot of pages now skips immedieatly instead of checking entries if not in char range
-🔥Added additional parsing for "Bleach" title
 -✍️Still issues with some light novel scrapes, hard to fix right now but will work on it later
📜RobertsAnimeCornerStore
-🔥Now correctly gets light novel data for a series where the manga and light novels are not in the same link
-🔥Fixed issue where light novel entries would be missing "Novel" in the scraped title sometimes
-🔥Light novel entries now correctly add "Vol" to entries that have a vol #
📜KinokuniyaUSA
-🔥Updated html parsing
-🔥Fixed issue where entries with titles at the end and subtitles in front now get parsed correctly
-🔥Fixed issue where color editions were not being scraped
 -✍️Still issue where some entries aren't included when sorting by manga, nothing I can do about that
📜InStockTrades
-🔥Minor fixes to some parsing
📜Indigo
-🔥Fixed crash issue related to stock status always returning null
-🔥Fixed issue where incorrect stock status would be returned on "pre-order" & "coming soon" entries
-🔥Fixed duplicate entry issue in final data sets
-🔥Fixwed issue where the check to see if a entry is a novel when searching for manga sometimes still includes a novel entry
-🔥Removes library edition volumes
-🔥Correctly formats collection edition volumes
-🔥Fixed duplicate "Vol" in special edition entries
-🔥Fix generated url to prevent non paperback & hardcover manga related entries from being skipped
📜Books-A-Million
-🔥Fix issue where boxsets would append vol 1 if they have no number sometimes
📜Barnes & Noble
-🔥Fix parsing issue with manga entries that are adaptations of novels

V1.1.3 - November 3rd
✅Added methods to print results to logger or console
🔥Changed namespace to MangaAndLightNovelWebScrape for cleaner naming conventions
🔥Crunchyroll volume numbers are nowtrimmed if there is a leading '0'
🔥Fixed issue w/ SciFier scrape where Vol # would be duplocated
⌛SciFier Scrape now uses the position of the first letter of the inputted title to determine how to sort the entries on the website to shrink processing time

V1.1.2 - November 1st
🔥HotFix to fix Helper method issues

V1.1.1 - November 1st
🔥HotFix to fix price comparison issue

V1.1.0 - November 1st
✅Websites no longer behind region specific namespace
🔥Fixed B&N StockStatus issue where "Unavailable" entries would be marked as IS (In Stock)
🔥Fixed issue w/ RobertsAnimeCornerStore where Omnibus volumes would have incorrect vol number
✅Changes to All Website Parsing for better filtering and to prevent certain keywords from skipping entries
✅Completed Canada Websites
✅Properly scrapes Special & Exclusive Edition volumes (ex AoT has both)